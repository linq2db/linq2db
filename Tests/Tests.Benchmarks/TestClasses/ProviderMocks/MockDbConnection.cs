using System;
using System.Data;
using System.Data.Common;

namespace LinqToDB.Benchmarks.TestProvider
{
	public class MockDbConnection : DbConnection
	{
		private readonly QueryResult _result;

		private ConnectionState _state;

		public MockDbConnection(string connectionString, QueryResult result)
		{
			ConnectionString = connectionString;
			_result = result;
		}

		public MockDbConnection(QueryResult result, ConnectionState state)
		{
			_result = result;
			_state = state;
		}

		public override string ConnectionString { get; set; } = "MockDbConnection";

		public override string Database => throw new NotImplementedException();

		public override string DataSource => throw new NotImplementedException();

		public override string ServerVersion => throw new NotImplementedException();

		public override ConnectionState State => _state;

		public override void ChangeDatabase(string databaseName)
		{
			throw new NotImplementedException();
		}

		public override void Close()
		{
			_state = ConnectionState.Closed;
		}

		public override void Open()
		{
			_state = ConnectionState.Open;
		}

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
		{
			throw new NotImplementedException();
		}

		protected override DbCommand CreateDbCommand()
		{
			return new MockDbCommand(_result);
		}
	}
}
