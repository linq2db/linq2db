using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using AsyncDisposableWrapper = LinqToDB.Tools.ActivityService.AsyncDisposableWrapper;

namespace LinqToDB.Async
{
	using Data;
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

		internal DataConnection? DataConnection { get; set; }

		public DbTransaction Transaction { get; }

		public virtual void Commit  ()
		{
			using var a = ActivityService.Start(ActivityID.TransactionCommit);
			if (a != null && DataConnection != null)
				a.AddQueryInfo(DataConnection, DataConnection.Connection, null);
			Transaction.Commit();
		}

		public virtual void Rollback()
		{
			using var a = ActivityService.Start(ActivityID.TransactionRollback);
			if (a != null && DataConnection != null)
				a.AddQueryInfo(DataConnection, DataConnection.Connection, null);
			Transaction.Rollback();
		}

		public virtual Task CommitAsync(CancellationToken cancellationToken)
		{
#if NET6_0_OR_GREATER
			var a = ActivityService.StartAndConfigureAwait(ActivityID.TransactionCommitAsync);
			if (a != null && DataConnection != null)
				a.AddQueryInfo(DataConnection, DataConnection.Connection, null);

			if (a is null)
				return Transaction.CommitAsync(cancellationToken);

			return CallAwaitUsing(a, Transaction, cancellationToken);

			static async Task CallAwaitUsing(AsyncDisposableWrapper activity, DbTransaction transaction, CancellationToken token)
			{
				await using (activity)
					await transaction.CommitAsync(token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
#else
			using var a = ActivityService.Start(ActivityID.TransactionCommitAsync);
			if (a != null && DataConnection != null)
				a.AddQueryInfo(DataConnection, DataConnection.Connection, null);

			Transaction.Commit();
			return Task.CompletedTask;
#endif
		}

		public virtual Task RollbackAsync(CancellationToken cancellationToken)
		{
#if NET6_0_OR_GREATER
			var a = ActivityService.StartAndConfigureAwait(ActivityID.TransactionRollbackAsync);
			if (a != null && DataConnection != null)
				a.AddQueryInfo(DataConnection, DataConnection.Connection, null);

			if (a is null)
				return Transaction.RollbackAsync(cancellationToken);

			return CallAwaitUsing(a, Transaction, cancellationToken);

			static async Task CallAwaitUsing(AsyncDisposableWrapper activity, DbTransaction transaction, CancellationToken token)
			{
				await using (activity)
					await transaction.RollbackAsync(token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
#else
			using var a = ActivityService.Start(ActivityID.TransactionRollbackAsync);
			if (a != null && DataConnection != null)
				a.AddQueryInfo(DataConnection, DataConnection.Connection, null);

			Transaction.Rollback();
			return Task.CompletedTask;
#endif
		}

		#region IDisposable

		public virtual void Dispose()
		{
			using var activity = ActivityService.Start(ActivityID.TransactionDispose);
			if (activity != null && DataConnection != null)
				activity.AddQueryInfo(DataConnection, DataConnection.Connection, null);

			Transaction.Dispose();
		}

		#endregion

		#region IAsyncDisposable
		public virtual ValueTask DisposeAsync()
		{
			if (Transaction is IAsyncDisposable asyncDisposable)
			{
				var a = ActivityService.StartAndConfigureAwait(ActivityID.TransactionDisposeAsync);
				if (a != null && DataConnection != null)
					a.AddQueryInfo(DataConnection, DataConnection.Connection, null);

				if (a is null)
					return asyncDisposable.DisposeAsync();

				return CallAwaitUsing(a, asyncDisposable);

				static async ValueTask CallAwaitUsing(AsyncDisposableWrapper activity, IAsyncDisposable disposable)
				{
					await using (activity)
						await disposable.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				}
			}

			using var activity = ActivityService.Start(ActivityID.TransactionDisposeAsync);
			if (activity != null && DataConnection != null)
				activity.AddQueryInfo(DataConnection, DataConnection.Connection, null);

			Transaction.Dispose();
			return default;
		}
		#endregion
	}
}
