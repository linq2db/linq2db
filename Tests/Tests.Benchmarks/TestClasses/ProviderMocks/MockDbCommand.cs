using System;
using System.Data;
using System.Data.Common;

namespace LinqToDB.Benchmarks.TestProvider
{
	public class MockDbCommand : DbCommand
	{
		private readonly QueryResult?   _result;
		private readonly QueryResult[]? _results;

		private readonly MockDbParameterCollection _parameters = new MockDbParameterCollection();

		public MockDbCommand(QueryResult result)
		{
			_result = result;
		}

		public MockDbCommand(QueryResult[] results)
		{
			_results = results;
		}

		public MockDbCommand(string command, QueryResult result)
		{
			CommandText = command;
			_result     = result;
		}

		public    override string?               CommandText              { get; set; }
		public    override CommandType           CommandType              { get; set; }
		protected override DbConnection?         DbConnection             { get; set; }
		protected override DbParameterCollection DbParameterCollection => _parameters;

		public override int CommandTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public override bool DesignTimeVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public override UpdateRowSource UpdatedRowSource { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		protected override DbTransaction? DbTransaction { get; set; }

		public override void Cancel() { }

		private QueryResult GetResult()
		{
			if (_result != null)
				return _result;

			for (var i = 0; i < _results!.Length; i++)
				if (_results[i].Match!(CommandText!))
					return _results[i];

			throw new NotImplementedException();
		}

		public override int ExecuteNonQuery()
		{
			return GetResult().Return;
		}

		public override object ExecuteScalar()
		{
			throw new NotImplementedException();
		}

		public override void Prepare()
		{
			throw new NotImplementedException();
		}

		protected override DbParameter CreateDbParameter()
		{
			return new MockDbParameter();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			return new MockDbDataReader(GetResult());
		}
	}
}
