using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

namespace ModelInterfaceDataContext
{
	public partial interface ITestDataDB
	{
	}

	public class DataConnectionTestDataDB : DataConnection, ITestDataDB
	{
		public DataConnectionTestDataDB(DataOptions dataOptions) : base(dataOptions)
		{
			DataContext = this;
			Model       = this;
		}

		public IDataContext                 DataContext { get; set; }
		public TestSchemaSchema.DataContext TestSchema  { get; set; } = null!;

		public ITestDataDB Model { get; }

	}

	public class DataContextTestDataDB : DataContext, ITestDataDB
	{
		public DataContextTestDataDB(DataOptions dataOptions) : base(dataOptions)
		{
			DataContext = this;
			Model       = this;
		}

		public IDataContext                 DataContext { get; set; }
		public TestSchemaSchema.DataContext TestSchema  { get; set; } = null!;

		public ITestDataDB Model { get; }
	}

#if !NET462

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
				_ = db.AllTypes.ToList();
			}
		}
	}

#endif
}
