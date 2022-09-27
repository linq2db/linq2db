using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Async
{
	/// <summary>
	/// Provides reflection-based <see cref="DbConnection"/> wrapper with async operations support.
	/// </summary>
	internal class ReflectedAsyncDbConnection : AsyncDbConnection
	{
		private readonly Func<DbConnection, CancellationToken, Task>?                                           _openAsync;
		private readonly Func<DbConnection, Task>?                                                              _closeAsync;
#if NATIVE_ASYNC
		private readonly Func<DbConnection, CancellationToken, ValueTask<IAsyncDbTransaction>>?                 _beginTransactionAsync;
		private readonly Func<DbConnection, IsolationLevel, CancellationToken, ValueTask<IAsyncDbTransaction>>? _beginTransactionIlAsync;
		private readonly Func<DbConnection, ValueTask>?                                                         _disposeAsync;
#else
		private readonly Func<DbConnection, CancellationToken, Task<IAsyncDbTransaction>>?                      _beginTransactionAsync;
		private readonly Func<DbConnection, IsolationLevel, CancellationToken, Task<IAsyncDbTransaction>>?      _beginTransactionIlAsync;
		private readonly Func<DbConnection, Task>?                                                              _disposeAsync;
#endif

		public ReflectedAsyncDbConnection(
			DbConnection connection,
#if NATIVE_ASYNC
			Func<DbConnection, CancellationToken, ValueTask<IAsyncDbTransaction>>?                 beginTransactionAsync,
			Func<DbConnection, IsolationLevel, CancellationToken, ValueTask<IAsyncDbTransaction>>? beginTransactionIlAsync,
#else
			Func<DbConnection, CancellationToken, Task<IAsyncDbTransaction>>?                      beginTransactionAsync,
			Func<DbConnection, IsolationLevel, CancellationToken, Task<IAsyncDbTransaction>>?      beginTransactionIlAsync,
#endif
			Func<DbConnection, CancellationToken, Task>?                                           openAsync,
			Func<DbConnection, Task>?                                                              closeAsync,
#if NATIVE_ASYNC
			Func<DbConnection, ValueTask>?                                                         disposeAsync)
#else
			Func<DbConnection, Task>?                                                              disposeAsync)
#endif
			: base(connection)
		{
			_beginTransactionAsync   = beginTransactionAsync;
			_beginTransactionIlAsync = beginTransactionIlAsync;
			_openAsync               = openAsync;
			_closeAsync              = closeAsync;
			_disposeAsync            = disposeAsync;
		}

#if NATIVE_ASYNC
		public override ValueTask<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
#else
		public override Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
#endif
		{
			return _beginTransactionAsync?.Invoke(Connection, cancellationToken) ?? base.BeginTransactionAsync(cancellationToken);
		}

#if NATIVE_ASYNC
		public override ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
#else
		public override Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
#endif
		{
			return _beginTransactionIlAsync?.Invoke(Connection, isolationLevel, cancellationToken) ?? base.BeginTransactionAsync(isolationLevel, cancellationToken);
		}

		public override Task OpenAsync(CancellationToken cancellationToken = default)
		{
			return _openAsync?.Invoke(Connection, cancellationToken) ?? base.OpenAsync(cancellationToken);
		}

		public override Task CloseAsync()
		{
			return _closeAsync?.Invoke(Connection) ?? base.CloseAsync();
		}

#if !NATIVE_ASYNC
		public override Task DisposeAsync()
#else
		public override ValueTask DisposeAsync()
#endif
		{
			return _disposeAsync?.Invoke(Connection) ?? base.DisposeAsync();
		}
	}
}
