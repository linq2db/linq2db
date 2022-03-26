using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;
	using Mapping;
	using Common;
	using SchemaProvider;
	using SqlProvider;
	using Extensions;
	
	public class SybaseDataProvider : DynamicDataProviderBase<SybaseProviderAdapter>
	{
		#region Init

		public SybaseDataProvider(string name)
			: base(name, MappingSchemaInstance.Get(name), SybaseProviderAdapter.GetInstance(name))
		{
			SqlProviderFlags.AcceptsTakeAsParameter           = false;
			SqlProviderFlags.IsSkipSupported                  = false;
			SqlProviderFlags.IsSubQueryTakeSupported          = false;
			//SqlProviderFlags.IsCountSubQuerySupported       = false;
			SqlProviderFlags.CanCombineParameters             = false;
			SqlProviderFlags.IsSybaseBuggyGroupBy             = true;
			SqlProviderFlags.IsCrossJoinSupported             = false;
			SqlProviderFlags.IsSubQueryOrderBySupported       = false;
			SqlProviderFlags.IsDistinctOrderBySupported       = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported = false;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("char",  DataTools.GetCharExpression);
			SetCharFieldToType<char>("nchar", DataTools.GetCharExpression);

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1900, 1, 1));
			SetField<IDataReader,DateTime>("time", (r,i) => GetDateTimeAsTime(r.GetDateTime(i)));

			_sqlOptimizer = new SybaseSqlOptimizer(SqlProviderFlags);
		}

		static DateTime GetDateTimeAsTime(DateTime value)
		{
			if (value.Year == 1900 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		#endregion

		#region Overrides

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			type = base.ConvertParameterType(type, dataType);

			// native client BulkCopy cannot stand nullable types and enums
			// AseBulkManager.IsWrongType
			if (Name == ProviderName.Sybase)
			{
				type = type.ToNullableUnderlying();
				if (type == typeof(char) || type == typeof(Guid))
					type = typeof(string);
				else if (type == typeof(TimeSpan))
					type = typeof(DateTime);
			}

			return type;
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsLocalTemporaryStructure  |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryData       |
			TableOptions.IsGlobalTemporaryData      |
			TableOptions.CreateIfNotExists          |
			TableOptions.DropIfExists;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new SybaseSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema NativeMappingSchema  = new SybaseMappingSchema.NativeMappingSchema();
			public static readonly MappingSchema ManagedMappingSchema = new SybaseMappingSchema.ManagedMappingSchema();

			public static MappingSchema Get(string name) => name == ProviderName.Sybase ? NativeMappingSchema : ManagedMappingSchema;
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SybaseSchemaProvider(this);
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.SByte      :
					dataType = dataType.WithDataType(DataType.Int16);
					if (value is sbyte sbyteValue)
						value = (short)sbyteValue;
					break;

				case DataType.Time       :
					if (value is TimeSpan ts) value = new DateTime(1900, 1, 1) + ts;
					break;

				case DataType.Xml        :
					dataType = dataType.WithDataType(DataType.NVarChar);
						 if (value is XDocument      ) value = value.ToString();
					else if (value is XmlDocument xml) value = xml.InnerXml;
					break;

				case DataType.Guid       :
					if (value != null)
						value = value.ToString();
					dataType = dataType.WithDataType(DataType.Char);
					parameter.Size = 36;
					break;

				case DataType.Undefined  :
					if (value == null)
						dataType = dataType.WithDataType(DataType.Char);
					break;
				case DataType.Char       :
				case DataType.NChar      :
					if (Name == ProviderName.Sybase)
						if (value is char)
							value = value.ToString();
					break;
			}

			base.SetParameter(dataConnection, parameter, "@" + name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			SybaseProviderAdapter.AseDbType? type = null;

			switch (dataType.DataType)
			{
				case DataType.Text          : type = SybaseProviderAdapter.AseDbType.Text;             break;
				case DataType.NText         : type = SybaseProviderAdapter.AseDbType.Unitext;          break;
				case DataType.Blob          :
				case DataType.VarBinary     : type = SybaseProviderAdapter.AseDbType.VarBinary;        break;
				case DataType.Image         : type = SybaseProviderAdapter.AseDbType.Image;            break;
				case DataType.SmallMoney    : type = SybaseProviderAdapter.AseDbType.SmallMoney;       break;
				case DataType.SmallDateTime : type = SybaseProviderAdapter.AseDbType.SmallDateTime;    break;
				case DataType.Timestamp     : type = SybaseProviderAdapter.AseDbType.TimeStamp;        break;
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
				// fallback types
				case DataType.Text          : parameter.DbType = DbType.AnsiString; break;
				case DataType.NText         : parameter.DbType = DbType.String;     break;
				case DataType.Timestamp     :
				case DataType.Image         : parameter.DbType = DbType.Binary;     break;
				case DataType.SmallMoney    : parameter.DbType = DbType.Currency;   break;
				case DataType.SmallDateTime : parameter.DbType = DbType.DateTime;   break;

				case DataType.VarNumeric    : parameter.DbType = DbType.Decimal;    break;
				case DataType.Binary        : parameter.DbType = DbType.Binary;     break;
				case DataType.Money         : parameter.DbType = DbType.Currency;   break;
				case DataType.DateTime2     : parameter.DbType = DbType.DateTime;   break;
				default                     :
					base.SetParameterType(dataConnection, parameter, dataType);     break;
			}
		}

		#endregion

		#region BulkCopy

		SybaseBulkCopy? _bulkCopy;

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (_bulkCopy == null)
				_bulkCopy = new SybaseBulkCopy(this);

			return _bulkCopy.BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SybaseTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (_bulkCopy == null)
				_bulkCopy = new SybaseBulkCopy(this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SybaseTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if NATIVE_ASYNC
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (_bulkCopy == null)
				_bulkCopy = new SybaseBulkCopy(this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SybaseTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
