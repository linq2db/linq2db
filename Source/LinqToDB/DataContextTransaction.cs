using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	/// <summary>
	/// Explicit data context <see cref="DataContext"/> transaction wrapper.
	/// </summary>
	[PublicAPI]
	public class DataContextTransaction : IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Creates new transaction wrapper.
		/// </summary>
		/// <param name="dataContext">Data context.</param>
		public DataContextTransaction(DataContext dataContext)
		{
			DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
		}

		/// <summary>
		/// Gets or sets transaction's data context.
		/// </summary>
		public DataContext DataContext { get; set; }

		int _transactionCounter;

		/// <summary>
		/// Start new transaction with default isolation level.
		/// If underlying connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		public void BeginTransaction()
		{
			var db = DataContext.GetDataConnection();

			db.BeginTransaction();

			if (_transactionCounter == 0)
				DataContext.LockDbManagerCounter++;

			_transactionCounter++;
		}

		/// <summary>
		/// Start new transaction with specified isolation level.
		/// If underlying connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="level">Transaction isolation level.</param>
		public void BeginTransaction(IsolationLevel level)
		{
			var db = DataContext.GetDataConnection();

			db.BeginTransaction(level);

			if (_transactionCounter == 0)
				DataContext.LockDbManagerCounter++;

			_transactionCounter++;
		}

		/// <summary>
		/// Start new transaction asynchronously with default isolation level.
		/// If underlying connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			var db = DataContext.GetDataConnection();

			await db.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

			if (_transactionCounter == 0)
				DataContext.LockDbManagerCounter++;

			_transactionCounter++;
		}

		/// <summary>
		/// Start new transaction asynchronously with specified isolation level.
		/// If underlying connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="level">Transaction isolation level.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		public async Task BeginTransactionAsync(IsolationLevel level, CancellationToken cancellationToken = default)
		{
			var db = DataContext.GetDataConnection();

			await db.BeginTransactionAsync(level, cancellationToken).ConfigureAwait(false);

			if (_transactionCounter == 0)
				DataContext.LockDbManagerCounter++;

			_transactionCounter++;
		}

		/// <summary>
		/// Commits started transaction.
		/// </summary>
		public void CommitTransaction()
		{
			if (_transactionCounter > 0)
			{
				var db = DataContext.GetDataConnection();

				db.CommitTransaction();

				_transactionCounter--;

				if (_transactionCounter == 0)
				{
					DataContext.LockDbManagerCounter--;
					DataContext.ReleaseQuery();
				}
			}
		}

		/// <summary>
		/// Rollbacks started transaction.
		/// </summary>
		public void RollbackTransaction()
		{
			if (_transactionCounter > 0)
			{
				var db = DataContext.GetDataConnection();

				db.RollbackTransaction();

				_transactionCounter--;

				if (_transactionCounter == 0)
				{
					DataContext.LockDbManagerCounter--;
					DataContext.ReleaseQuery();
				}
			}
		}

		/// <summary>
		/// Commits started transaction.
		/// If underlying provider doesn't support asynchronous commit, it will be performed synchronously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
		{
			if (_transactionCounter > 0)
			{
				var db = DataContext.GetDataConnection();

				await db.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

				_transactionCounter--;

				if (_transactionCounter == 0)
				{
					DataContext.LockDbManagerCounter--;
					await DataContext.ReleaseQueryAsync().ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// Rollbacks started transaction asynchronously.
		/// If underlying provider doesn't support asynchronous rollback, it will be performed synchronously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
		{
			if (_transactionCounter > 0)
			{
				var db = DataContext.GetDataConnection();

				await db.RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);

				_transactionCounter--;

				if (_transactionCounter == 0)
				{
					DataContext.LockDbManagerCounter--;
					await DataContext.ReleaseQueryAsync().ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// Rollbacks started transaction (if any).
		/// </summary>
		public void Dispose()
		{
			if (_transactionCounter > 0)
			{
				var db = DataContext.GetDataConnection();

				db.DisposeTransaction();

				_transactionCounter = 0;

				DataContext.LockDbManagerCounter--;
				DataContext.ReleaseQuery();
			}
		}

		/// <inheritdoc cref="Dispose"/>
		async ValueTask IAsyncDisposable.DisposeAsync()
		{
			if (_transactionCounter > 0)
			{
				var db = DataContext.GetDataConnection();

				await db.DisposeTransactionAsync().ConfigureAwait(false);

				_transactionCounter = 0;

				DataContext.LockDbManagerCounter--;
				await DataContext.ReleaseQueryAsync().ConfigureAwait(false);
			}
		}
	}
}
