using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using LinqToDB.Internal.DataProvider.MySql.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.MySql
{
#pragma warning disable MA0048 // File name must match type name
	sealed class MySql57DataProviderMySqlData()        : MySqlDataProvider(ProviderName.MySql57MySqlData,        MySqlVersion.MySql57,   MySqlProvider.MySqlData     ) { }
	sealed class MySql57DataProviderMySqlConnector()   : MySqlDataProvider(ProviderName.MySql57MySqlConnector,   MySqlVersion.MySql57,   MySqlProvider.MySqlConnector) { }
	sealed class MySql80DataProviderMySqlData()        : MySqlDataProvider(ProviderName.MySql80MySqlData,        MySqlVersion.MySql80,   MySqlProvider.MySqlData     ) { }
	sealed class MySql80DataProviderMySqlConnector()   : MySqlDataProvider(ProviderName.MySql80MySqlConnector,   MySqlVersion.MySql80,   MySqlProvider.MySqlConnector) { }
	sealed class MariaDB10DataProviderMySqlData()      : MySqlDataProvider(ProviderName.MariaDB10MySqlData,      MySqlVersion.MariaDB10, MySqlProvider.MySqlData     ) { }
	sealed class MariaDB10DataProviderMySqlConnector() : MySqlDataProvider(ProviderName.MariaDB10MySqlConnector, MySqlVersion.MariaDB10, MySqlProvider.MySqlConnector) { }
#pragma warning restore MA0048 // File name must match type name

	public abstract class MySqlDataProvider : DynamicDataProviderBase<MySqlProviderAdapter>
	{
		protected MySqlDataProvider(string name, MySqlVersion version, MySqlProvider provider)
			: base(name, GetMappingSchema(provider, version), MySqlProviderAdapter.GetInstance(provider))
		{
			Provider = provider;
			Version  = version;

			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = version > MySqlVersion.MySql57;
			SqlProviderFlags.IsUpdateFromSupported             = false;
			SqlProviderFlags.IsNamingQueryBlockSupported       = true;
			SqlProviderFlags.IsDistinctFromSupported           = true;
			SqlProviderFlags.SupportsPredicatesComparison      = true;
			SqlProviderFlags.IsAllSetOperationsSupported       = version > MySqlVersion.MySql57;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = version > MySqlVersion.MySql57;
			// MariaDB still lacking it
			// https://jira.mariadb.org/browse/MDEV-6373
			// https://jira.mariadb.org/browse/MDEV-19078
			SqlProviderFlags.IsApplyJoinSupported              = version == MySqlVersion.MySql80;
			SqlProviderFlags.IsCrossApplyJoinSupportsCondition = version == MySqlVersion.MySql80;
			SqlProviderFlags.IsOuterApplyJoinSupportsCondition = version == MySqlVersion.MySql80;
			SqlProviderFlags.IsWindowFunctionsSupported        = Version >= MySqlVersion.MySql80;

			SqlProviderFlags.IsSubqueryWithParentReferenceInJoinConditionSupported = false;
			SqlProviderFlags.SupportedCorrelatedSubqueriesLevel                    = 1;
			SqlProviderFlags.RowConstructorSupport                                 = RowFeature.Equality | RowFeature.Comparisons | RowFeature.CompareToSelect | RowFeature.In;

			SqlProviderFlags.IsUpdateTakeSupported     = true;

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
#if SUPPORTS_DATEONLY
			if (Adapter.GetTimeOnlyMethodName != null) SetProviderField<TimeOnly>(Adapter.GetTimeOnlyMethodName, Adapter.DataReaderType);
			if (Adapter.GetDateOnlyMethodName != null) SetProviderField<DateOnly>(Adapter.GetDateOnlyMethodName, Adapter.DataReaderType);
#endif
		}

		public MySqlVersion  Version  { get; }
		public MySqlProvider Provider { get; }

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new MySqlMemberTranslator();
		}

		public override ISchemaProvider GetSchemaProvider()
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
				return new MySql57SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
			if (Version == MySqlVersion.MySql80)
				return new MySql80SqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);

			return new MySqlSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		private static MappingSchema GetMappingSchema(MySqlProvider provider, MySqlVersion version)
		{
			return (provider, version) switch
			{
				(MySqlProvider.MySqlData, MySqlVersion.MySql57)        => new MySqlMappingSchema.MySqlData57MappingSchema(),
				(MySqlProvider.MySqlData, MySqlVersion.MySql80)        => new MySqlMappingSchema.MySqlData80MappingSchema(),
				(MySqlProvider.MySqlData, MySqlVersion.MariaDB10)      => new MySqlMappingSchema.MySqlDataMariaDB10MappingSchema(),
				(MySqlProvider.MySqlConnector, MySqlVersion.MySql57)   => new MySqlMappingSchema.MySqlConnector57MappingSchema(),
				(MySqlProvider.MySqlConnector, MySqlVersion.MySql80)   => new MySqlMappingSchema.MySqlConnector80MappingSchema(),
				(MySqlProvider.MySqlConnector, MySqlVersion.MariaDB10) => new MySqlMappingSchema.MySqlConnectorMariaDB10MappingSchema(),
				_                                                      => new MySqlMappingSchema.MySqlConnector57MappingSchema(),
			};
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
		{
			return _sqlOptimizer;
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, in DbDataType dataType, object? value)
		{
			// mysql.data bugs workaround
			if (Adapter.MySqlDecimalType != null && Adapter.MySqlDecimalGetter != null && value?.GetType() == Adapter.MySqlDecimalType)
			{
				value    = Adapter.MySqlDecimalGetter(value);
				// yep, MySql.Data just crash here on large decimals even for string value as it tries to convert it back
				// to decimal for DataType.Decimal just to convert it back to string ¯\_(ツ)_/¯
				// https://github.com/mysql/mysql-connector-net/blob/8.0/MySQL.Data/src/Types/MySqlDecimal.cs#L103
				base.SetParameter(dataConnection, parameter, name, dataType.WithDataType(DataType.VarChar), value);
				return;
			}

#if SUPPORTS_DATEONLY
			if (!Adapter.IsDateOnlySupported && value is DateOnly d)
			{
				value = d.ToDateTime(TimeOnly.MinValue);
			}
#endif

			base.SetParameter(dataConnection, parameter, name, in dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, in DbDataType dataType)
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
