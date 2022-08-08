using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;

	class MySqlDataProviderMySqlOfficial  : MySqlDataProvider { public MySqlDataProviderMySqlOfficial()  : base(ProviderName.MySqlOfficial)  {} }
	class MySqlDataProviderMySqlConnector : MySqlDataProvider { public MySqlDataProviderMySqlConnector() : base(ProviderName.MySqlConnector) {} }

	public abstract class MySqlDataProvider : DynamicDataProviderBase<MySqlProviderAdapter>
	{
		protected MySqlDataProvider(string name)
			: base(name, GetMappingSchema(name), MySqlProviderAdapter.GetInstance(name))
		{
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;
			SqlProviderFlags.IsNamingQueryBlockSupported       = true;
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
				SetProviderField(typeof(DateTimeOffset), Adapter.GetDateTimeOffsetMethodName, Adapter.DataReaderType);
				SetToTypeField  (typeof(DateTimeOffset), Adapter.GetDateTimeOffsetMethodName, Adapter.DataReaderType);
			}

			SetProviderField(Adapter.MySqlDateTimeType, Adapter.GetMySqlDateTimeMethodName, Adapter.DataReaderType);
			SetToTypeField  (Adapter.MySqlDateTimeType, Adapter.GetMySqlDateTimeMethodName, Adapter.DataReaderType);

			if (Adapter.GetTimeSpanMethodName != null) SetProviderField(typeof(TimeSpan), Adapter.GetTimeSpanMethodName, Adapter.DataReaderType);
			if (Adapter.GetSByteMethodName    != null) SetProviderField(typeof(sbyte)   , Adapter.GetSByteMethodName   , Adapter.DataReaderType);
			if (Adapter.GetUInt16MethodName   != null) SetProviderField(typeof(ushort)  , Adapter.GetUInt16MethodName  , Adapter.DataReaderType);
			if (Adapter.GetUInt32MethodName   != null) SetProviderField(typeof(uint)    , Adapter.GetUInt32MethodName  , Adapter.DataReaderType);
			if (Adapter.GetUInt64MethodName   != null) SetProviderField(typeof(ulong)   , Adapter.GetUInt64MethodName  , Adapter.DataReaderType);
#if NET6_0_OR_GREATER
			if (Adapter.GetTimeOnlyMethodName != null) SetProviderField(typeof(TimeOnly), Adapter.GetTimeOnlyMethodName, Adapter.DataReaderType);
			if (Adapter.GetDateOnlyMethodName != null) SetProviderField(typeof(DateOnly), Adapter.GetDateOnlyMethodName, Adapter.DataReaderType);
#endif
		}

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

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new MySqlSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		private static MappingSchema GetMappingSchema(string name)
		{
			return name switch
			{
				ProviderName.MySqlConnector => new MySqlMappingSchema.MySqlConnectorMappingSchema(),
				_                           => new MySqlMappingSchema.MySqlOfficialMappingSchema(),
			};
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
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

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MySqlBulkCopy(this).BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? MySqlTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MySqlBulkCopy(this).BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? MySqlTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if NATIVE_ASYNC
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MySqlBulkCopy(this).BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? MySqlTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
