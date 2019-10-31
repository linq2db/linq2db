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
		private Func<IDbConnection, CancellationToken, Task>?                                      _openAsync;
		private Func<IDbConnection, Task>?                                                         _closeAsync;
		private Func<IDbConnection, Task>?                                                         _disposeAsync;
		private Func<IDbConnection, CancellationToken, Task<IAsyncDbTransaction>>?                 _beginTransactionAsync;
		private Func<IDbConnection, IsolationLevel, CancellationToken, Task<IAsyncDbTransaction>>? _beginTransactionIlAsync;

		public ReflectedAsyncDbConnection(
			IDbConnection connection,
			Func<IDbConnection, CancellationToken, Task<IAsyncDbTransaction>>?                 beginTransactionAsync,
			Func<IDbConnection, IsolationLevel, CancellationToken, Task<IAsyncDbTransaction>>? beginTransactionIlAsync,
			Func<IDbConnection, CancellationToken, Task>?                                      openAsync,
			Func<IDbConnection, Task>?                                                         closeAsync,
			Func<IDbConnection, Task>?                                                         disposeAsync)
			: base(connection)
		{
			_beginTransactionAsync   = beginTransactionAsync;
			_beginTransactionIlAsync = beginTransactionIlAsync;
			_openAsync               = openAsync;
			_closeAsync              = closeAsync;
			_disposeAsync            = disposeAsync;
		}

		public override Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			return _beginTransactionAsync?.Invoke(Connection, cancellationToken) ?? base.BeginTransactionAsync(cancellationToken);
		}

		public override Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
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

		public override Task DisposeAsync()
		{
			return _disposeAsync?.Invoke(Connection) ?? base.DisposeAsync();
		}
	}
}
