using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Async
{
	/// <summary>
	/// Asynchronous version of the <see cref="DbTransaction"/> interface, allowing asynchronous operations,
	/// missing from <see cref="DbTransaction"/>.
	/// Providers with async operations support could override its methods with asynchronous implementations.
	/// </summary>
	internal sealed class ReflectedAsyncDbTransaction : AsyncDbTransaction
	{
		private readonly Func<DbTransaction, CancellationToken, Task>? _commitAsync;
		private readonly Func<DbTransaction, CancellationToken, Task>? _rollbackAsync;
		private readonly Func<DbTransaction, ValueTask>?               _disposeAsync;

		public ReflectedAsyncDbTransaction(
			DbTransaction transaction,
			Func<DbTransaction, CancellationToken, Task>? commitAsync,
			Func<DbTransaction, CancellationToken, Task>? rollbackAsync,
			Func<DbTransaction, ValueTask>?               disposeAsync)
			: base(transaction)
		{
			_commitAsync   = commitAsync;
			_rollbackAsync = rollbackAsync;
			_disposeAsync  = disposeAsync;
		}

		public override Task CommitAsync(CancellationToken cancellationToken)
		{
			return _commitAsync?.Invoke(Transaction, cancellationToken) ?? base.CommitAsync(cancellationToken);
		}

		public override Task RollbackAsync(CancellationToken cancellationToken)
		{
			return _rollbackAsync?.Invoke(Transaction, cancellationToken) ?? base.RollbackAsync(cancellationToken);
		}

		public override ValueTask DisposeAsync()
		{
			return _disposeAsync?.Invoke(Transaction) ?? base.DisposeAsync();
		}
	}
}
