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
	{
		/// <summary>
		/// Starts new transaction asynchronously for current connection with default isolation level.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Starts new transaction asynchronously for current connection with specified isolation level.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

		/// <summary>
		/// Closes current connection asynchonously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Async operation task.</returns>
		ValueTask CloseAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Opens current connection asynchonously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Async operation task.</returns>
		Task OpenAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Changes database for current connectin asynchonously.
		/// </summary>
		/// <param name="databaseName">Name of database to switch to.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Async operation task.</returns>
		Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Disposes connection asynchonously.
		/// </summary>
		/// <returns>Async operation task.</returns>
		ValueTask DisposeAsync();

		/// <summary>
		/// Gets underlying connection instance.
		/// </summary>
		IDbConnection Unwrap { get; }

		/// <summary>
		/// Returns cloned connection instance, if underlying provider support cloning or null otherwise.
		/// </summary>
		IAsyncDbConnection TryClone();
	}
}
