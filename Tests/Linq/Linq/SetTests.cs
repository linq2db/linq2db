using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class SetTests : TestBase
	{
		[Test, DataContextSource]
		public void Except1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.Except(   Child.Where(p => p.ParentID == 3)),
					db.Child.Except(db.Child.Where(p => p.ParentID == 3)));
		}

		//[Test, DataContextSource]
		public void Except2(string context)
		{
			var ids = new[] { 1, 2 };

			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Where(c => c.GrandChildren.Select(_ => _.ParentID ?? 0).Except(ids).Count() == 0),
					db.Child.Where(c => c.GrandChildren.Select(_ => _.ParentID ?? 0).Except(ids).Count() == 0));
		}

		[Test, DataContextSource]
		public void Intersect(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.Intersect(   Child.Where(p => p.ParentID == 3)),
					db.Child.Intersect(db.Child.Where(p => p.ParentID == 3)));
		}

		[Test, DataContextSource]
		public void Contains1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Select(c => c.Parent).Contains(p),
					from p in db.Parent select db.Child.Select(c => c.Parent).Contains(p));
		}

		[Test, DataContextSource]
		public void Contains2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Select(c => c.ParentID).Contains(p.ParentID),
					from p in db.Parent select db.Child.Select(c => c.ParentID).Contains(p.ParentID));
		}

		[Test, DataContextSource]
		public void Contains201(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Select(c => c.ParentID).Contains(p.ParentID - 1),
					from p in db.Parent select db.Child.Select(c => c.ParentID).Contains(p.ParentID - 1));
		}

		[Test, DataContextSource]
		public void Contains3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where    Child.Select(c => c.Parent).Contains(p) select p,
					from p in db.Parent where db.Child.Select(c => c.Parent).Contains(p) select p);
		}

		[Test, DataContextSource]
		public void Contains4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where    Child.Select(c => c.ParentID).Contains(p.ParentID) select p,
					from p in db.Parent where db.Child.Select(c => c.ParentID).Contains(p.ParentID) select p);
		}

		[Test, DataContextSource]
		public void Contains5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where    Child.Select(c => c.ParentID).Contains(p.ParentID + 1) select p,
					from p in db.Parent where db.Child.Select(c => c.ParentID).Contains(p.ParentID + 1) select p);
		}

		[Test, DataContextSource]
		public void Contains6(string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where    Child.Select(c => c.ParentID).Contains(p.ParentID + n) select p,
					from p in db.Parent where db.Child.Select(c => c.ParentID).Contains(p.ParentID + n) select p);
		}

		[Test, DataContextSource]
		public void Contains7(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Select(c => c.ParentID).Contains(11),
					db.Child.Select(c => c.ParentID).Contains(11));
		}

		[Test, DataContextSource]
		public void Contains701(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Select(c => c.Parent).Contains(new Parent { ParentID = 11, Value1 = 11}),
					db.Child.Select(c => c.Parent).Contains(new Parent { ParentID = 11, Value1 = 11}));
		}

		[Test, DataContextSource]
		public void Contains8(string context)
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

		[Test, DataContextSource]
		public void Contains801(string context)
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

		[Test, DataContextSource]
		public void Contains802(string context)
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

		[Test, DataContextSource]
		public void Contains803(string context)
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

		[Test, DataContextSource]
		public void Contains9(string context)
		{
			var arr = Parent1.Take(2).ToArray();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent1 where arr.Contains(p) select p,
					from p in db.Parent1 where arr.Contains(p) select p);
		}

		[Test, NorthwindDataContext]
		public void Contains10(string context)
		{
			using (var db = new NorthwindDB())
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

		[Test, NorthwindDataContext]
		public void Contains11(string context)
		{
			using (var db = new NorthwindDB())
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

		[Test, NorthwindDataContext]
		public void Contains12(string context)
		{
			using (var db = new NorthwindDB())
			{
				var q =
					from e in db.EmployeeTerritory
					group e by e.Employee into g
					where g.Key.EmployeeTerritories.Count() > 1 && g.Count() > 2
					select new
					{
						g.Key.LastName,
						//cnt = g.Where(t => t.Employee.FirstName.Contains("an")).Count(),
					};

				q.ToList();
			}
		}

		[Test, NorthwindDataContext]
		public void Contains13(string context)
		{
			using (var db = new NorthwindDB())
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
			Assert.AreEqual(
				   Parent1.Where(p => p.ParentID == 1).Contains(parent),
				db.Parent1.Where(p => p.ParentID == 1).Contains(parent));
		}

		[Test, DataContextSource]
		public void Contains14(string context)
		{
			var ps = Parent1.OrderBy(p => p.ParentID).Take(2).ToArray();

			using (var db = GetDataContext(context))
				foreach (var p in ps)
					TestContains(db, p);
		}

		[Test, DataContextSource]
		public void Contains15(string context)
		{
			var arr = Parent1.Take(2).ToArray();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Child where arr.Contains(p.Parent1) select p,
					from p in db.Child where arr.Contains(p.Parent1) select p);
		}

		[Test, DataContextSource]
		public void Contains16(string context)
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
				Assert.AreEqual(d.First().Value, g.ParentID);
			}
		}

		[Test, DataContextSource]
		public void TestForGroupBy(string context)
		{
			using (var db = GetDataContext(context))
			{
				GetData(db, new List<int?> { 2 });
				GetData(db, new List<int?> { 3 });
			}
		}
	}
}
