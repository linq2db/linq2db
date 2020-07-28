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
#if !NETFRAMEWORK
		private readonly Func<IDbConnection, ValueTask>?                _disposeAsync;
#else
		private readonly Func<IDbConnection, Task>?                     _disposeAsync;
#endif

		public ReflectedAsyncDbTransaction(
			IDbTransaction                                 transaction,
			Func<IDbTransaction, CancellationToken, Task>? commitAsync,
			Func<IDbTransaction, CancellationToken, Task>? rollbackAsync,
#if !NETFRAMEWORK
			Func<IDbConnection, ValueTask>?                disposeAsync)
#else
			Func<IDbConnection, Task>?                     disposeAsync)
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

#if NETFRAMEWORK
		public override Task DisposeAsync()
		{
			return _disposeAsync?.Invoke(Connection) ?? base.DisposeAsync();
		}
#else
		public override ValueTask DisposeAsync()
		{
			return _disposeAsync != null ? _disposeAsync.Invoke(Connection) : base.DisposeAsync();
		}
#endif
	}
}
