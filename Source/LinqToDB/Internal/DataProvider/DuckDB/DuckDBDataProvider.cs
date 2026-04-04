using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider.DuckDB;
using LinqToDB.Internal.DataProvider.DuckDB.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBDataProvider : DynamicDataProviderBase<DuckDBProviderAdapter>
	{
		public DuckDBDataProvider()
			: this(ProviderName.DuckDB, DuckDBMappingSchema.Instance)
		{
		}

		protected DuckDBDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema, DuckDBProviderAdapter.Instance)
		{
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsUnionAllOrderBySupported        = true;
			SqlProviderFlags.IsAllSetOperationsSupported       = true;
			SqlProviderFlags.IsInsertOrUpdateSupported         = true;
			SqlProviderFlags.IsApplyJoinSupported              = true;
			SqlProviderFlags.IsCrossApplyJoinSupportsCondition = true;
			SqlProviderFlags.IsOuterApplyJoinSupportsCondition = true;
			SqlProviderFlags.IsDistinctFromSupported           = true;
			SqlProviderFlags.SupportsPredicatesComparison      = true;

			SqlProviderFlags.DefaultMultiQueryIsolationLevel = System.Data.IsolationLevel.Snapshot;

			SqlProviderFlags.RowConstructorSupport =
				RowFeature.Equality        | RowFeature.Comparisons |
				RowFeature.CompareToSelect | RowFeature.In          |
				RowFeature.IsNull          | RowFeature.Update      |
				RowFeature.UpdateLiteral   | RowFeature.Between;

			SetCharFieldToType<char>("VARCHAR", DataTools.GetCharExpression);
			SetCharField            ("VARCHAR", (r,i) => r.GetString(i).TrimEnd(' '));

			_sqlOptimizer = new DuckDBSqlOptimizer(SqlProviderFlags);
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                  |
			TableOptions.IsLocalTemporaryStructure    |
			TableOptions.IsLocalTemporaryData         |
			TableOptions.CreateIfNotExists            |
			TableOptions.DropIfExists;

		protected override IMemberTranslator CreateMemberTranslator() => new DuckDBMemberTranslator();

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new DuckDBSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		private readonly ISqlOptimizer _sqlOptimizer;
		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => _sqlOptimizer;

		public override ISchemaProvider GetSchemaProvider() => new DuckDBSchemaProvider();

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			if (value is char chr)
				value = chr.ToString();

			if (value is sbyte sb)
			{
				dataType = dataType.WithDataType(DataType.Int16);
				value    = (short)sb;
			}

			if (value is TimeSpan ts)
			{
				// DuckDB.NET doesn't natively handle TimeSpan as INTERVAL parameter
				value = ts.TotalDays >= 1 || ts.TotalDays <= -1
					? $"{(int)ts.TotalDays} days {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}"
					: $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			// DuckDB.NET requires explicit DbType to avoid treating parameters as STRING_LITERAL
			switch (dataType.DataType)
			{
				case DataType.SByte    :
				case DataType.Int16    : parameter.DbType = System.Data.DbType.Int16    ; return;
				case DataType.Int32    : parameter.DbType = System.Data.DbType.Int32    ; return;
				case DataType.Int64    : parameter.DbType = System.Data.DbType.Int64    ; return;
				case DataType.Byte     : parameter.DbType = System.Data.DbType.Byte     ; return;
				case DataType.UInt16   : parameter.DbType = System.Data.DbType.UInt16   ; return;
				case DataType.UInt32   : parameter.DbType = System.Data.DbType.UInt32   ; return;
				case DataType.UInt64   : parameter.DbType = System.Data.DbType.UInt64   ; return;
				case DataType.Single   : parameter.DbType = System.Data.DbType.Single   ; return;
				case DataType.Double   : parameter.DbType = System.Data.DbType.Double   ; return;
				case DataType.Decimal  :
				case DataType.Money    :
				case DataType.SmallMoney:
				case DataType.VarNumeric: parameter.DbType = System.Data.DbType.Decimal ; return;
				case DataType.Boolean  : parameter.DbType = System.Data.DbType.Boolean  ; return;
				case DataType.Guid     : parameter.DbType = System.Data.DbType.Guid     ; return;
				case DataType.Date     : parameter.DbType = System.Data.DbType.Date     ; return;
				case DataType.DateTime :
				case DataType.DateTime2: parameter.DbType = System.Data.DbType.DateTime ; return;
				case DataType.DateTimeOffset: parameter.DbType = System.Data.DbType.DateTimeOffset; return;
				case DataType.Time     : parameter.DbType = System.Data.DbType.Time     ; return;
				case DataType.Binary   :
				case DataType.VarBinary: parameter.DbType = System.Data.DbType.Binary   ; return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		#region BulkCopy

		private static readonly DuckDBBulkCopy _bulkCopy = new ();

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return _bulkCopy.BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(DuckDBOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(DuckDBOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(DuckDBOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
