using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2008Tests : TestBase
	{
		[Table]
		public class Table1
		{
			[Column]
			public int Id_1 { get; set; }
			[Column]
			public int Description { get; set; }
		}

		[Table]
		public class Table2
		{
			[Column]
			public int Id_2 { get; set; }
			[Column]
			public int Description { get; set; }
		}

		[Table]
		public class Table3
		{
			[Column]
			public int Id_3 { get; set; }
			[Column]
			public int Description { get; set; }
		}

		[Test]
		public void Issue2008Test([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Table1>())
			using (db.CreateLocalTable<Table2>())
			using (db.CreateLocalTable<Table3>())
			{
				var query = from rTable1 in db.GetTable<Table1>()
					from rTable2 in db.GetTable<Table2>().Where(r2 => r2.Id_2 == rTable1.Id_1 &&
					                                                  db.GetTable<Table3>().Any(r3 =>
						                                                  r3.Id_3 == rTable1.Id_1)
					).DefaultIfEmpty()
					select new { rTable1, rTable2 };
				var result = query.ToList();
			}
		}
	}
}
