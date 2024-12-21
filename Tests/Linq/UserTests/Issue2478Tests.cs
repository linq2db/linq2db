using System.Linq;
using LinqToDB;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2478Tests : TestBase
	{
		[Test]
		public void CrossApplyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.GetTable<Parent>()
					from c in (from t in db.GetTable<Child>()
						where t.ParentID == p.ParentID
						group t by 1
						into g
						select new { Count = g.Count(), Sum = g.Sum(_ => _.ChildID) })
					select new { p.ParentID, Count = c == null ? 0 : c.Count, Sum = c == null ? 0 : c.Sum };

				var result = query.ToArray();
				var cnt    = query.Count();
				
				Assert.That(cnt, Is.EqualTo(result.Length));
			}
		}

		[Test]
		public void CrossApplyParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] bool shouldFilter, [Values] bool isPositive)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.GetTable<Parent>()
					from c in (from t in db.GetTable<Child>()
						where t.ParentID == p.ParentID
						group t by 1
						into g
						select new { Count = g.Count(), Sum = g.Sum(_ => _.ChildID) })
					select new { p.ParentID, Count = c.Count, Sum = c.Sum };

				query = query.Where(q => shouldFilter ? q.Count > 0 : isPositive);

				var result = query.ToArray();
				var cnt    = query.Count();
				
				Assert.That(cnt, Is.EqualTo(result.Length));
			}
		}

		[Test]
		public void CrossApplyTestExt([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.GetTable<Parent>()
					from c in (from t in db.GetTable<Child>()
						where t.ParentID == p.ParentID
						select new { Count = Sql.Ext.Count().ToValue(), Sum = Sql.Ext.Sum(t.ChildID).ToValue() })
					select new { p.ParentID, Count = c == null ? 0 : c.Count, Sum = c == null ? 0 : c.Sum };

				var result = query.ToArray();
				var cnt    = query.Count();
				
				Assert.That(cnt, Is.EqualTo(result.Length));
			}
		}

		[Test]
		public void CrossApplyTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.GetTable<Parent>()
					// where p.ParentID <= 2
					from c in (from t in db.GetTable<Child>()
						where t.ParentID != p.ParentID && p.ParentID <= 2
						group t by 1
						into g
						select new { Count = g.Count(), Sum = g.Sum(_ => _.ChildID) })
					select new { p.ParentID, Count = c == null ? 0 : c.Count, Sum = c == null ? 0 : c.Sum };

				var result = query.ToArray();
				var cnt    = query.Count();
				
				Assert.That(cnt, Is.EqualTo(result.Length));
			}
		}

		[Test]
		public void OuterApplyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.GetTable<Parent>()
					from c in (from t in db.GetTable<Child>()
						where t.ParentID == p.ParentID
						group t by 1
						into g
						select new { Count = g.Count() }).DefaultIfEmpty()
					select new { p.ParentID, Count = c == null ? 0 : c.Count };

				var result = query.ToArray();
				var cnt    = query.Count();
				
				Assert.That(cnt, Is.EqualTo(result.Length));
			}
		}

		[Test]
		public void ExistsRemoval([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.GetTable<Parent>()
					where (from t in db.GetTable<Child>()
						where t.ParentID == p.ParentID
						group t by 1
						into g
						select new { Count = g.Count(), Sum = g.Sum(_ => _.ChildID) }).Any()
					select p;

				var result = query.ToArray();
				var cnt    = query.Count();

				Assert.Multiple(() =>
				{
					Assert.That(query.ToSqlQuery().Sql, Does.Not.Contain("EXISTS"));

					Assert.That(cnt, Is.EqualTo(result.Length));
				});
			}
		}

	}
}
