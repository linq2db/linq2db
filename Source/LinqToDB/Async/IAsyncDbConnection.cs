using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace LinqToDB.Async
{
	using Data.RetryPolicy;

	/// <summary>
	/// Wrapper over <see cref="DbConnection"/> instance which contains all operations that could have custom implementation like:
	/// <list type="bullet">
	/// <item><see cref="IRetryPolicy"/> support</item>
	/// <item>asynchronous operations, missing from <see cref="DbConnection"/> but provided by data provider implementation.</item>.
	/// </list>
	/// </summary>
	[PublicAPI]
	public interface IAsyncDbConnection : IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Gets underlying connection instance.
		/// </summary>
		DbConnection Connection { get; }

		/// <inheritdoc cref="DbConnection.ConnectionString"/>
		string ConnectionString { get; set; }

		/// <inheritdoc cref="DbConnection.State"/>
		ConnectionState State { get; }

		/// <inheritdoc cref="DbConnection.CreateCommand"/>
		DbCommand CreateCommand();

		/// <inheritdoc cref="DbConnection.Open"/>
		void Open();
		/// <inheritdoc cref="DbConnection.OpenAsync(CancellationToken)"/>
		Task OpenAsync(CancellationToken cancellationToken);

		/// <inheritdoc cref="DbConnection.Close"/>
		void Close();
		/// <summary>
		/// Closes current connection asynchonously.
		/// </summary>
		/// <returns>Async operation task.</returns>
		Task CloseAsync();

		/// <summary>
		/// Starts new transaction for current connection with default isolation level.
		/// </summary>
		/// <returns>Database transaction object.</returns>
		IAsyncDbTransaction BeginTransaction();

		/// <summary>
		/// Starts new transaction for current connection with specified isolation level.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		/// <returns>Database transaction object.</returns>
		IAsyncDbTransaction BeginTransaction(IsolationLevel isolationLevel);

		/// <summary>
		/// Starts new transaction asynchronously for current connection with default isolation level.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		ValueTask<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Starts new transaction asynchronously for current connection with specified isolation level.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken);
	}
}
