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

			base.SetParameter(dataConnection, parameter, name, dataType, value);
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
