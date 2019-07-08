using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue928Tests : TestBase
	{
		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var subquery =
					from p in db.Parent
					where db.Child.Select(ch => ch.ParentID).Contains(p.ParentID)
					group p by p.ParentID into g
					select new { ParentID = g.Key, Sum = g.Sum(x => x.ParentID) };

				var q3 =
					from p1 in db.Parent//subquery
					from p2 in subquery.Where(o => o.ParentID == p1.ParentID).DefaultIfEmpty()
					select new { p1, p2 };

				q3.ToList();
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var subquery =
					from p in db.Parent
					where db.Child.Select(ch => ch.ParentID).Contains(p.ParentID)
					group p by p.ParentID into g
					select new { ParentID = g.Key, Sum = g.Sum(x => x.ParentID) };

				var q3 =
					from p1 in db.Parent//subquery
					from p2 in subquery.Where(o => o.ParentID == p1.ParentID)
					select new { p1, p2 };

				q3.ToList();
			}
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var subquery =
					from p in db.Parent
					where db.Child.Select(ch => ch.ParentID).Contains(p.ParentID)
					group p by p.ParentID into g
					select new { ParentID = g.Key, Sum1 = g.Sum(x => x.ParentID) };

				var q3 =
					from p1 in db.Parent
					from p2 in subquery.LeftJoin(o => o.ParentID == p1.ParentID)
					select new { p1, p2 };

				q3.ToList();
			}
		}

		[Test]
		public void Test4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var subquery =
					from p in db.Parent
					where db.Child.Select(ch => ch.ParentID).Contains(p.ParentID)
					group p by p.ParentID into g
					select new { ParentID = g.Key, Sum = g.Sum(x => x.ParentID) };

				var q3 =
					from p1 in subquery
					join p2 in subquery on p1.ParentID equals p2.ParentID into gp2
					from p2 in gp2.DefaultIfEmpty()
					select new { p1, p2 };

				q3.ToList();
			}
		}
	}
}
