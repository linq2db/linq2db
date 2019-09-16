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
		private Func<IDbConnection, CancellationToken, Task>?                                      _closeAsync;
		private Func<IDbConnection, CancellationToken, Task<IAsyncDbTransaction>>?                 _beginTransactionAsync;
		private Func<IDbConnection, IsolationLevel, CancellationToken, Task<IAsyncDbTransaction>>? _beginTransactionIlAsync;

		public ReflectedAsyncDbConnection(
			IDbConnection connection,
			Func<IDbConnection, CancellationToken, Task<IAsyncDbTransaction>>?                 beginTransactionAsync,
			Func<IDbConnection, IsolationLevel, CancellationToken, Task<IAsyncDbTransaction>>? beginTransactionIlAsync,
			Func<IDbConnection, CancellationToken, Task>?                                      closeAsync)
			: base(connection)
		{
			_beginTransactionAsync   = beginTransactionAsync;
			_beginTransactionIlAsync = beginTransactionIlAsync;
			_closeAsync              = closeAsync;
		}

		public override Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			return _beginTransactionAsync?.Invoke(Connection, cancellationToken) ?? base.BeginTransactionAsync(cancellationToken);
		}

		public override Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
		{
			return _beginTransactionIlAsync?.Invoke(Connection, isolationLevel, cancellationToken) ?? base.BeginTransactionAsync(isolationLevel, cancellationToken);
		}

		public override Task CloseAsync(CancellationToken cancellationToken = default)
		{
			return _closeAsync?.Invoke(Connection, cancellationToken) ?? base.CloseAsync(cancellationToken);
		}
	}
}
