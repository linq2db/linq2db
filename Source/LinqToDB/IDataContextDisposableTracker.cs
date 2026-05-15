using System;
using System.Collections.Generic;

namespace LinqToDB
{
	/// <summary>
	/// Optional companion to <see cref="IDataContext"/> implemented by contexts that track
	/// lifetime-bound disposable resources. The most common case is temporary tables created by
	/// <see cref="LinqExtensions.AsQueryable{TElement}(System.Collections.Generic.IEnumerable{TElement},IDataContext,System.Linq.Expressions.Expression{System.Func{Linq.IAsQueryableBuilder{TElement},Linq.IAsQueryableExceptBuilder{TElement}}})"/>
	/// when <see cref="Linq.IAsQueryableExceptBuilder{T}.UseTempTable"/> is chained with
	/// <see cref="Linq.IAsQueryableExceptBuilder{T}.DisposeWithConnection"/>. Registered resources
	/// are released by the context's Close / Dispose pipeline; one bad resource does not block
	/// disposal of the others.
	/// </summary>
	public interface IDataContextDisposableTracker
	{
		/// <summary>
		/// Registers <paramref name="resource"/> with the context. The context owns the resource's
		/// lifetime until <see cref="Unregister"/> is called for it or the context closes.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
		/// <exception cref="ObjectDisposedException">The context has already been disposed.</exception>
		void Register(IAsyncDisposable resource);

		/// <summary>
		/// Removes <paramref name="resource"/> from the tracked set. Idempotent — returns
		/// <see langword="false"/> if it was not registered. Does not dispose the resource.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
		bool Unregister(IAsyncDisposable resource);

		/// <summary>
		/// Snapshot of currently registered resources in registration order. The returned list is
		/// detached from internal state and safe to enumerate concurrently with further registrations.
		/// </summary>
		IReadOnlyList<IAsyncDisposable> ActiveDisposables { get; }
	}
}
