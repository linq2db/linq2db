using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;


namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ColumnAliasTests : TestBase
	{
		public enum TestValue
		{
			Value1 = 1,
		}

		[Table("Parent")]
		class TestParent
		{
			[Column] public int       ParentID;
			[Column] public TestValue Value1;

			[ColumnAlias("ParentID")] public int ID;
		}

		[Test]
		public void AliasTest1()
		{
			using (var db = new TestDataConnection())
			{
				var count = db.GetTable<TestParent>().Count(t => t.ID > 0);
			}
		}

		[Test]
		public void AliasTest2()
		{
			using (var db = new TestDataConnection())
			{
				db.GetTable<TestParent>()
					.Where(t => t.ID < 0 && t.ID > 0)
					.Update(t => new TestParent
					{
						ID = t.ID - 1
					});
			}
		}

		[Test, DataContextSource]
		public void ProjectionTest1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GetTable<TestParent>()
					select new TestParent
					{
						ID     = p.ID,
						Value1 = p.Value1,
					} into p
					where p.ParentID > 1
					select p;

				var count = q.Count();

				Assert.That(count, Is.GreaterThan(0));
			}
		}

		[Test, DataContextSource]
		public void ProjectionTest2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GetTable<TestParent>()
					select new TestParent
					{
						ParentID = p.ParentID,
						Value1   = p.Value1,
					} into p
					where p.ID > 1
					select p;

				var count = q.Count();

				Assert.That(count, Is.GreaterThan(0));
			}
		}

		[Test, DataContextSource]
		public void UnionTest1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
				(
					from p in db.GetTable<TestParent>()
					where p.ID > 2
					select new
					{
						p.ID
					}
				).Union(
					from p in db.GetTable<TestParent>()
					where p.ID > 2
					select new
					{
						p.ID
					}
				);

				var count = q.Count();

				Assert.That(count, Is.GreaterThan(0));
			}
		}

		[Test, DataContextSource]
		public void UnionTest2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GetTable<TestParent>().Union(db.GetTable<TestParent>())
					where p.ID > 1
					select p;

				var count = q.Count();

				Assert.That(count, Is.GreaterThan(0));
			}
		}

		[Test, DataContextSource]
		public void UnionTest3(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GetTable<TestParent>().Union(db.GetTable<TestParent>())
					select new
					{
						p.ID
					};

				var count = q.ToList().Count;

				Assert.That(count, Is.GreaterThan(0));
			}
		}
	}
}
