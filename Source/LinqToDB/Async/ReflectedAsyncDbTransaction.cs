using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Async
{
	/// <summary>
	/// Asynchronous version of the <see cref="IDbTransaction"/> interface, allowing asynchronous operations,
	/// missing from <see cref="IDbTransaction"/>.
	/// Providers with async operations support could override its methods with asynchronous implementations.
	/// </summary>
	internal class ReflectedAsyncDbTransaction : AsyncDbTransaction
	{
		private Func<IDbTransaction, CancellationToken, Task>? _commitAsync;
		private Func<IDbTransaction, CancellationToken, Task>? _rollbackAsync;

		public ReflectedAsyncDbTransaction(
			IDbTransaction                                 transaction,
			Func<IDbTransaction, CancellationToken, Task>? commitAsync,
			Func<IDbTransaction, CancellationToken, Task>? rollbackAsync)
			: base(transaction)
		{
			_commitAsync   = commitAsync;
			_rollbackAsync = rollbackAsync;
		}

		public override Task CommitAsync(CancellationToken cancellationToken = default)
		{
			return _commitAsync?.Invoke(Transaction, cancellationToken) ?? base.CommitAsync(cancellationToken);
		}

		public override Task RollbackAsync(CancellationToken cancellationToken = default)
		{
			return _rollbackAsync?.Invoke(Transaction, cancellationToken) ?? base.RollbackAsync(cancellationToken);
		}
	}
}
