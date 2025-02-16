using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Async;
using LinqToDB.Configuration;

namespace LinqToDB.Data.RetryPolicy
{
	sealed class RetryingDbConnection : IAsyncDbConnection, IProxy<DbConnection>
	{
		readonly DataConnection     _dataConnection;
		readonly IAsyncDbConnection _connection;
		readonly IRetryPolicy       _policy;

		public RetryingDbConnection(DataConnection dataConnection, IAsyncDbConnection connection, IRetryPolicy policy)
		{
			_dataConnection = dataConnection;
			_connection     = connection;
			_policy         = policy;
		}

		#region IProxy<DbConnection>
		DbConnection IProxy<DbConnection>.UnderlyingObject => _connection.Connection;
		#endregion

		#region IDisposable
		void IDisposable.Dispose() => _connection.Dispose();
		#endregion

		#region IAsyncDisposable
		ValueTask IAsyncDisposable.DisposeAsync() => _connection.DisposeAsync();
		#endregion

		#region IAsyncDbConnection
		DbConnection IAsyncDbConnection.Connection => _connection.Connection;

		string IAsyncDbConnection.ConnectionString
		{
			get => _connection.ConnectionString;
			set => _connection.ConnectionString = value;
		}

		ConnectionState IAsyncDbConnection.State => _connection.State;

		IAsyncDbTransaction IAsyncDbConnection.BeginTransaction() => _connection.BeginTransaction();
		IAsyncDbTransaction IAsyncDbConnection.BeginTransaction(IsolationLevel isolationLevel) => _connection.BeginTransaction(isolationLevel);
		ValueTask<IAsyncDbTransaction> IAsyncDbConnection.BeginTransactionAsync(CancellationToken cancellationToken) => _connection.BeginTransactionAsync(cancellationToken);
		ValueTask<IAsyncDbTransaction> IAsyncDbConnection.BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) => _connection.BeginTransactionAsync(isolationLevel, cancellationToken);

		DbCommand IAsyncDbConnection.CreateCommand() => new RetryingDbCommand(_dataConnection, _connection.CreateCommand(), _policy);

		void IAsyncDbConnection.Close() => _connection.Close();
		Task IAsyncDbConnection.CloseAsync() => _connection.CloseAsync();

		void IAsyncDbConnection.Open() => _policy.Execute(_connection.Open);
		Task IAsyncDbConnection.OpenAsync(CancellationToken cancellationToken) => _policy.ExecuteAsync(_connection.OpenAsync, cancellationToken);
#endregion
	}
}
