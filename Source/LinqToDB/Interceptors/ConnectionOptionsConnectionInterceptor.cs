using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	public sealed class ConnectionOptionsConnectionInterceptor : ConnectionInterceptor
	{
		internal readonly Action<DbConnection>?                        OnConnectionOpening;
		internal readonly Func<DbConnection, CancellationToken, Task>? OnConnectionOpeningAsync;
		internal readonly Action<DbConnection>?                        OnConnectionOpened;
		internal readonly Func<DbConnection, CancellationToken, Task>? OnConnectionOpenedAsync;

		internal ConnectionOptionsConnectionInterceptor(
			Action<DbConnection>?                        connectionOpening,
			Func<DbConnection, CancellationToken, Task>? connectionOpeningAsync,
			Action<DbConnection>?                        connectionOpened,
			Func<DbConnection, CancellationToken, Task>? connectionOpenedAsync)
		{
			OnConnectionOpening      = connectionOpening;
			OnConnectionOpeningAsync = connectionOpeningAsync;
			OnConnectionOpened       = connectionOpened;
			OnConnectionOpenedAsync  = connectionOpenedAsync;
		}

		public override void ConnectionOpening(ConnectionEventData eventData, DbConnection connection)
		{
			OnConnectionOpening?.Invoke(connection);
		}

		public override Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			if (OnConnectionOpeningAsync != null)
				return OnConnectionOpeningAsync(connection, cancellationToken);

			OnConnectionOpening?.Invoke(connection);
			return Task.CompletedTask;
		}

		public override void ConnectionOpened(ConnectionEventData eventData, DbConnection connection)
		{
			OnConnectionOpened?.Invoke(connection);
		}

		public override Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			if (OnConnectionOpenedAsync != null)
				return OnConnectionOpenedAsync(connection, cancellationToken);

			OnConnectionOpened?.Invoke(connection);
			return Task.CompletedTask;
		}
	}
}
