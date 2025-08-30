using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Async
{
	/// <summary>
	/// Wrapper over <see cref="DbTransaction"/> instance with asynchronous operations, missing from <see cref="DbTransaction"/>.
	/// Includes only operations, used by Linq To DB.
	/// </summary>
	interface IAsyncDbTransaction : IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Gets underlying transaction instance.
		/// </summary>
		DbTransaction Transaction { get; }

		/// <inheritdoc cref="DbTransaction.Commit"/>
		void Commit();
		/// <inheritdoc cref="DbTransaction.Rollback()"/>
		void Rollback();

		/// <summary>
		/// Commits transaction asynchronously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		Task CommitAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Rollbacks transaction asynchronously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		Task RollbackAsync(CancellationToken cancellationToken);
	}
}
