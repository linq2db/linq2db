using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Async
{
	using Tools;

	/// <summary>
	/// Basic <see cref="IAsyncDbTransaction"/> implementation with fallback to synchronous operations if corresponding functionality
	/// missing from <see cref="DbTransaction"/>.
	/// </summary>
	[PublicAPI]
	public class AsyncDbTransaction : IAsyncDbTransaction
	{
		protected internal AsyncDbTransaction(DbTransaction transaction)
		{
			Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
		}

		public DbTransaction Transaction { get; }

		public virtual void Commit  ()
		{
			using var a = ActivityService.Start(ActivityID.TransactionCommit);
			Transaction.Commit();
		}

		public virtual void Rollback()
		{
			using var a = ActivityService.Start(ActivityID.TransactionRollback);
			Transaction.Rollback();
		}

		public virtual Task CommitAsync(CancellationToken cancellationToken)
		{
#if NETSTANDARD2_1PLUS
			var a = ActivityService.StartAndConfigureAwait(ActivityID.TransactionCommitAsync);

			if (a is null)
				return Transaction.CommitAsync(cancellationToken);

			return CallAwaitUsing(a, Transaction, cancellationToken);

			static async Task CallAwaitUsing(IAsyncDisposable activity, DbTransaction transaction, CancellationToken token)
			{
				await using (activity)
					await transaction.CommitAsync(token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
#else
			Commit();
			return TaskEx.CompletedTask;
#endif
		}

		public virtual Task RollbackAsync(CancellationToken cancellationToken)
		{
#if NETSTANDARD2_1PLUS
			var a = ActivityService.StartAndConfigureAwait(ActivityID.TransactionRollbackAsync);

			if (a is null)
				return Transaction.RollbackAsync(cancellationToken);

			return CallAwaitUsing(a, Transaction, cancellationToken);

			static async Task CallAwaitUsing(IAsyncDisposable activity, DbTransaction transaction, CancellationToken token)
			{
				await using (activity)
					await transaction.RollbackAsync(token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
#else
			Rollback();
			return TaskEx.CompletedTask;
#endif
		}

		#region IDisposable

		public virtual void Dispose()
		{
			using var a = ActivityService.Start(ActivityID.TransactionDispose);
			Transaction.Dispose();
		}

		#endregion

		#region IAsyncDisposable
#if !NATIVE_ASYNC
		public virtual Task DisposeAsync()
		{
			Dispose();
			return TaskEx.CompletedTask;
		}
#else
		public virtual ValueTask DisposeAsync()
		{
			if (Transaction is IAsyncDisposable asyncDisposable)
			{
				var a = ActivityService.StartAndConfigureAwait(ActivityID.TransactionDisposeAsync);

				if (a is null)
					return asyncDisposable.DisposeAsync();

				return CallAwaitUsing(a, asyncDisposable);

				static async ValueTask CallAwaitUsing(IAsyncDisposable activity, IAsyncDisposable disposable)
				{
					await using (activity)
						await disposable.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				}
			}

			Dispose();
			return default;
		}
#endif
		#endregion
	}
}
