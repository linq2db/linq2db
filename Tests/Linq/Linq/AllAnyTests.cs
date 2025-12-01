using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class AllAnyTests : TestBase
	{
		[YdbMemberNotFound]
		[Test]
		public void Any1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p =>    Child.Where(c => c.ParentID == p.ParentID).Any(c => c.ParentID > 3)),
					db.Parent.Where(p => db.Child.Where(c => c.ParentID == p.ParentID).Any(c => c.ParentID > 3)));
		}

		[YdbMemberNotFound]
		[Test]
		public void Any2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p =>    Child.Where(c => c.ParentID == p.ParentID).Any()),
					db.Parent.Where(p => db.Child.Where(c => c.ParentID == p.ParentID).Any()));
		}

		[YdbMemberNotFound]
		[Test]
		public void Any3([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.Any(c => c.ParentID > 3)),
					db.Parent.Where(p => p.Children.Any(c => c.ParentID > 3)));
		}

		[YdbMemberNotFound]
		[Test]
		public void Any31([DataSources(TestProvName.AllClickHouse)] string context)
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

		[YdbMemberNotFound]
		[Test]
		public void Any32([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.ParentID > 0 && SelectAny(p)),
					db.Parent.Where(p => p.ParentID > 0 && SelectAny(p)));
		}

		[YdbMemberNotFound]
		[Test]
		public void Any4([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.Any()),
					db.Parent.Where(p => p.Children.Any()));
		}

		[YdbMemberNotFound]
		[Test]
		public void Any5([DataSources(TestProvName.AllClickHouse)] string context)
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
				Assert.That(
					db.Child.Any(c => c.ParentID > 3), Is.EqualTo(Child.Any(c => c.ParentID > 3)));
		}

		[Test]
		public void Any7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(db.Child.Any(), Is.EqualTo(Child.Any()));
		}

		[YdbMemberNotFound]
		[Test]
		public void Any8([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Select(c => c.Parent).Any(c => c == p),
					from p in db.Parent select db.Child.Select(c => c.Parent).Any(c => c == p));
		}

		[YdbMemberNotFound]
		[Test]
		public void Any9([DataSources(TestProvName.AllClickHouse)] string context)
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

		[YdbMemberNotFound]
		[Test]
		public void Any10([DataSources(TestProvName.AllClickHouse)] string context)
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

		[YdbMemberNotFound]
		[Test]
		public void Any11([DataSources(TestProvName.AllClickHouse)] string context)
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

		[YdbMemberNotFound]
		[Test]
		public void Any12([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in             Parent    where             Child.   Any(c => p.ParentID == c.ParentID && c.ChildID > 3) select p,
					from p in db.GetTable<Parent>() where db.GetTable<Child>().Any(c => p.ParentID == c.ParentID && c.ChildID > 3) select p);
		}

		[YdbMemberNotFound]
		[Test]
		public void All1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p =>    Child.Where(c => c.ParentID == p.ParentID).All(c => c.ParentID > 3)),
					db.Parent.Where(p => db.Child.Where(c => c.ParentID == p.ParentID).All(c => c.ParentID > 3)));
		}

		[YdbMemberNotFound]
		[Test]
		public void All2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Children.All(c => c.ParentID > 3)),
					db.Parent.Where(p => p.Children.All(c => c.ParentID > 3)));
		}

		[YdbMemberNotFound]
		[Test]
		public void All3([DataSources(TestProvName.AllClickHouse)] string context)
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
				Assert.That(
					db.Child.All(c => c.ParentID > 3), Is.EqualTo(Child.All(c => c.ParentID > 3)));
		}

		[Test]
		public async Task All4Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					await db.Child.AllAsync(c => c.ParentID > 3), Is.EqualTo(Child.All     (c => c.ParentID > 3)));
		}

		[Test]
		public void All5([DataSources] string context)
		{
			int n = 3;

			using (var db = GetDataContext(context))
				Assert.That(
					db.Child.All(c => c.ParentID > n), Is.EqualTo(Child.All(c => c.ParentID > n)));
		}

		[Test]
		[YdbMemberNotFound]
		public void SubQueryAllAny([DataSources(TestProvName.AllClickHouse)] string context)
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

		[Test]
		public void StackOverflowRegressionTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person
					.Select(_ => _.Patient)
					.Any().ShouldBeTrue();
			}
		}

		sealed record Filter(string[]? NamesProp);

		[YdbMemberNotFound]
		// Access: unsupported syntax for enumerable subquery
		// ClickHouse: EXISTS with correlated scalar subquery used, we should generate IN instead
		[ActiveIssue(Configurations = new[] { TestProvName.AllAccess, TestProvName.AllClickHouse })]
		[Test]
		public void TestIssue4261([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var filter = new Filter(new[] { "John", "Not John" });

			var res = db.Person.Where(x => filter.NamesProp!.Any(y => y == x.FirstName)).ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			Assert.That(res[0].ID, Is.EqualTo(1));
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2156")]
		public void Issue2156Test([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query = from i in db.Person
						join t in (db.Person.Where(x => x.FirstName != "Nameless One")) on i.ID equals t.ID into tj
						from t in tj.DefaultIfEmpty()
						join tg in db.Person on t.ID equals tg.ID into tgj
						from tg in tgj.DefaultIfEmpty()
						join u in db.Person on i.ID equals u.ID into uj
						from u in uj.DefaultIfEmpty()
						join p in db.Person on i.ID equals p.ID
						join iSs in db.Person on i.ID equals iSs.ID
						join e in db.Person on u.ID equals e.ID into ej
						from e in ej.Take(1).DefaultIfEmpty()
						where i.Patient!.Diagnosis != "Immortality"
						select new { Issue = i, User = u, Project = p, Status = iSs, Email = e, IsHoliday = tgj.Any(x => x.FirstName == "John") };

			var grouped = query
				.Distinct()
				.OrderBy(x => x.User)
				.ToList()
				.GroupBy(x => (x.User, x.Email))
				.ToList();
		}
	}
}
