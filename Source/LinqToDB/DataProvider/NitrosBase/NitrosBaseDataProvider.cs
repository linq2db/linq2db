using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.NitrosBase
{
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class NitrosBaseDataProvider : DynamicDataProviderBase<NitrosBaseProviderAdapter>
	{
		public NitrosBaseDataProvider()
			: this(ProviderName.NitrosBase, NitrosBaseMappingSchema.Instance)
		{
		}

		protected NitrosBaseDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema != NitrosBaseMappingSchema.Instance ? new NitrosBaseMappingSchema(mappingSchema) : mappingSchema, NitrosBaseProviderAdapter.GetInstance())
		{
			SqlProviderFlags.IsSubQueryOrderBySupported     = true;
			SqlProviderFlags.IsInsertOrUpdateSupported      = false;
			SqlProviderFlags.IsUpdateSetTableAliasSupported = false;
			SqlProviderFlags.IsCrossJoinSupported           = false;
			SqlProviderFlags.IsCountDistinctSupported       = true;
			SqlProviderFlags.IsUpdateFromSupported          = true;

			_sqlOptimizer = new NitrosBaseSqlOptimizer(SqlProviderFlags);
		}

		#region Overrides
		// TODO: temporary table structure/data visibility not clear from documentation
		public override TableOptions SupportedTableOptions => TableOptions.CheckExistence | TableOptions.IsTemporary;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema) => new NitrosBaseSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);

		readonly ISqlOptimizer _sqlOptimizer;
		public override ISqlOptimizer GetSqlOptimizer() => _sqlOptimizer;

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
