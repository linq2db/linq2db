using LinqToDB;
using NUnit.Framework;
using System;
using System.Linq;
using System.Linq.Expressions;
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
		public void Insert([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

				Assert.AreEqual(3, count);
				Assert.AreEqual(2, db.Types.Count(x => x.ID > 1000));
				Assert.AreEqual(1, db.Child.Count(x => x.ChildID == 1003));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void InsertAll([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

				Assert.AreEqual(2, count);
				Assert.AreEqual(1, db.Types.Count(x => x.ID > 1000));
				Assert.AreEqual(1, db.Child.Count(x => x.ChildID == 1003));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void InsertFirst([IncludeDataSources(true, TestProvName.AllOracle)] string context)
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

				Assert.AreEqual(1, count);
				Assert.AreEqual(1, db.Types.Count(x => x.ID > 1000));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void Parameters([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int id = 3000, newId2 = 3002;
				int? checkNull = 0;

				var command = db
					.SelectQuery(() => new { ID = id, N = 42 })
					.MultiInsert()
					.When(
						x => checkNull == null,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, GuidValue = Sql.NewGuid() }
					)
					.When(
						x => x.N > 40,
						db.Types,
						x => new LinqDataTypes { ID = newId2, GuidValue = Sql.NewGuid() }
					);

				// Perform a simple INSERT ALL with parameters
				int count = command.InsertAll();
				Assert.AreEqual(1, count);
				Assert.AreEqual(1, db.Types.Count(x => x.ID > 3000));
				Assert.AreEqual(0, db.Types.Count(x => x.ID > 4000));
				
				Cleanup(db);

				// Perform the same INSERT ALL with different parameter values
				id        = 4000; 
				checkNull = null; 
				newId2    = 4002;
				count     = command.InsertAll();
				Assert.AreEqual(2, count);
				Assert.AreEqual(2, db.Types.Count(x => x.ID > 4000));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void Expressions([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				// Perform a simple INSERT ALL with some expressions
				int count = InsertAll(
					() => new TestSource { ID = 3000, N = 42 },
					x  => x.N < 0, 
					x  => new LinqDataTypes { ID = 3002, GuidValue = Sql.NewGuid() });

				Assert.AreEqual(1, count);
				Assert.AreEqual(1, db.Types.Count(x => x.ID > 3000));
				Assert.AreEqual(0, db.Types.Count(x => x.ID > 4000));

				Cleanup(db);

				// Perform the same INSERT ALL with different expressions
				count = InsertAll(
					() => new TestSource { ID = 4000, N = 42 }, 
					x  => true, 
					x  => new LinqDataTypes { ID = 4002, GuidValue = Sql.NewGuid() });

				Assert.AreEqual(2, count);
				Assert.AreEqual(2, db.Types.Count(x => x.ID > 4000));
			}
			finally
			{
				Cleanup(db);
			}

			int InsertAll(
				Expression<Func<TestSource>>                source,
				Expression<Func<TestSource, bool>>          condition1,
				Expression<Func<TestSource, LinqDataTypes>> setter2)
			{
				return db
					.SelectQuery(source)
					.MultiInsert()
					.When(
						condition1,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, GuidValue = Sql.NewGuid() }
					)
					.When(
						x => x.N > 40,
						db.Types,
						setter2
					)
					.InsertAll();
			}			
		}

		class TestSource
		{
			public int ID { get; set; }
			public int N  { get; set; }
		}
	}
}
