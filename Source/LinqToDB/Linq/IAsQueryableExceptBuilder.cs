using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Configuration stage exposed after the rendering mode has been chosen via
	/// <see cref="IAsQueryableBuilder{T}.Parameterize"/> or <see cref="IAsQueryableBuilder{T}.Inline"/>.
	/// Allows per-member overrides via <see cref="Except"/>.
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
		/// Switches rendering from inline <c>VALUES</c> to a real temporary table when the source has more
		/// than <paramref name="threshold"/> rows at query-execute time. The temp table is created on first
		/// execution, populated via <c>BulkCopy</c> / <c>BulkCopyAsync</c> (matching the execute path), and
		/// dropped immediately after the query completes — unless <see cref="DisposeWithConnection"/> is also
		/// chained, in which case the table's lifetime extends to the surrounding <see cref="IDataContext"/>.
		/// Below the threshold the existing inline <c>VALUES</c> path is used. Calling this method twice in
		/// the same chain is a configuration error.
		/// Providers that don't support session-scoped temp tables created at execute time without
		/// elevated privileges (Oracle's <c>GLOBAL TEMPORARY TABLE</c>, Firebird, Access, etc.) silently
		/// ignore this opt-in and fall through to inline <c>VALUES</c> — gated by
		/// <c>SqlProviderFlags.IsRuntimeTempTableCreationSupported</c>.
		/// </summary>
		IAsQueryableExceptBuilder<T> UseTempTable(int threshold);

		/// <summary>
		/// Extends the lifetime of a <see cref="UseTempTable"/>-created temp table to the surrounding
		/// <see cref="IDataContext"/>. The table is created once on first execution and reused across
		/// subsequent executions of the same <see cref="System.Linq.IQueryable{T}"/>; it is dropped when
		/// the data context closes or disposes. Requires the context to expose an
		/// <see cref="IDisposableTracker"/> via
		/// <see cref="LinqToDB.Internal.Infrastructure.IInfrastructure{T}"/> (<c>DataConnection</c> and
		/// <c>DataContext</c> both do). Data is captured at first execution and not refreshed across
		/// subsequent executions — drop this call if you want fresh data per execution. Has no effect
		/// unless <see cref="UseTempTable"/> is also chained, and is consequently also a no-op on
		/// providers that silently drop <see cref="UseTempTable"/> (those without
		/// <c>SqlProviderFlags.IsRuntimeTempTableCreationSupported</c>) — the inline-<c>VALUES</c>
		/// fallback there isn't owned by the data context and has nothing to track.
		/// </summary>
		IAsQueryableExceptBuilder<T> DisposeWithConnection();
	}
}
