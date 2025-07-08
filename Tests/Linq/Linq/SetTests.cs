using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Shouldly;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Tools;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class SetTests : TestBase
	{
		[Test]
		public void Except1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.Except(   Child.Where(p => p.ParentID == 3)),
					db.Child.Except(db.Child.Where(p => p.ParentID == 3)));
		}

		//[Test]
		//public void Except2([DataSources] string context)
		//{
		//	var ids = new[] { 1, 2 };

		//	using (var db = GetDataContext(context))
		//		Assert.That(
		//			db.Child.Where(c => c.GrandChildren.Select(_ => _.ParentID ?? 0).Except(ids).Count() == 0), Is.EqualTo(Child.Where(c => c.GrandChildren.Select(_ => _.ParentID ?? 0).Except(ids).Count() == 0)));
		//}

		[Test]
		public void Intersect([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.Intersect(   Child.Where(p => p.ParentID == 3)),
					db.Child.Intersect(db.Child.Where(p => p.ParentID == 3)));
		}

		[Test]
		public void Contains1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Select(c => c.Parent).Contains(p),
					from p in db.Parent select db.Child.Select(c => c.Parent).Contains(p));
		}

		[Test]
		public void Contains2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Select(c => c.ParentID).Contains(p.ParentID),
					from p in db.Parent select db.Child.Select(c => c.ParentID).Contains(p.ParentID));
		}

		[Test]
		public void Contains201([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Select(c => c.ParentID).Contains(p.ParentID - 1),
					from p in db.Parent select db.Child.Select(c => c.ParentID).Contains(p.ParentID - 1));
		}

		[Test]
		public void Contains3([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where    Child.Select(c => c.Parent).Contains(p) select p,
					from p in db.Parent where db.Child.Select(c => c.Parent).Contains(p) select p);
		}

		[Test]
		public void Contains4([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where    Child.Select(c => c.ParentID).Contains(p.ParentID) select p,
					from p in db.Parent where db.Child.Select(c => c.ParentID).Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains5([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where    Child.Select(c => c.ParentID).Contains(p.ParentID + 1) select p,
					from p in db.Parent where db.Child.Select(c => c.ParentID).Contains(p.ParentID + 1) select p);
		}

		[Test]
		public void Contains6([DataSources(TestProvName.AllClickHouse)] string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where    Child.Select(c => c.ParentID).Contains(p.ParentID + n) select p,
					from p in db.Parent where db.Child.Select(c => c.ParentID).Contains(p.ParentID + n) select p);
		}

		[Test]
		public void Contains7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Child.Select(c => c.ParentID).Contains(11), Is.EqualTo(Child.Select(c => c.ParentID).Contains(11)));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void Contains701([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Child.Select(c => c.Parent).Contains(new Parent { ParentID = 11, Value1 = 11}), Is.EqualTo(Child.Select(c => c.Parent).Contains(new Parent { ParentID = 11, Value1 = 11})));
		}

		[Test]
		public void Contains8([DataSources] string context)
		{
			var arr = new[] { GrandChild.ElementAt(0), GrandChild.ElementAt(1) };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join ch in Child on p.ParentID equals ch.ParentID
					join gc in GrandChild on ch.ChildID equals gc.ChildID
					where arr.Contains(gc)
					select p
					,
					from p in db.Parent
					join ch in db.Child on p.ParentID equals ch.ParentID
					join gc in db.GrandChild on ch.ChildID equals gc.ChildID
					where arr.Contains(gc)
					select p);
		}

		[Test]
		public void NotContains8([DataSources] string context)
		{
			var arr = new[] { GrandChild.ElementAt(0), GrandChild.ElementAt(1) };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join ch in Child on p.ParentID equals ch.ParentID
					join gc in GrandChild on ch.ChildID equals gc.ChildID
					where !arr.Contains(gc)
					select p
					,
					from p in db.Parent
					join ch in db.Child on p.ParentID equals ch.ParentID
					join gc in db.GrandChild on ch.ChildID equals gc.ChildID
					where !arr.Contains(gc)
					select p);
		}

		[Test]
		public void Contains801([DataSources] string context)
		{
			var arr = new[] { GrandChild.ElementAt(0), GrandChild.ElementAt(1) };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join ch in Child      on p.ParentID equals ch.ParentID
					join gc in GrandChild on ch.ChildID equals gc.ChildID
					select new GrandChild { ParentID = 2, ChildID = ch.ChildID, GrandChildID = gc.GrandChildID } into gc
					where arr.Contains(gc)
					select gc
					,
					from p in db.Parent
					join ch in db.Child      on p.ParentID equals ch.ParentID
					join gc in db.GrandChild on ch.ChildID equals gc.ChildID
					select new GrandChild { ParentID = 2, ChildID = ch.ChildID, GrandChildID = gc.GrandChildID } into gc
					where arr.Contains(gc)
					select gc);
		}

		[Test]
		public void Contains802([DataSources] string context)
		{
			var arr = new[] { GrandChild.ElementAt(0), GrandChild.ElementAt(1) };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join ch in Child on p.ParentID equals ch.ParentID
					join gc in GrandChild on ch.ChildID equals gc.ChildID
					where arr.Contains(new GrandChild { ParentID = p.ParentID, ChildID = ch.ChildID, GrandChildID = gc.GrandChildID })
					select p
					,
					from p in db.Parent
					join ch in db.Child on p.ParentID equals ch.ParentID
					join gc in db.GrandChild on ch.ChildID equals gc.ChildID
					where arr.Contains(new GrandChild { ParentID = p.ParentID, ChildID = ch.ChildID, GrandChildID = gc.GrandChildID })
					select p);
		}

		[Test]
		public void Contains803([DataSources] string context)
		{
			var arr = new[] { GrandChild.ElementAt(0), GrandChild.ElementAt(1) };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					join ch in Child on p.ParentID equals ch.ParentID
					join gc in GrandChild on ch.ChildID equals gc.ChildID
					where arr.Contains(new GrandChild { ParentID = 1, ChildID = ch.ChildID, GrandChildID = gc.GrandChildID })
					select p
					,
					from p in db.Parent
					join ch in db.Child on p.ParentID equals ch.ParentID
					join gc in db.GrandChild on ch.ChildID equals gc.ChildID
					where arr.Contains(new GrandChild { ParentID = 1, ChildID = ch.ChildID, GrandChildID = gc.GrandChildID })
					select p);
		}

		[Test]
		public void Contains9([DataSources] string context)
		{
			var arr = Parent1.Take(2).ToArray();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent1 where arr.Contains(p) select p,
					from p in db.Parent1 where arr.Contains(p) select p);
		}

		[Test]
		public void Contains10([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var arr = new[]
				{
					new Northwind.Order { OrderID = 11000 },
					new Northwind.Order { OrderID = 11001 },
					new Northwind.Order { OrderID = 11002 }
				};

				var q =
					from e in db.Employee
					from o in e.Orders
					where arr.Contains(o)
					select new
					{
						e.FirstName,
						o.OrderID,
					};

				q.ToList();
			}
		}

		[Test]
		public void Contains11([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from e in db.EmployeeTerritory
					group e by e.Employee into g
					where g.Key.EmployeeTerritories.Count() > 1
					select new
					{
						g.Key.LastName,
						cnt = g.Where(t => t.Employee.FirstName.Contains("an")).Count(),
					};

				q.ToList();
			}
		}

		[Test]
		public void Contains12([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from e in db.EmployeeTerritory
					group e by e.Employee into g
					where g.Key.EmployeeTerritories.Count() > 1 && g.Count() > 2
					select new
					{
						g.Key.LastName,
						cnt = g.Where(t => t.Employee.FirstName.Contains("an")).Count(),
					};

				q.ToList();
			}
		}

		[Test]
		public void Contains13([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var arr = new[]
				{
					new Northwind.EmployeeTerritory { EmployeeID = 1, TerritoryID = "01581" },
					new Northwind.EmployeeTerritory { EmployeeID = 1, TerritoryID = "02116" },
					new Northwind.EmployeeTerritory { EmployeeID = 1, TerritoryID = "31406" }
				};

				var q =
					from e in db.EmployeeTerritory
					group e by e.EmployeeID into g
					select new
					{
						g.Key,
						cnt = g.Count(t => arr.Contains(t)),
					};

				q.ToList();
			}
		}

		void TestContains(ITestDataContext db, Parent1 parent)
		{
			Assert.That(
				db.Parent1.Where(p => p.ParentID == 1).Contains(parent), Is.EqualTo(Parent1.Where(p => p.ParentID == 1).Contains(parent)));
		}

		[Test]
		public void Contains14([DataSources] string context)
		{
			var ps = Parent1.OrderBy(p => p.ParentID).Take(2).ToArray();

			using (var db = GetDataContext(context))
				foreach (var p in ps)
					TestContains(db, p);
		}

		[Test]
		public void Contains15([DataSources] string context)
		{
			var arr = Parent1.Take(2).ToArray();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Child where arr.Contains(p.Parent1) select p,
					from p in db.Child where arr.Contains(p.Parent1) select p);
		}

		[Test]
		public void Contains16([DataSources] string context)
		{
			var arr = Child.Take(2).ToArray();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    GrandChild where arr.Contains(p.Child) select p,
					from p in db.GrandChild where arr.Contains(p.Child) select p);
		}

		static void GetData(ITestDataContext db, IEnumerable<int?> d)
		{
			var r1 = db.GrandChild
				.Where(x => d.Contains(x.ParentID))
				.ToList();

			foreach (var g in r1)
			{
				Assert.That(g.ParentID, Is.EqualTo(d.First()!.Value));
			}
		}

		[Test]
		public void TestForGroupBy([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				GetData(db, new List<int?> { 2 });
				GetData(db, new List<int?> { 3 });
			}
		}

		[Test]
		public void Issue3017([IncludeDataSources(/*TestProvName.AllSqlServer, */TestProvName.AllClickHouse)] string context)
		{
			using var scope = new DisableBaseline("Multithreading");

			var tasks = new List<Task>();

			for (var i = 0; i < 30; i++)
			{
				var local = i;
				tasks.Add(Task.Run(() => Issue3017Action(context)));
			}

			Task.WaitAll(tasks.ToArray());

			foreach (var task in tasks)
				Assert.That(task.IsFaulted, Is.False);
		}

		private async Task Issue3017Action(string context)
		{
			using (var db = GetDataContext(context))
			{
				var targets = db.Person.ToArray();

				var keys = targets.Select(r => new { r.ID, r.FirstName, r.LastName });

				await db.Person.Where(r => new { r.ID, r.FirstName, r.LastName }.In(keys)).ToListAsync();
			}
		}
	}
}
