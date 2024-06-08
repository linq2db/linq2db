using System;
using System.Data;
using System.Data.Common;

namespace LinqToDB.Benchmarks.TestProvider
{
	public class MockDbTransaction : DbTransaction
	{
		public MockDbTransaction()
		{
		}

		public override IsolationLevel IsolationLevel => throw new NotImplementedException();

		protected override DbConnection DbConnection => throw new NotImplementedException();

		public override void Commit()
		{
			throw new NotImplementedException();
		}

		public override void Rollback()
		{
		}
	}
}
