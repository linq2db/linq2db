using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class AllAnyTests : TestBase
	{
		[Test]
		public void Any1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p =>    Child.Where(c => c.ParentID == p.ParentID).Any(c => c.ParentID > 3)),
					db.Parent.Where(p => db.Child.Where(c => c.ParentID == p.ParentID).Any(c => c.ParentID > 3)));
		}

		[Test]
		public void Any2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p =>    Child.Where(c => c.ParentID == p.ParentID).Any()),
					db.Parent.Where(p => db.Child.Where(c => c.ParentID == p.ParentID).Any()));
		}

		[Test]
		public void Any3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.Any(c => c.ParentID > 3)),
					db.Parent.Where(p => p.Children.Any(c => c.ParentID > 3)));
		}

		[Test]
		public void Any31([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.ParentID > 0 && p.Children.Any(c => c.ParentID > 0 && c.ParentID > 3)),
					db.Parent.Where(p => p.ParentID > 0 && p.Children.Any(c => c.ParentID > 0 && c.ParentID > 3)));
		}

		[ExpressionMethod(nameof(SelectAnyExpression))]
		static bool SelectAny(Parent p)
		{
			return p.Children.Any(c => c.ParentID > 0 && c.ParentID > 3);
		}

		static Expression<Func<Parent,bool>> SelectAnyExpression()
		{
			return p => p.Children.Any(c => c.ParentID > 0 && c.ParentID > 3);
		}

		[Test]
		public void Any32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.ParentID > 0 && SelectAny(p)),
					db.Parent.Where(p => p.ParentID > 0 && SelectAny(p)));
		}

		[Test]
		public void Any4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.Any()),
					db.Parent.Where(p => p.Children.Any()));
		}

		[Test]
		public void Any5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.Any(c => c.GrandChildren.Any(g => g.ParentID > 3))),
					db.Parent.Where(p => p.Children.Any(c => c.GrandChildren.Any(g => g.ParentID > 3))));
		}

		[Test]
		public void Any6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Any(c => c.ParentID > 3),
					db.Child.Any(c => c.ParentID > 3));
		}

		[Test]
		public void Any7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(Child.Any(), db.Child.Any());
		}

		[Test]
		public void Any8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Select(c => c.Parent).Any(c => c == p),
					from p in db.Parent select db.Child.Select(c => c.Parent).Any(c => c == p));
		}

		[Test]
		public void Any9([DataSources] string context)
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

		[Test]
		public void Any10([DataSources] string context)
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

		[Test]
		public void Any11([DataSources] string context)
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

		[Test]
		public void Any12([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in             Parent    where             Child.   Any(c => p.ParentID == c.ParentID && c.ChildID > 3) select p,
					from p in db.GetTable<Parent>() where db.GetTable<Child>().Any(c => p.ParentID == c.ParentID && c.ChildID > 3) select p);
		}

		[Test]
		public void All1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p =>    Child.Where(c => c.ParentID == p.ParentID).All(c => c.ParentID > 3)),
					db.Parent.Where(p => db.Child.Where(c => c.ParentID == p.ParentID).All(c => c.ParentID > 3)));
		}

		[Test]
		public void All2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.All(c => c.ParentID > 3)),
					db.Parent.Where(p => p.Children.All(c => c.ParentID > 3)));
		}

		[Test]
		public void All3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.All(c => c.GrandChildren.All(g => g.ParentID > 3))),
					db.Parent.Where(p => p.Children.All(c => c.GrandChildren.All(g => g.ParentID > 3))));
		}

		[Test]
		public void All4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.All(c => c.ParentID > 3),
					db.Child.All(c => c.ParentID > 3));

		}

		[Test]
		public async Task All4Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					         Child.All     (c => c.ParentID > 3),
					await db.Child.AllAsync(c => c.ParentID > 3));
		}

		[Test]
		public void All5([DataSources] string context)
		{
			int n = 3;

			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.All(c => c.ParentID > n),
					db.Child.All(c => c.ParentID > n));
		}

		[Test]
		public void SubQueryAllAny([DataSources] string context)
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
		public void AllNestedTest([NorthwindDataContext] string context)
		{
			var dd = GetNorthwindAsList(context);

			using (var db = new NorthwindDB(context))
				AreEqual(
					from c in dd.Customer
					where dd.Order.Where(o => o.Customer == c).All(o => dd.Employee.Where(e => o.Employee == e).Any(e => e.FirstName.StartsWith("A")))
					select c,
					from c in db.Customer
					where db.Order.Where(o => o.Customer == c).All(o => db.Employee.Where(e => o.Employee == e).Any(e => e.FirstName.StartsWith("A")))
					select c);
		}

		[Test]
		public void ComplexAllTest([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					from o in dd.Order
					where
					dd.Customer.Where(c => c == o.Customer).All(c => c.CompanyName.StartsWith("A")) ||
					dd.Employee.Where(e => e == o.Employee).All(e => e.FirstName.EndsWith("t"))
					select o,
					from o in db.Order
					where
					db.Customer.Where(c => c == o.Customer).All(c => c.CompanyName.StartsWith("A")) ||
					db.Employee.Where(e => e == o.Employee).All(e => e.FirstName.EndsWith("t"))
					select o);
			}
		}
	}
}
