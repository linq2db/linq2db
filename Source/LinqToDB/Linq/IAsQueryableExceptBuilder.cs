using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Configuration stage exposed after the rendering mode has been chosen via
	/// <see cref="IAsQueryableBuilder{T}.Parameterize"/> or <see cref="IAsQueryableBuilder{T}.Inline"/>.
	/// Allows per-member overrides via <see cref="Except"/> and per-call temp-table opt-in via
	/// <see cref="UseTempTable(int)"/> / <see cref="UseTempTable(Func{ITempTableConfigBuilder, ITempTableConfigBuilder})"/>.
	/// </summary>
	/// <typeparam name="T">Element type of the source enumerable.</typeparam>
	public interface IAsQueryableExceptBuilder<T>
	{
		/// <summary>
		/// Flips the chosen mode for the listed members: under <see cref="IAsQueryableBuilder{T}.Parameterize"/>
		/// the listed members are inlined as literals; under <see cref="IAsQueryableBuilder{T}.Inline"/> they
		/// are rendered as parameters. Selectors are member-access chains rooted at the row, e.g.
		/// <c>p =&gt; p.Id</c> or <c>p =&gt; p.Address.Zip</c>. An empty member list is a no-op.
		/// </summary>
		IAsQueryableExceptBuilder<T> Except(params Expression<Func<T, object?>>[] members);

		/// <summary>
		/// Convenience overload — equivalent to <c>UseTempTable(b =&gt; b.Threshold(threshold))</c>.
		/// Switches rendering from inline <c>VALUES</c> to a real temporary table when the source has
		/// more than <paramref name="threshold"/> rows at query-execute time. The temp table is created
		/// on first execution, populated via <c>BulkCopy</c> / <c>BulkCopyAsync</c> (matching the
		/// execute path), and dropped immediately after the query completes. Below the threshold the
		/// existing inline <c>VALUES</c> path is used. Calling any UseTempTable overload twice in the
		/// same chain is a configuration error.
		/// Providers that don't support session-scoped temp tables created at execute time without
		/// elevated privileges (Oracle's <c>GLOBAL TEMPORARY TABLE</c>, Firebird, Access, etc.)
		/// silently ignore this opt-in and fall through to inline <c>VALUES</c> — gated by
		/// <c>SqlProviderFlags.IsRuntimeTempTableCreationSupported</c>.
		/// </summary>
		IAsQueryableExceptBuilder<T> UseTempTable(int threshold);

		/// <summary>
		/// Configures temp-table materialisation via the shared <see cref="ITempTableConfigBuilder"/>:
		/// <c>UseTempTable(b =&gt; b.Threshold(100).ConfigureBulkCopy(bc =&gt; bc.WithMaxBatchSize(5000)).DisposeWithConnection())</c>.
		/// Per-call configuration; any field the chain sets explicitly wins over the matching
		/// <c>DataOptionsExtensions.UseTempTablesForLocalCollections</c> default — unset fields fall
		/// back to the DataOptions value. Calling any UseTempTable overload twice in the same chain
		/// is a configuration error.
		/// </summary>
		IAsQueryableExceptBuilder<T> UseTempTable(Func<ITempTableConfigBuilder, ITempTableConfigBuilder> configure);
	}
}
