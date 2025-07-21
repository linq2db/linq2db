using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Metrics;

using AsyncDisposableWrapper = LinqToDB.Metrics.ActivityService.AsyncDisposableWrapper;

namespace LinqToDB.Internal.Async
{
	/// <summary>
	/// Basic <see cref="IAsyncDbTransaction"/> implementation with fallback to synchronous operations if corresponding functionality
	/// missing from <see cref="DbTransaction"/>.
	/// </summary>
	public class AsyncDbTransaction : IAsyncDbTransaction
	{
		protected internal AsyncDbTransaction(DbTransaction transaction)
		{
			Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
		}

		internal DataConnection? DataConnection { get; set; }

		public DbTransaction Transaction { get; }

		public virtual void Commit  ()
		{
			using var a = ActivityService.Start(ActivityID.TransactionCommit)?.AddQueryInfo(DataConnection, DataConnection?.CurrentConnection, null);
			Transaction.Commit();
		}

		public virtual void Rollback()
		{
			using var a = ActivityService.Start(ActivityID.TransactionRollback)?.AddQueryInfo(DataConnection, DataConnection?.CurrentConnection, null);
			Transaction.Rollback();
		}

		public virtual Task CommitAsync(CancellationToken cancellationToken)
		{
#if ADO_ASYNC
			var a = ActivityService.StartAndConfigureAwait(ActivityID.TransactionCommitAsync)?.AddQueryInfo(DataConnection, DataConnection?.CurrentConnection, null);

			if (a is null)
				return Transaction.CommitAsync(cancellationToken);

			return CallAwaitUsing(a, Transaction, cancellationToken);

			static async Task CallAwaitUsing(AsyncDisposableWrapper activity, DbTransaction transaction, CancellationToken token)
			{
				await using (activity)
					await transaction.CommitAsync(token).ConfigureAwait(false);
			}
#else
			using var a = ActivityService.Start(ActivityID.TransactionCommitAsync)?.AddQueryInfo(DataConnection, DataConnection?.CurrentConnection, null);

			Transaction.Commit();
			return Task.CompletedTask;
#endif
		}

		public virtual Task RollbackAsync(CancellationToken cancellationToken)
		{
#if ADO_ASYNC
			var a = ActivityService.StartAndConfigureAwait(ActivityID.TransactionRollbackAsync)?.AddQueryInfo(DataConnection, DataConnection?.CurrentConnection, null);

			if (a is null)
				return Transaction.RollbackAsync(cancellationToken);

			return CallAwaitUsing(a, Transaction, cancellationToken);

			static async Task CallAwaitUsing(AsyncDisposableWrapper activity, DbTransaction transaction, CancellationToken token)
			{
				await using (activity)
					await transaction.RollbackAsync(token).ConfigureAwait(false);
			}
#else
			using var a = ActivityService.Start(ActivityID.TransactionRollbackAsync)?.AddQueryInfo(DataConnection, DataConnection?.CurrentConnection, null);

			Transaction.Rollback();
			return Task.CompletedTask;
#endif
		}

		#region IDisposable

		public virtual void Dispose()
		{
			using var _ = ActivityService.Start(ActivityID.TransactionDispose)?.AddQueryInfo(DataConnection, DataConnection?.CurrentConnection, null);
			Transaction.Dispose();
		}

		#endregion

		#region IAsyncDisposable
		public virtual ValueTask DisposeAsync()
		{
			var a = ActivityService.StartAndConfigureAwait(ActivityID.TransactionDisposeAsync)?.AddQueryInfo(DataConnection, DataConnection?.CurrentConnection, null);

			if (a is null)
				return Transaction.DisposeAsync();

			return CallAwaitUsing(a, Transaction);

			static async ValueTask CallAwaitUsing(AsyncDisposableWrapper activity, DbTransaction transaction)
			{
				await using (activity)
					await transaction.DisposeAsync().ConfigureAwait(false);
			}
		}
		#endregion
	}
}
