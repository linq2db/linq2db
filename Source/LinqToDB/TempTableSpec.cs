using LinqToDB.Data;

namespace LinqToDB
{
	/// <summary>
	/// Snapshot of a single temp-table configuration scope (<see cref="Threshold"/> +
	/// <see cref="DisposeWithConnection"/> + <see cref="BulkCopyOptions"/>). Produced by
	/// <see cref="ITempTableConfigBuilder"/>'s setters; consumed by the LINQ translator at
	/// AST-build time. Used both as the per-call configuration captured from the AsQueryable
	/// chain and as the global default carried by <see cref="TempTableOptions"/>'s
	/// <c>LocalCollections</c> / <c>Contains</c> slots.
	/// </summary>
	/// <param name="Threshold">
	/// Materialise into a real temp table when the source row count exceeds this value;
	/// <see langword="null"/> means "no threshold set" which disables the temp-table strategy
	/// for this scope.
	/// </param>
	/// <param name="DisposeWithConnection">
	/// Tie the temp table's lifetime to the surrounding <see cref="IDataContext"/> instead of
	/// the single query execution.
	/// </param>
	/// <param name="BulkCopyOptions">
	/// Options forwarded to the <c>TempTable&lt;T&gt;</c> bulk-copy call when the temp table is
	/// materialised; <see langword="null"/> means "use provider default".
	/// </param>
	public sealed record TempTableSpec(
		int?             Threshold,
		bool             DisposeWithConnection,
		BulkCopyOptions? BulkCopyOptions);
}
