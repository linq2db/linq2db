﻿using System;
using System.Data;
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
		private readonly IDbTransaction _transaction;

		internal protected AsyncDbTransaction(IDbTransaction transaction)
		{
			_transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
		}

		public virtual IDbConnection Connection => Transaction.Connection;

		public virtual IsolationLevel IsolationLevel => Transaction.IsolationLevel;

		public IDbTransaction Transaction => _transaction;

		public virtual void Commit()
		{
			Transaction.Commit();
		}

		public virtual Task CommitAsync(CancellationToken cancellationToken = default)
		{
			Commit();

			return TaskEx.CompletedTask;
		}

		public virtual void Dispose()
		{
			Transaction.Dispose();
		}

		public virtual void Rollback()
		{
			Transaction.Rollback();
		}

		public virtual Task RollbackAsync(CancellationToken cancellationToken = default)
		{
			Rollback();

			return TaskEx.CompletedTask;
		}
	}
}
