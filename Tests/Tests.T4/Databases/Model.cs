using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

namespace ModelDataContext
{
	public partial class TestDataDB
	{
	}

	public interface ITestDataDB : IDataContext
	{
		TestDataDB Model { get; }
	}

	public class DataConnectionTestDataDB : DataConnection, ITestDataDB
	{
		public DataConnectionTestDataDB(DataOptions dataOptions) : base(dataOptions)
		{
			Model = new TestDataDB(this);
		}

		public TestDataDB Model { get; }
	}

	public class DataContextTestDataDB : DataContext, ITestDataDB
	{
		public DataContextTestDataDB(DataOptions dataOptions) : base(dataOptions)
		{
			Model = new TestDataDB(this);
		}

		public TestDataDB Model { get; }
	}

	class Test
	{
		static void UseModel()
		{
			using var db1 = new DataConnectionTestDataDB(new DataOptions());

			_ = db1.Model.AllTypes.ToList();

			using var db2 = new DataContextTestDataDB(new DataOptions());

			_ = db2.Model.AllTypes.ToList();

			TestContext(db1);
			TestContext(db2);

			static void TestContext(ITestDataDB db)
			{
				_ = db.Model.AllTypes.ToList();
			}
		}
	}
}
