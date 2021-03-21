using LinqToDB;
using NUnit.Framework;
using System.Linq;
using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	public class MultiInsertTests : TestBase
	{
		private void Cleanup(ITestDataContext db)
		{
			db.Types.Delete(x => x.ID > 1000);
			db.Child.Delete(x => x.ChildID > 1000);
		}

		[Test]
		public void Insert([DataSources(TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{				
				int count = db
					.SelectQuery(() => new { ID = 1000, N = 42 })
					.MultiInsert()
					.Into(
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, GuidValue = Sql.NewGuid() }
					)
					.Into(
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 2, GuidValue = Sql.NewGuid() }
					)
					.Into(
						db.Child,
						x => new Child { ParentID = x.ID + 1, ChildID = x.ID + 3 }
					)
					.Insert();

				Assert.AreEqual(count, 3);
				Assert.AreEqual(2, db.Types.Count(x => x.ID > 1000));
				Assert.AreEqual(1, db.Child.Count(x => x.ChildID == 1003));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void InsertAll([DataSources(TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{				
				int count = db
					.SelectQuery(() => new { ID = 1000, N = 42 })
					.MultiInsert()
					.When(
						x => x.N > 40,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, GuidValue = Sql.NewGuid() }
					)
					.When(
						x => x.N < 40,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 2, GuidValue = Sql.NewGuid() }
					)
					.When(
						x => true,
						db.Child,
						x => new Child { ParentID = x.ID + 1, ChildID = x.ID + 3 }
					)
					.InsertAll();

				Assert.AreEqual(count, 2);
				Assert.AreEqual(1, db.Types.Count(x => x.ID > 1000));
				Assert.AreEqual(1, db.Child.Count(x => x.ChildID == 1003));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void InsertFirst([DataSources(TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{				
				int count = db
					.SelectQuery(() => new { ID = 1000, N = 42 })
					.MultiInsert()
					.When(
						x => x.N < 40,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, GuidValue = Sql.NewGuid() }
					)
					.When(
						x => false,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 2, GuidValue = Sql.NewGuid() }
					)
					.Else(
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 3, GuidValue = Sql.NewGuid() }
					)
					.InsertFirst();

				Assert.AreEqual(count, 1);
				Assert.AreEqual(1, db.Types.Count(x => x.ID > 1000));
			}
			finally
			{
				Cleanup(db);
			}
		}
	}
}
