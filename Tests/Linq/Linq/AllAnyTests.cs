using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class AllAnyTests : TestBase
	{
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

		[Test, NorthwindDataContext]
		public void AllNestedTest(string context)
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

		[Test, NorthwindDataContext]
		public void ComplexAllTest(string context)
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
	}
}
