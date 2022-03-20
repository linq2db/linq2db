using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider.SqlCe
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SqlCeDataProvider : DynamicDataProviderBase<SqlCeProviderAdapter>
	{
		public SqlCeDataProvider()
			: this(ProviderName.SqlCe, new SqlCeMappingSchema())
		{
		}

		protected SqlCeDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema, SqlCeProviderAdapter.GetInstance())
		{
			SqlProviderFlags.IsSubQueryColumnSupported            = false;
			SqlProviderFlags.IsCountSubQuerySupported             = false;
			SqlProviderFlags.IsApplyJoinSupported                 = true;
			SqlProviderFlags.IsInsertOrUpdateSupported            = false;
			SqlProviderFlags.IsDistinctOrderBySupported           = false;
			SqlProviderFlags.IsOrderByAggregateFunctionsSupported = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported     = false;
			SqlProviderFlags.IsUpdateFromSupported                = false;

			SetCharFieldToType<char>("NChar", DataTools.GetCharExpression);

			SetCharField("NChar",    (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("NVarChar", (r,i) => r.GetString(i).TrimEnd(' '));

			_sqlOptimizer = new SqlCeSqlOptimizer(SqlProviderFlags);
		}

		#region Overrides

		public override TableOptions SupportedTableOptions => TableOptions.None;

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
				var param = TryGetProviderParameter(parameter, dataConnection.MappingSchema);
				if (param != null)
				{
					Adapter.SetDbType(param, type.Value);
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
				case DataType.Char       : 
				case DataType.NChar      : parameter.DbType = DbType.String;            return;
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

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new SqlCeBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SqlCeTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if NATIVE_ASYNC
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new SqlCeBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SqlCeTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
