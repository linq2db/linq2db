using System;

namespace LinqToDB
{
	/// <summary>
	/// Fluent builder for the <see cref="LinqToDB.Data.BulkCopyOptions"/> applied when
	/// <see cref="ITempTableConfigBuilder.ConfigureBulkCopy"/> configures the bulk-copy that
	/// populates an auto-created temp table. Exposes one entry method per
	/// <see cref="LinqToDB.Data.BulkCopyType"/> — pick the one matching the strategy you want
	/// and pass a configure lambda for any per-type tuning. If none of the three methods is
	/// called, the provider's <see cref="LinqToDB.Data.BulkCopyType.Default"/> applies.
	/// </summary>
	public interface ITempTableBulkCopyOptionsBuilder
	{
		/// <summary>
		/// Use the <see cref="LinqToDB.Data.BulkCopyType.RowByRow"/> strategy — one INSERT per row.
		/// Slowest path; primarily useful as a diagnostic when faster strategies misbehave.
		/// </summary>
		ITempTableBulkCopyOptionsBuilder UseRowByRow(Func<ITempTableBulkCopyTypeConfigBuilder, ITempTableBulkCopyTypeConfigBuilder>? configure = null);

		/// <summary>
		/// Use the <see cref="LinqToDB.Data.BulkCopyType.MultipleRows"/> strategy — one INSERT
		/// statement with many <c>VALUES</c> tuples per batch. The default for most providers.
		/// </summary>
		ITempTableBulkCopyOptionsBuilder UseMultiRows(Func<ITempTableBulkCopyTypeConfigBuilder, ITempTableBulkCopyTypeConfigBuilder>? configure = null);

		/// <summary>
		/// Use the <see cref="LinqToDB.Data.BulkCopyType.ProviderSpecific"/> strategy — the
		/// provider's native bulk-copy API (e.g. SqlBulkCopy on SQL Server, <c>COPY</c> on Npgsql).
		/// Fastest path on providers that expose it.
		/// </summary>
		ITempTableBulkCopyOptionsBuilder UseProviderSpecific(Func<ITempTableBulkCopyTypeConfigBuilder, ITempTableBulkCopyTypeConfigBuilder>? configure = null);
	}

}
