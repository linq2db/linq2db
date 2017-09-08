using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using LinqToDB.Configuration;
#if !NOASYNC
using System.Threading;
using System.Threading.Tasks;
#endif

namespace LinqToDB.Data.RetryPolicy
{
	class RetryingDbConnection : DbConnection, IProxy<DbConnection>, IDisposable, ICloneable
	{
		readonly DataConnection _dataConnection;
		readonly DbConnection   _connection;
		readonly IRetryPolicy   _policy;

		public RetryingDbConnection(DataConnection dataConnection, DbConnection connection, IRetryPolicy policy)
		{
			_dataConnection = dataConnection;
			_connection = connection;
			_policy     = policy;
		}

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
		{
			return _connection.BeginTransaction();
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
		public override string          DataSource    => _connection.DataSource;
		public override string          ServerVersion => _connection.ServerVersion;

		protected override DbCommand CreateDbCommand()
		{
			return new RetryingDbCommand(_connection.CreateCommand(), _policy);
		}

#if !NOASYNC
		public override async Task OpenAsync(CancellationToken cancellationToken)
		{
			await _policy.ExecuteAsync(async ct => await _connection.OpenAsync(ct), cancellationToken);
		}
#endif

		void IDisposable.Dispose()
		{
			((IDisposable)_connection).Dispose();
		}

		public DbConnection UnderlyingObject => _connection;

#if !NETSTANDARD1_6
		public override DataTable GetSchema()
		{
			return _connection.GetSchema();
		}

		public override DataTable GetSchema(string collectionName)
		{
			return _connection.GetSchema(collectionName);
		}

		public override DataTable GetSchema(string collectionName, string[] restrictionValues)
		{
			return _connection.GetSchema(collectionName, restrictionValues);
		}

		public override ISite Site
		{
			get { return _connection.Site;  }
			set { _connection.Site = value; }
		}
#endif

		public override int ConnectionTimeout => _connection.ConnectionTimeout;

		public override event StateChangeEventHandler StateChange
		{
			add    => _connection.StateChange += value;
			remove => _connection.StateChange -= value;
		}

		public object Clone()
		{
			if (_connection is ICloneable)
				return ((ICloneable)_connection).Clone();
			return _dataConnection.DataProvider.CreateConnection(_dataConnection.ConnectionString);
		}
	}
}
