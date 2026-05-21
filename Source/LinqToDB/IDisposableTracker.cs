using System;
using System.Collections.Generic;

using LinqToDB.Internal.Infrastructure;

namespace LinqToDB
{
	/// <summary>
	/// Tracker for lifetime-bound disposable resources owned by a data context — typically temp
	/// tables created by <see cref="LinqExtensions.AsQueryable{TElement}(System.Collections.Generic.IEnumerable{TElement},IDataContext,System.Linq.Expressions.Expression{System.Func{Linq.IAsQueryableBuilder{TElement},Linq.IAsQueryableExceptBuilder{TElement}}})"/>
	/// when the configure chain includes <c>UseTempTable(b =&gt; b.Threshold(N).DisposeWithConnection())</c>.
	/// A data context exposes its tracker via <see cref="IInfrastructure{T}"/>:
	/// <c>((IInfrastructure&lt;IDisposableTracker&gt;)dc).Instance</c>. Registered resources are
	/// released by the context's Dispose pipeline; soft <c>Close()</c> intentionally does not
	/// drain (a close-then-reuse cycle on the data context preserves user-owned temp tables).
	/// One bad resource does not block disposal of the others.
	/// </summary>
	public interface IDisposableTracker
	{
		/// <summary>
		/// Registers <paramref name="resource"/> with the tracker. Ownership of the lifetime
		/// transfers to the tracker until <see cref="Unregister"/> is called or the owning context closes.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
		void Register(IAsyncDisposable resource);

		/// <summary>
		/// Removes <paramref name="resource"/> from the tracked set. Idempotent — returns
		/// <see langword="false"/> if it was not registered. Does not dispose the resource.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
		bool Unregister(IAsyncDisposable resource);

		/// <summary>
		/// Snapshot of currently registered resources in registration order. The returned list is
		/// detached from internal state — subsequent <see cref="Register"/> / <see cref="Unregister"/>
		/// calls do not affect the returned array. The tracker itself is not thread-safe; callers
		/// must serialise access if <see cref="Register"/> / <see cref="Unregister"/> /
		/// <see cref="ActiveDisposables"/> run from multiple threads.
		/// </summary>
		IReadOnlyList<IAsyncDisposable> ActiveDisposables { get; }
	}
}
