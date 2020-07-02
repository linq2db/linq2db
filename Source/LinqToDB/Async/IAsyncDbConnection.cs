using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Async
{
	/// <summary>
	/// Asynchronous version of the <see cref="IDbConnection"/> interface, allowing asynchronous operations, missing from <see cref="IDbConnection"/>.
	/// </summary>
	[PublicAPI]
	public interface IAsyncDbConnection : IDbConnection
#if !NET45 && !NET46
		, IAsyncDisposable
#endif
	{
		/// <summary>
		/// Starts new transaction asynchronously for current connection with default isolation level.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
#if !NET45 && !NET46
		ValueTask<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
#else
		Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
#endif

		/// <summary>
		/// Starts new transaction asynchronously for current connection with specified isolation level.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
#if !NET45 && !NET46
		ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
#else
		Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
#endif

		/// <summary>
		/// Closes current connection asynchonously.
		/// </summary>
		/// <returns>Async operation task.</returns>
		Task CloseAsync();

		/// <summary>
		/// Opens current connection asynchonously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Async operation task.</returns>
		Task OpenAsync(CancellationToken cancellationToken = default);

#if NET45 || NET46
		/// <summary>
		/// Disposes current connection asynchonously.
		/// </summary>
		/// <returns>Async operation task.</returns>
		Task DisposeAsync();
#endif

		/// <summary>
		/// Gets underlying connection instance.
		/// </summary>
		IDbConnection Connection { get; }

		/// <summary>
		/// Returns cloned connection instance, if underlying provider supports cloning or null otherwise.
		/// </summary>
		IAsyncDbConnection? TryClone();
	}
}
