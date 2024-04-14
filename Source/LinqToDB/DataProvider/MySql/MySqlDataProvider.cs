using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;

	sealed class MySql57DataProviderMySqlData()        : MySqlDataProvider(ProviderName.MySql57,   MySqlVersion.MySql57,   MySqlProvider.MySqlData     ) { }
	sealed class MySql57DataProviderMySqlConnector()   : MySqlDataProvider(ProviderName.MySql57,   MySqlVersion.MySql57,   MySqlProvider.MySqlConnector) { }
	sealed class MySql80DataProviderMySqlData()        : MySqlDataProvider(ProviderName.MySql80,   MySqlVersion.MySql80,   MySqlProvider.MySqlData     ) { }
	sealed class MySql80DataProviderMySqlConnector()   : MySqlDataProvider(ProviderName.MySql80,   MySqlVersion.MySql80,   MySqlProvider.MySqlConnector) { }
	sealed class MariaDB10DataProviderMySqlData()      : MySqlDataProvider(ProviderName.MariaDB10, MySqlVersion.MariaDB10, MySqlProvider.MySqlData     ) { }
	sealed class MariaDB10DataProviderMySqlConnector() : MySqlDataProvider(ProviderName.MariaDB10, MySqlVersion.MariaDB10, MySqlProvider.MySqlConnector) { }

	public abstract class MySqlDataProvider : DynamicDataProviderBase<MySqlProviderAdapter>
	{
		protected MySqlDataProvider(string name, MySqlVersion version, MySqlProvider provider)
			: this(name, version, MySqlProviderAdapter.GetInstance(provider == MySqlProvider.AutoDetect ? provider = MySqlProviderDetector.DetectProvider() : provider))
		{
			Provider = provider;
		}

		private MySqlDataProvider(string name, MySqlVersion version, MySqlProviderAdapter adapter)
			: base(name, GetMappingSchema(version, adapter.MappingSchema), adapter)
		{
			Version = version;

			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = version > MySqlVersion.MySql57;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;
			SqlProviderFlags.IsNamingQueryBlockSupported       = true;
			SqlProviderFlags.IsAllSetOperationsSupported       = version > MySqlVersion.MySql57;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = version > MySqlVersion.MySql57;
			SqlProviderFlags.IsApplyJoinSupported              = version > MySqlVersion.MySql57;
			SqlProviderFlags.RowConstructorSupport             = RowFeature.Equality | RowFeature.Comparisons | RowFeature.CompareToSelect | RowFeature.In;

			_sqlOptimizer = new MySqlSqlOptimizer(SqlProviderFlags);

			// configure provider-specific data readers
			if (Adapter.GetMySqlDecimalMethodName != null)
			{
				// SetProviderField is not needed for this type
				SetToTypeField(Adapter.MySqlDecimalType!, Adapter.GetMySqlDecimalMethodName, Adapter.DataReaderType);
			}

			if (Adapter.GetDateTimeOffsetMethodName != null)
			{
				SetProviderField<DateTimeOffset>(Adapter.GetDateTimeOffsetMethodName, Adapter.DataReaderType);
				SetToTypeField(typeof(DateTimeOffset), Adapter.GetDateTimeOffsetMethodName, Adapter.DataReaderType);
			}

			SetProviderField(Adapter.MySqlDateTimeType, Adapter.GetMySqlDateTimeMethodName, Adapter.DataReaderType);
			SetToTypeField  (Adapter.MySqlDateTimeType, Adapter.GetMySqlDateTimeMethodName, Adapter.DataReaderType);

			if (Adapter.GetTimeSpanMethodName != null) SetProviderField<TimeSpan>(Adapter.GetTimeSpanMethodName, Adapter.DataReaderType);
			if (Adapter.GetSByteMethodName    != null) SetProviderField<sbyte   >(Adapter.GetSByteMethodName   , Adapter.DataReaderType);
			if (Adapter.GetUInt16MethodName   != null) SetProviderField<ushort  >(Adapter.GetUInt16MethodName  , Adapter.DataReaderType);
			if (Adapter.GetUInt32MethodName   != null) SetProviderField<uint    >(Adapter.GetUInt32MethodName  , Adapter.DataReaderType);
			if (Adapter.GetUInt64MethodName   != null) SetProviderField<ulong   >(Adapter.GetUInt64MethodName  , Adapter.DataReaderType);
#if NET6_0_OR_GREATER
			if (Adapter.GetTimeOnlyMethodName != null) SetProviderField<TimeOnly>(Adapter.GetTimeOnlyMethodName, Adapter.DataReaderType);
			if (Adapter.GetDateOnlyMethodName != null) SetProviderField<DateOnly>(Adapter.GetDateOnlyMethodName, Adapter.DataReaderType);
#endif
		}

		public MySqlVersion  Version  { get; }
		public MySqlProvider Provider { get; }

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new MySqlSchemaProvider(this);
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary               |
			TableOptions.IsLocalTemporaryStructure |
			TableOptions.IsLocalTemporaryData      |
			TableOptions.CreateIfNotExists         |
			TableOptions.DropIfExists;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			if (Version == MySqlVersion.MySql57)
			{
				return new MySql57SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
			}

			return new MySqlSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		private static MappingSchema GetMappingSchema(MySqlVersion version, MappingSchema adapterSchema)
		{
			return version switch
			{
				MySqlVersion.MySql57   => new MySqlMappingSchema.MySql57MappingSchema  (adapterSchema),
				MySqlVersion.MySql80   => new MySqlMappingSchema.MySql80MappingSchema  (adapterSchema),
				MySqlVersion.MariaDB10 => new MySqlMappingSchema.MariaDB10MappingSchema(adapterSchema),
				_                      => new MySqlMappingSchema.MySql57MappingSchema  (adapterSchema),
			};
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
		{
			return _sqlOptimizer;
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			// mysql.data bugs workaround
			if (Adapter.MySqlDecimalType != null && Adapter.MySqlDecimalGetter != null && value?.GetType() == Adapter.MySqlDecimalType)
			{
				value    = Adapter.MySqlDecimalGetter(value);
				// yep, MySql.Data just crash here on large decimals even for string value as it tries to convert it back
				// to decimal for DataType.Decimal just to convert it back to string ¯\_(ツ)_/¯
				// https://github.com/mysql/mysql-connector-net/blob/8.0/MySQL.Data/src/Types/MySqlDecimal.cs#L103
				dataType = dataType.WithDataType(DataType.VarChar);
			}

#if NET6_0_OR_GREATER
			if (!Adapter.IsDateOnlySupported && value is DateOnly d)
			{
				value = d.ToDateTime(TimeOnly.MinValue);
			}
#endif

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			// VarNumeric - mysql.data trims fractional part
			// Date/DateTime2 - mysql.data trims time part
			switch (dataType.DataType)
			{
				case DataType.VarNumeric: parameter.DbType = DbType.Decimal;  return;
				case DataType.Date:
				case DataType.DateTime2 : parameter.DbType = DbType.DateTime; return;
				case DataType.BitArray  : parameter.DbType = DbType.UInt64;   return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MySqlBulkCopy(this).BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(MySqlOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MySqlBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(MySqlOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MySqlBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(MySqlOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
