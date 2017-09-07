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
			get { return _connection.ConnectionString;  }
			set { _connection.ConnectionString = value; }
		}

		public override string Database
		{
			get { return _connection.Database; }
		}

		public override ConnectionState State
		{
			get { return _connection.State; }
		}

		public override string DataSource
		{
			get { return _connection.DataSource; }
		}

		public override string ServerVersion
		{
			get { return _connection.ServerVersion; }
		}

		protected override DbCommand CreateDbCommand()
		{
			return new RetryingDbCommand(_connection.CreateCommand(), _policy);
		}

#if !NOASYNC
		public override Task OpenAsync(CancellationToken cancellationToken)
		{
			return _policy.ExecuteAsync(ct => _connection.OpenAsync(ct), cancellationToken);
		}
#endif

		void IDisposable.Dispose()
		{
			((IDisposable)_connection).Dispose();
		}

		public DbConnection UnderlyingObject
		{
			get { return _connection; }
		}

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

		public override int ConnectionTimeout
		{
			get { return _connection.ConnectionTimeout; }
		}

		public override event StateChangeEventHandler StateChange
		{
			add    { _connection.StateChange += value; }
			remove { _connection.StateChange -= value; }
		}

		public object Clone()
		{
			if (_connection is ICloneable)
				return ((ICloneable)_connection).Clone();
			return _dataConnection.DataProvider.CreateConnection(_dataConnection.ConnectionString);
		}
	}
}
