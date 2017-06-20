using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	internal class RetryingDbConnection : DbConnection
	{
		private readonly DbConnection _connection;
		private readonly IRetryPolicy _policy;

		public RetryingDbConnection(DbConnection connection, IRetryPolicy policy)
		{
			_connection = connection;
			_policy = policy;
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
			_policy.Execute(() => { _connection.Open(); return 0;});
		}

		public override string ConnectionString
		{
			get { return _connection.ConnectionString; }
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
			return _policy.ExecuteAsync(
				ct =>
				{
					_connection.OpenAsync(ct);
					return Task.FromResult(0);
				},
				cancellationToken);
		}
#endif
	}
}