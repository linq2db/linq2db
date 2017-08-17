using System;
using System.Data;

using JetBrains.Annotations;

namespace LinqToDB
{
	/// <summary>
	/// Explicit data context <see cref="DataContext"/> transaction wrapper.
	/// </summary>
	public class DataContextTransaction : IDisposable
	{
		/// <summary>
		/// Creates new transaction wrapper.
		/// </summary>
		/// <param name="dataContext">Data context.</param>
		public DataContextTransaction([NotNull] DataContext dataContext)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");

			DataContext = dataContext;
		}

		/// <summary>
		/// Gets or sets transaction's data context.
		/// </summary>
		public DataContext DataContext { get; set; }

		int _transactionCounter;

		/// <summary>
		/// Start new transaction with default isolation level.
		/// If underlying connection already has transaction, it will be rolled back.
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
		/// If underlying connection already has transaction, it will be rolled back.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		public void BeginTransaction(IsolationLevel level)
		{
			var db = DataContext.GetDataConnection();

			db.BeginTransaction(level);

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
		/// Rollbacks started transaction (if any).
		/// </summary>
		public void Dispose()
		{
			if (_transactionCounter > 0)
			{
				var db = DataContext.GetDataConnection();

				db.RollbackTransaction();

				_transactionCounter = 0;

				DataContext.LockDbManagerCounter--;
			}
		}
	}
}
