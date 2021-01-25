using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data.RetryPolicy
{
	using Configuration;
	using LinqToDB.Async;

	class RetryingDbConnection : DbConnection, IProxy<DbConnection>, IDisposable, ICloneable, IAsyncDbConnection
	{
		readonly DataConnection     _dataConnection;
		readonly DbConnection       _dbConnection;
		readonly IAsyncDbConnection _connection;
		readonly IRetryPolicy       _policy;

		public RetryingDbConnection(DataConnection dataConnection, IAsyncDbConnection connection, IRetryPolicy policy)
		{
			_dataConnection = dataConnection;
			_connection     = connection;
			_dbConnection   = (DbConnection)connection.Connection;
			_policy         = policy;
		}

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
		{
			return _dbConnection.BeginTransaction(isolationLevel);
		}

		public override void Close()
		{
			_connection.Close();
		}

		public override void ChangeDatabase(string databaseName)
		{
			_connection.ChangeDatabase(databaseName);
		}

		public override void Open()
		{
			_policy.Execute(_connection.Open);
		}

		public override string ConnectionString
		{
			get => _connection.ConnectionString;
			set => _connection.ConnectionString = value;
		}

		public override string          Database      => _connection.Database;

		public override ConnectionState State         => _connection.State;
		public override string          DataSource    => _dbConnection.DataSource;
		public override string          ServerVersion => _dbConnection.ServerVersion;

		protected override DbCommand CreateDbCommand()
		{
			return new RetryingDbCommand(_dbConnection.CreateCommand(), _policy);
		}

		public override async Task OpenAsync(CancellationToken cancellationToken)
		{
			await _policy.ExecuteAsync(async ct => await _connection.OpenAsync(ct).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext), cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		void IDisposable.Dispose()
		{
			((IDisposable)_connection).Dispose();
		}

		public DbConnection UnderlyingObject => _dbConnection;

		public override DataTable GetSchema()
		{
			return _dbConnection.GetSchema();
		}

		public override DataTable GetSchema(string collectionName)
		{
			return _dbConnection.GetSchema(collectionName);
		}

		public override DataTable GetSchema(string collectionName, string[] restrictionValues)
		{
			return _dbConnection.GetSchema(collectionName, restrictionValues);
		}

		public override ISite Site
		{
			get => _dbConnection.Site;
			set => _dbConnection.Site = value;
		}

		public override int ConnectionTimeout => _connection.ConnectionTimeout;

		// return this or it will be breaking change for DataConnection.Connection property
		public IDbConnection Connection => this;

		public override event StateChangeEventHandler StateChange
		{
			add    => _dbConnection.StateChange += value;
			remove => _dbConnection.StateChange -= value;
		}

		public object Clone()
		{
			if (_connection is ICloneable cloneable)
				return cloneable.Clone();
			return _dataConnection.DataProvider.CreateConnection(_dataConnection.ConnectionString!);
		}

#if NETSTANDARD2_1PLUS
		public new ValueTask<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
			=> _connection.BeginTransactionAsync(cancellationToken);

		public new ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
			=> _connection.BeginTransactionAsync(isolationLevel, cancellationToken);

		protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
			=> _dbConnection.BeginTransactionAsync(isolationLevel, cancellationToken);
#elif !NETFRAMEWORK
		public ValueTask<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
			=> _connection.BeginTransactionAsync(cancellationToken);

		public ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
			=> _connection.BeginTransactionAsync(isolationLevel, cancellationToken);
#else
		public Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
			=> _connection.BeginTransactionAsync(cancellationToken);

		public Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
			=> _connection.BeginTransactionAsync(isolationLevel, cancellationToken);
#endif

#if !NETSTANDARD2_1PLUS
		public Task CloseAsync()
#else
		public override Task CloseAsync()
#endif
		{
			return _connection.CloseAsync();
		}

#if NETSTANDARD2_1PLUS
#pragma warning disable CA2215 // CA2215: Dispose methods should call base class dispose
		public override ValueTask DisposeAsync()
#pragma warning restore CA2215 // CA2215: Dispose methods should call base class dispose
#elif !NETFRAMEWORK
		public ValueTask DisposeAsync()
#else
		public Task DisposeAsync()
#endif
		{
			return _connection.DisposeAsync();
		}

		public IAsyncDbConnection TryClone()
		{
			return AsyncFactory.Create((IDbConnection)Clone());
		}
	}
}
