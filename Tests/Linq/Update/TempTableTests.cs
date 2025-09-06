using System;
using System.Linq;

using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	public class TempTableTests : TestBase
	{
		class Table1
		{
			[PrimaryKey]
			public int ID;
		}

		class Table2
		{
			[PrimaryKey]
			public int       ID;
			public DateTime? Date;
		}

		[Test]
		public void InsertTest([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Table1>([new Table1 { ID = 10 }]);
			using var t2 = db.CreateLocalTable<Table2>();

			t2.Insert(from t in t1 select new Table2 { ID = t.ID });

			var result = t2.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result,         Has.Count.EqualTo(1));
				Assert.That(result[0].ID,   Is.EqualTo(10));
				Assert.That(result[0].Date, Is.Null);
			}
		}
	}
}
