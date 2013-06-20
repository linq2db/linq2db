﻿using System;
using System.Data;

using JetBrains.Annotations;

namespace LinqToDB
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
			var db = DataContext.GetDataConnection();

			db.BeginTransaction();

			if (_transactionCounter == 0)
				DataContext.LockDbManagerCounter++;

			_transactionCounter++;
		}

		public void BeginTransaction(IsolationLevel level)
		{
			var db = DataContext.GetDataConnection();

			db.BeginTransaction(level);

			if (_transactionCounter == 0)
				DataContext.LockDbManagerCounter++;

			_transactionCounter++;
		}

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
