using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class SetTest : TestBase
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
		public void Any1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p =>    Child.Where(c => c.ParentID == p.ParentID).Any(c => c.ParentID > 3)),
					db.Parent.Where(p => db.Child.Where(c => c.ParentID == p.ParentID).Any(c => c.ParentID > 3)));
		}

		[Test, DataContextSource]
		public void Any2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p =>    Child.Where(c => c.ParentID == p.ParentID).Any()),
					db.Parent.Where(p => db.Child.Where(c => c.ParentID == p.ParentID).Any()));
		}

		[Test, DataContextSource]
		public void Any3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.Any(c => c.ParentID > 3)),
					db.Parent.Where(p => p.Children.Any(c => c.ParentID > 3)));
		}

		[Test, DataContextSource]
		public void Any31(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.ParentID > 0 && p.Children.Any(c => c.ParentID > 0 && c.ParentID > 3)),
					db.Parent.Where(p => p.ParentID > 0 && p.Children.Any(c => c.ParentID > 0 && c.ParentID > 3)));
		}

		[ExpressionMethod("SelectAnyExpression")]
		static bool SelectAny(Parent p)
		{
			return p.Children.Any(c => c.ParentID > 0 && c.ParentID > 3);
		}

		static Expression<Func<Parent,bool>> SelectAnyExpression()
		{
			return p => p.Children.Any(c => c.ParentID > 0 && c.ParentID > 3);
		}

		[Test, DataContextSource]
		public void Any32(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.ParentID > 0 && SelectAny(p)),
					db.Parent.Where(p => p.ParentID > 0 && SelectAny(p)));
		}

		[Test, DataContextSource]
		public void Any4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.Any()),
					db.Parent.Where(p => p.Children.Any()));
		}

		[Test, DataContextSource]
		public void Any5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.Any(c => c.GrandChildren.Any(g => g.ParentID > 3))),
					db.Parent.Where(p => p.Children.Any(c => c.GrandChildren.Any(g => g.ParentID > 3))));
		}

		[Test, DataContextSource]
		public void Any6(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Any(c => c.ParentID > 3),
					db.Child.Any(c => c.ParentID > 3));
		}

		[Test, DataContextSource]
		public void Any7(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(Child.Any(), db.Child.Any());
		}

		[Test, DataContextSource]
		public void Any8(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Select(c => c.Parent).Any(c => c == p),
					from p in db.Parent select db.Child.Select(c => c.Parent).Any(c => c == p));
		}

		[Test, DataContextSource]
		public void Any9(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in 
						from p in Parent
						from g in p.GrandChildren
						join c in Child on g.ChildID equals c.ChildID
						select c
					where !p.GrandChildren.Any(x => x.ParentID < 0)
					select p,
					from p in 
						from p in db.Parent
						from g in p.GrandChildren
						join c in db.Child on g.ChildID equals c.ChildID
						select c
					where !p.GrandChildren.Any(x => x.ParentID < 0)
					select p);
		}

		[Test, DataContextSource]
		public void Any10(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in 
						from p in Parent
						from g in p.GrandChildren
						join c in Child on g.ChildID equals c.ChildID
						select p
					where !p.GrandChildren.Any(x => x.ParentID < 0)
					select p,
					from p in 
						from p in db.Parent
						from g in p.GrandChildren
						join c in db.Child on g.ChildID equals c.ChildID
						select p
					where !p.GrandChildren.Any(x => x.ParentID < 0)
					select p);
		}

		[Test, DataContextSource]
		public void Any11(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in 
						from p in Parent
						from g in p.GrandChildren
						join c in Child on g.ChildID equals c.ChildID
						join t in Types on c.ParentID equals t.ID
						select c
					where !p.GrandChildren.Any(x => x.ParentID < 0)
					select p,
					from p in 
						from p in db.Parent
						from g in p.GrandChildren
						join c in db.Child on g.ChildID equals c.ChildID
						join t in db.Types on c.ParentID equals t.ID
						select c
					where !p.GrandChildren.Any(x => x.ParentID < 0)
					select p);
		}

		[Test, DataContextSource]
		public void Any12(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in             Parent    where             Child.   Any(c => p.ParentID == c.ParentID && c.ChildID > 3) select p,
					from p in db.GetTable<Parent>() where db.GetTable<Child>().Any(c => p.ParentID == c.ParentID && c.ChildID > 3) select p);
		}

		[Test, DataContextSource]
		public void All1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p =>    Child.Where(c => c.ParentID == p.ParentID).All(c => c.ParentID > 3)),
					db.Parent.Where(p => db.Child.Where(c => c.ParentID == p.ParentID).All(c => c.ParentID > 3)));
		}

		[Test, DataContextSource]
		public void All2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.All(c => c.ParentID > 3)),
					db.Parent.Where(p => p.Children.All(c => c.ParentID > 3)));
		}

		[Test, DataContextSource]
		public void All3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.All(c => c.GrandChildren.All(g => g.ParentID > 3))),
					db.Parent.Where(p => p.Children.All(c => c.GrandChildren.All(g => g.ParentID > 3))));
		}

		[Test, DataContextSource]
		public void All4(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.All(c => c.ParentID > 3),
					db.Child.All(c => c.ParentID > 3));
		}

		[Test, DataContextSource]
		public void All5(string context)
		{
			int n = 3;

			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.All(c => c.ParentID > n),
					db.Child.All(c => c.ParentID > n));
		}

		[Test, DataContextSource]
		public void SubQueryAllAny(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Parent
					where    Child.Where(o => o.Parent == c).All(o =>    Child.Where(e => o == e).Any(e => e.ChildID > 10))
					select c,
					from c in db.Parent
					where db.Child.Where(o => o.Parent == c).All(o => db.Child.Where(e => o == e).Any(e => e.ChildID > 10))
					select c);
		}

		[Test]
		public void AllNestedTest([IncludeDataContexts("Northwind")] string context)
		{
			using (var db = new NorthwindDB())
				AreEqual(
					from c in    Customer
					where    Order.Where(o => o.Customer == c).All(o =>    Employee.Where(e => o.Employee == e).Any(e => e.FirstName.StartsWith("A")))
					select c,
					from c in db.Customer
					where db.Order.Where(o => o.Customer == c).All(o => db.Employee.Where(e => o.Employee == e).Any(e => e.FirstName.StartsWith("A")))
					select c);
		}

		[Test]
		public void ComplexAllTest([IncludeDataContexts("Northwind")] string context)
		{
			using (var db = new NorthwindDB())
				AreEqual(
					from o in Order
					where
						Customer.Where(c => c == o.Customer).All(c => c.CompanyName.StartsWith("A")) ||
						Employee.Where(e => e == o.Employee).All(e => e.FirstName.EndsWith("t"))
					select o,
					from o in db.Order
					where
						db.Customer.Where(c => c == o.Customer).All(c => c.CompanyName.StartsWith("A")) ||
						db.Employee.Where(e => e == o.Employee).All(e => e.FirstName.EndsWith("t"))
					select o);
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

		[Test]
		public void Contains10([IncludeDataContexts("Northwind")] string context)
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

		[Test]
		public void Contains11([IncludeDataContexts("Northwind")] string context)
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

		[Test]
		public void Contains12([IncludeDataContexts("Northwind")] string context)
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

		[Test]
		public void Contains13([IncludeDataContexts("Northwind")] string context)
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
				Array.ForEach(ps, p => TestContains(db, p));
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
