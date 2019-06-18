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
			return _dbConnection.BeginTransaction();
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

		public
#if !NET40
			override
#endif
			async Task OpenAsync(CancellationToken cancellationToken)
		{
			await _policy.ExecuteAsync(async ct => await _connection.OpenAsync(ct), cancellationToken);
		}

		void IDisposable.Dispose()
		{
			((IDisposable)_connection).Dispose();
		}

		public DbConnection UnderlyingObject => _dbConnection;

#if !NETSTANDARD1_6
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
#endif

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
			return _dataConnection.DataProvider.CreateConnection(_dataConnection.ConnectionString);
		}

		public Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			return _connection.BeginTransactionAsync(cancellationToken);
		}

		public Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
		{
			return _connection.BeginTransactionAsync(isolationLevel, cancellationToken);
		}

		public Task CloseAsync(CancellationToken cancellationToken = default)
		{
			return _connection.CloseAsync(cancellationToken);
		}

		public IAsyncDbConnection TryClone()
		{
			return AsyncFactory.Create((IDbConnection)Clone());
		}
	}
}
