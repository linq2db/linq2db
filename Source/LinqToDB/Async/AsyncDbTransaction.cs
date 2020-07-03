using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Async
{
	/// <summary>
	/// Asynchronous version of the <see cref="IDbTransaction"/> interface, allowing asynchronous operations,
	/// missing from <see cref="IDbTransaction"/>.
	/// Providers with async operations support could override its methods with asynchronous implementations.
	/// </summary>
	[PublicAPI]
	public class AsyncDbTransaction : IAsyncDbTransaction
	{
		internal protected AsyncDbTransaction(IDbTransaction transaction)
		{
			Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
		}

		public virtual IDbConnection Connection      => Transaction.Connection;

		public virtual IsolationLevel IsolationLevel => Transaction.IsolationLevel;

		public IDbTransaction Transaction { get; }

		public virtual void Commit()
		{
			Transaction.Commit();
		}

		public virtual Task CommitAsync(CancellationToken cancellationToken = default)
		{
#if NETSTANDARD2_1 || NETCOREAPP3_1
			if (Transaction is DbTransaction dbTransaction)
				return dbTransaction.CommitAsync(cancellationToken);
#endif

			Commit();

			return TaskEx.CompletedTask;
		}

		public virtual void Dispose()
		{
			Transaction.Dispose();
		}

#if NET45 || NET46
		public virtual Task DisposeAsync()
		{
			Dispose();

			return TaskEx.CompletedTask;
		}
#else
		public virtual ValueTask DisposeAsync()
		{
			if (Transaction is IAsyncDisposable asyncDisposable)
				return asyncDisposable.DisposeAsync();

			Dispose();
			return new ValueTask(Task.CompletedTask);
		}
#endif

		public virtual void Rollback()
		{
			Transaction.Rollback();
		}

		public virtual Task RollbackAsync(CancellationToken cancellationToken = default)
		{
#if NETSTANDARD2_1 || NETCOREAPP3_1
			if (Transaction is DbTransaction dbTransaction)
				return dbTransaction.RollbackAsync(cancellationToken);
#endif

			Rollback();

			return TaskEx.CompletedTask;
		}
	}
}
