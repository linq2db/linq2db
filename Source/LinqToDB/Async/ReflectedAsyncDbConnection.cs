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
		private Func<IDbConnection, CancellationToken, ValueTask>                                 _closeAsync;
		private Func<IDbConnection, ValueTask>                                                    _disposeAsync;
		private Func<IDbConnection, CancellationToken, Task<IAsyncDbTransaction>>                 _beginTransactionAsync;
		private Func<IDbConnection, IsolationLevel, CancellationToken, Task<IAsyncDbTransaction>> _beginTransactionIlAsync;
		private Func<IDbConnection, string, CancellationToken, Task>                              _changeDatabaseAsync;

		public ReflectedAsyncDbConnection(
			IDbConnection connection,
			Func<IDbConnection, CancellationToken, ValueTask>                                 closeAsync,
			Func<IDbConnection, ValueTask>                                                    disposeAsync,
			Func<IDbConnection, CancellationToken, Task<IAsyncDbTransaction>>                 beginTransactionAsync,
			Func<IDbConnection, IsolationLevel, CancellationToken, Task<IAsyncDbTransaction>> beginTransactionIlAsync,
			Func<IDbConnection, string, CancellationToken, Task>                              changeDatabaseAsync)
			: base(connection)
		{
			_closeAsync              = closeAsync;
			_disposeAsync            = disposeAsync;
			_beginTransactionAsync   = beginTransactionAsync;
			_beginTransactionIlAsync = beginTransactionIlAsync;
			_changeDatabaseAsync     = changeDatabaseAsync;
		}

		public override Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			return _beginTransactionAsync?.Invoke(Connection, cancellationToken) ?? base.BeginTransactionAsync(cancellationToken);
		}

		public override Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
		{
			return _beginTransactionIlAsync?.Invoke(Connection, isolationLevel, cancellationToken) ?? base.BeginTransactionAsync(isolationLevel, cancellationToken);
		}

		public override ValueTask CloseAsync(CancellationToken cancellationToken = default)
		{
			return _closeAsync?.Invoke(Connection, cancellationToken) ?? base.CloseAsync(cancellationToken);
		}

		public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
		{
			return _changeDatabaseAsync?.Invoke(Connection, databaseName, cancellationToken) ?? base.ChangeDatabaseAsync(databaseName, cancellationToken);
		}

		public override ValueTask DisposeAsync()
		{
			return _disposeAsync?.Invoke(Connection) ?? base.DisposeAsync();
		}
	}
}
