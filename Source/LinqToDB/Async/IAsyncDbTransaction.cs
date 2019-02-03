using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Async
{
	/// <summary>
	/// Asynchronous version of the <see cref="IDbTransaction"/> interface, allowing asynchronous operations, missing from <see cref="IDbTransaction"/>.
	/// </summary>
	[PublicAPI]
	public interface IAsyncDbTransaction : IDbTransaction
	{
		/// <summary>
		/// Commits transaction asynchonously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		Task CommitAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Rollbacks transaction asynchonously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		Task RollbackAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Disposes transaction asynchonously.
		/// </summary>
		/// <returns>Async operation task.</returns>
		Task DisposeAsync();

		/// <summary>
		/// Gets underlying transaction instance.
		/// </summary>
		IDbTransaction Unwrap { get; }

		/// <summary>
		/// Gets underlying instance of <see cref="IAsyncDbConnection"/>.
		/// </summary>
		IAsyncDbConnection AsyncConnection { get; }
	}
}
