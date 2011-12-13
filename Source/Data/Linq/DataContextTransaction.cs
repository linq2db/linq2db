using System;
using System.Data;

using JetBrains.Annotations;

namespace LinqToDB.Data.Linq
{
	public class DataContextTransaction : IDisposable
	{
		public DataContextTransaction([NotNull] DataContext dataContext)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");

			DataContext = dataContext;
		}

		public DataContext DataContext { get; set; }

		int _transactionCounter;

		public void BeginTransaction()
		{
			var db = DataContext.GetDBManager();

			db.BeginTransaction();

			if (_transactionCounter == 0)
				DataContext.LockDbManagerCounter++;

			_transactionCounter++;
		}

		public void BeginTransaction(IsolationLevel level)
		{
			var db = DataContext.GetDBManager();

			db.BeginTransaction(level);

			if (_transactionCounter == 0)
				DataContext.LockDbManagerCounter++;

			_transactionCounter++;
		}

		public void CommitTransaction()
		{
			if (_transactionCounter > 0)
			{
				var db = DataContext.GetDBManager();

				db.CommitTransaction();

				_transactionCounter--;

				if (_transactionCounter == 0)
				{
					DataContext.LockDbManagerCounter--;
					DataContext.ReleaseQuery();
				}
			}
		}

		public void RollbackTransaction()
		{
			if (_transactionCounter > 0)
			{
				var db = DataContext.GetDBManager();

				db.RollbackTransaction();

				_transactionCounter--;

				if (_transactionCounter == 0)
				{
					DataContext.LockDbManagerCounter--;
					DataContext.ReleaseQuery();
				}
			}
		}

		public void Dispose()
		{
			if (_transactionCounter > 0)
			{
				var db = DataContext.GetDBManager();

				db.RollbackTransaction();

				_transactionCounter = 0;

				DataContext.LockDbManagerCounter--;
			}
		}
	}
}
