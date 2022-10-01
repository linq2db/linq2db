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
	internal class ReflectedAsyncDbTransaction : AsyncDbTransaction
	{
		private readonly Func<DbTransaction, CancellationToken, Task>? _commitAsync;
		private readonly Func<DbTransaction, CancellationToken, Task>? _rollbackAsync;
#if NATIVE_ASYNC
		private readonly Func<DbTransaction, ValueTask>?               _disposeAsync;
#else
		private readonly Func<DbTransaction, Task>?                    _disposeAsync;
#endif

		public ReflectedAsyncDbTransaction(
			DbTransaction transaction,
			Func<DbTransaction, CancellationToken, Task>? commitAsync,
			Func<DbTransaction, CancellationToken, Task>? rollbackAsync,
#if NATIVE_ASYNC
			Func<DbTransaction, ValueTask>?               disposeAsync)
#else
			Func<DbTransaction, Task>?                    disposeAsync)
#endif
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

#if !NATIVE_ASYNC
		public override Task DisposeAsync()
#else
		public override ValueTask DisposeAsync()
#endif
		{
			return _disposeAsync?.Invoke(Transaction) ?? base.DisposeAsync();
		}
	}
}
