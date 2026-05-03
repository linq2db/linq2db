using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Async
{
	/// <summary>
	/// Provides reflection-based <see cref="DbConnection"/> wrapper with async operations support.
	/// </summary>
	internal sealed class ReflectedAsyncDbConnection : AsyncDbConnection
	{
		private readonly Func<DbConnection, CancellationToken, Task>?                                           _openAsync;
		private readonly Func<DbConnection, Task>?                                                              _closeAsync;
		private readonly Func<DbConnection, CancellationToken, ValueTask<IAsyncDbTransaction>>?                 _beginTransactionAsync;
		private readonly Func<DbConnection, IsolationLevel, CancellationToken, ValueTask<IAsyncDbTransaction>>? _beginTransactionIlAsync;
		private readonly Func<DbConnection, ValueTask>?                                                         _disposeAsync;

		public ReflectedAsyncDbConnection(
			DbConnection connection,
			Func<DbConnection, CancellationToken, ValueTask<IAsyncDbTransaction>>?                 beginTransactionAsync,
			Func<DbConnection, IsolationLevel, CancellationToken, ValueTask<IAsyncDbTransaction>>? beginTransactionIlAsync,
			Func<DbConnection, CancellationToken, Task>?                                           openAsync,
			Func<DbConnection, Task>?                                                              closeAsync,
			Func<DbConnection, ValueTask>?                                                         disposeAsync)
			: base(connection)
		{
			_beginTransactionAsync   = beginTransactionAsync;
			_beginTransactionIlAsync = beginTransactionIlAsync;
			_openAsync               = openAsync;
			_closeAsync              = closeAsync;
			_disposeAsync            = disposeAsync;
		}

		public override ValueTask<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
		{
			return _beginTransactionAsync?.Invoke(Connection, cancellationToken) ?? base.BeginTransactionAsync(cancellationToken);
		}

		public override ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
		{
			return _beginTransactionIlAsync?.Invoke(Connection, isolationLevel, cancellationToken) ?? base.BeginTransactionAsync(isolationLevel, cancellationToken);
		}

		public override Task OpenAsync(CancellationToken cancellationToken)
		{
			return _openAsync?.Invoke(Connection, cancellationToken) ?? base.OpenAsync(cancellationToken);
		}

		public override Task CloseAsync()
		{
			return _closeAsync?.Invoke(Connection) ?? base.CloseAsync();
		}

		public override ValueTask DisposeAsync()
		{
			return _disposeAsync?.Invoke(Connection) ?? base.DisposeAsync();
		}
	}
}
