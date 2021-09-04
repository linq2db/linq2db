using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.NitrosBase
{
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	// data provider is used for all connections and must be thread-safe
	public class NitrosBaseDataProvider : DynamicDataProviderBase<NitrosBaseProviderAdapter>
	{
		public NitrosBaseDataProvider()
			: this(ProviderName.NitrosBase, NitrosBaseMappingSchema.Instance)
		{
		}

		protected NitrosBaseDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema != NitrosBaseMappingSchema.Instance ? new NitrosBaseMappingSchema(mappingSchema) : mappingSchema, NitrosBaseProviderAdapter.GetInstance())
		{
			// TODO: setup sql flags here

			_sqlOptimizer = new NitrosBaseSqlOptimizer(SqlProviderFlags);
		}

		#region Overrides
		// TODO: specify flags for temporary tables and conditional table management support by database
		public override TableOptions SupportedTableOptions => TableOptions.None;

		// SQL builder is not thread-safe and we always create new instance on request
		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new NitrosBaseSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		// sql optimizer must be thread-safe and we must use shared instance
		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer() => _sqlOptimizer;

		// schema provider is not thread-safe
		// if database doesn't expose schema information this method should throw exception
		public override ISchemaProvider GetSchemaProvider() => new NitrosBaseSchemaProvider();
		#endregion

		#region BulkCopy
		// we can already implement bulk copy methods as they just pass data to real implementation
		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new NitrosBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? NitrosBaseTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new NitrosBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? NitrosBaseTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if NATIVE_ASYNC
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new NitrosBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? NitrosBaseTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif
		#endregion
	}
}
