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
		private readonly Func<IDbTransaction, CancellationToken, Task>? _commitAsync;
		private readonly Func<IDbTransaction, CancellationToken, Task>? _rollbackAsync;
#if NATIVE_ASYNC
		private readonly Func<IDbTransaction, ValueTask>?               _disposeAsync;
#else
		private readonly Func<IDbTransaction, Task>?                    _disposeAsync;
#endif

		public ReflectedAsyncDbTransaction(
			IDbTransaction                                 transaction,
			Func<IDbTransaction, CancellationToken, Task>? commitAsync,
			Func<IDbTransaction, CancellationToken, Task>? rollbackAsync,
#if NATIVE_ASYNC
			Func<IDbTransaction, ValueTask>?               disposeAsync)
#else
			Func<IDbTransaction, Task>?                    disposeAsync)
#endif
			: base(transaction)
		{
			_commitAsync   = commitAsync;
			_rollbackAsync = rollbackAsync;
			_disposeAsync  = disposeAsync;
		}

		public override Task CommitAsync(CancellationToken cancellationToken = default)
		{
			return _commitAsync?.Invoke(Transaction, cancellationToken) ?? base.CommitAsync(cancellationToken);
		}

		public override Task RollbackAsync(CancellationToken cancellationToken = default)
		{
			return _rollbackAsync?.Invoke(Transaction, cancellationToken) ?? base.RollbackAsync(cancellationToken);
		}

#if !NATIVE_ASYNC
		public override Task DisposeAsync()
		{
			return _disposeAsync?.Invoke(Transaction) ?? base.DisposeAsync();
		}
#else
		public override ValueTask DisposeAsync()
		{
			return _disposeAsync != null ? _disposeAsync.Invoke(Transaction) : base.DisposeAsync();
		}
#endif
	}
}
