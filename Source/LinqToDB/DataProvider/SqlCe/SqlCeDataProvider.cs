using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider.SqlCe
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SqlCeDataProvider : DynamicDataProviderBase
	{
		public SqlCeDataProvider()
			: this(ProviderName.SqlCe, new SqlCeMappingSchema())
		{
		}

		protected SqlCeDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsSubQueryColumnSupported            = false;
			SqlProviderFlags.IsCountSubQuerySupported             = false;
			SqlProviderFlags.IsApplyJoinSupported                 = true;
			SqlProviderFlags.IsInsertOrUpdateSupported            = false;
			SqlProviderFlags.IsCrossJoinSupported                 = true;
			SqlProviderFlags.IsDistinctOrderBySupported           = false;
			SqlProviderFlags.IsOrderByAggregateFunctionsSupported = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported     = false;
			SqlProviderFlags.IsUpdateFromSupported                = false;

			SetCharFieldToType<char>("NChar", (r, i) => DataTools.GetChar(r, i));

			SetCharField("NChar",    (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("NVarChar", (r,i) => r.GetString(i).TrimEnd(' '));

			_sqlOptimizer = new SqlCeSqlOptimizer(SqlProviderFlags);
		}

		public    override string ConnectionNamespace => "System.Data.SqlServerCe";
		protected override string ConnectionTypeName  => $"{ConnectionNamespace}.SqlCeConnection, {ConnectionNamespace}";
		protected override string DataReaderTypeName  => $"{ConnectionNamespace}.SqlCeDataReader, {ConnectionNamespace}";

#if !NETSTANDARD2_0
		public override string DbFactoryProviderName => "System.Data.SqlServerCe.4.0";
#endif

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
		}

		#region Overrides

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new SqlCeSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SqlCeSchemaProvider();
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.Xml :
					dataType = dataType.WithDataType(DataType.NVarChar);

					if      (value is SqlXml xml)      value = xml.IsNull ? null : xml.Value;
					else if (value is XDocument xdoc)  value = xdoc.ToString();
					else if (value is XmlDocument doc) value = doc.InnerXml;

					break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			SqlDbType? type = null;

			switch (dataType.DataType)
			{
				case DataType.Text       :
				case DataType.NText      : type = SqlDbType.NText;     break;
				case DataType.VarChar    :
				case DataType.NVarChar   : type = SqlDbType.NVarChar;  break;
				case DataType.Timestamp  : type = SqlDbType.Timestamp; break;
				case DataType.Binary     : type = SqlDbType.Binary;    break;
				case DataType.VarBinary  : type = SqlDbType.VarBinary; break;
				case DataType.Image      : type = SqlDbType.Image;     break;
			}

			if (type != null)
			{
				SqlCeWrappers.Initialize();
				var param = TryConvertParameter(SqlCeWrappers.ParameterType, parameter, dataConnection.MappingSchema);
				if (param != null)
				{
					SqlCeWrappers.TypeSetter(param, type.Value);
					return;
				}
			}

			switch (dataType.DataType)
			{
				case DataType.SByte      : parameter.DbType = DbType.Int16;             return;
				case DataType.UInt16     : parameter.DbType = DbType.Int32;             return;
				case DataType.UInt32     : parameter.DbType = DbType.Int64;             return;
				case DataType.UInt64     : parameter.DbType = DbType.Decimal;           return;
				case DataType.VarNumeric : parameter.DbType = DbType.Decimal;           return;
				case DataType.Char       : parameter.DbType = DbType.StringFixedLength; return;
				case DataType.Date       :
				case DataType.DateTime2  : parameter.DbType = DbType.DateTime;          return;
				case DataType.Money      : parameter.DbType = DbType.Currency;          return;
				case DataType.Text       :
				case DataType.VarChar    :
				case DataType.NText      : parameter.DbType = DbType.String;            return;
				case DataType.Timestamp  :
				case DataType.Binary     :
				case DataType.Image      : parameter.DbType = DbType.Binary;            return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

#endregion

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return true;
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new SqlCeBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SqlCeTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

#endregion
	}
}
