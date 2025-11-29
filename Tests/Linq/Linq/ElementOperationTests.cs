using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ElementOperationTests : TestBase
	{
		[Test]
		public void First([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Parent.OrderByDescending(p => p.ParentID).First().ParentID, Is.EqualTo(Parent.OrderByDescending(p => p.ParentID).First().ParentID));
		}

		[Test]
		public void FirstWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(db.Parent.First(p => p.ParentID == 2).ParentID, Is.EqualTo(2));
		}

		[Test]
		public void FirstOrDefault([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Parent where p.ParentID == 100 select p).FirstOrDefault(), Is.Null);
		}

		[Test]
		public void FirstOrDefaultWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(db.Parent.FirstOrDefault(p => p.ParentID == 2)!.ParentID, Is.EqualTo(2));
		}

		[Test]
		public void Single([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(db.Parent.Where(p => p.ParentID == 1).Single().ParentID, Is.EqualTo(1));
		}

		[Test]
		public void SingleWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(db.Parent.Single(p => p.ParentID == 2).ParentID, Is.EqualTo(2));
		}

		[Test]
		public void SingleOrDefault([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Parent where p.ParentID == 100 select p).SingleOrDefault(), Is.Null);
		}

		[Test]
		public void SingleOrDefaultWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(db.Parent.SingleOrDefault(p => p.ParentID == 2)!.ParentID, Is.EqualTo(2));
		}

		[Test]
		public void FirstOrDefaultScalar([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Parent.OrderBy(p => p.ParentID).FirstOrDefault()!.ParentID, Is.EqualTo(Parent.OrderBy(p => p.ParentID).FirstOrDefault()!.ParentID));
		}

		[Test]
		public void NestedFirstOrDefaultScalar1([DataSources(
			TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.FirstOrDefault()!.ChildID,
					from p in db.Parent select db.Child.FirstOrDefault()!.ChildID);
		}

		[Test]
		public void NestedFirstOrDefaultScalar2([DataSources(
			TestProvName.AllAccess,
			TestProvName.AllInformix,
			TestProvName.AllOracle,
			TestProvName.AllClickHouse,
			TestProvName.AllSybase)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new
					{
						p.ParentID,
						MaxChild =
							Child
								.Where(c => c.Parent == p)
								.OrderByDescending(c => c.ChildID * c.ParentID)
								.FirstOrDefault() == null ?
							0 :
							Child
								.Where(c => c.Parent == p)
								.OrderByDescending(c => c.ChildID * c.ParentID)
								.FirstOrDefault()!
								.ChildID
					},
					from p in db.Parent
					select new
					{
						p.ParentID,
						MaxChild = db.Child
							.Where(c => c.Parent == p)
							.OrderByDescending(c => c.ChildID * c.ParentID)
							.FirstOrDefault()!
							.ChildID
					});
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void NestedFirstOrDefault1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.FirstOrDefault(),
					from p in db.Parent select db.Child.FirstOrDefault());
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void NestedFirstOrDefault2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.OrderBy(c => c.ChildID).FirstOrDefault(),
					from p in db.Parent select p.Children.OrderBy(c => c.ChildID).FirstOrDefault());
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void NestedFirstOrDefault3([DataSources(TestProvName.AllInformix, TestProvName.AllOracle, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.Select(c => c.ParentID).Distinct().FirstOrDefault(),
					from p in db.Parent select p.Children.Select(c => c.ParentID).Distinct().FirstOrDefault());
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, TestProvName.AllMySql57, TestProvName.AllSybase, TestProvName.AllOracle11, TestProvName.AllMariaDB, TestProvName.AllDB2, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void NestedFirstOrDefault4([DataSources(TestProvName.AllInformix, TestProvName.AllPostgreSQL9)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.Where(c => c.ParentID > 0).Distinct().OrderBy(_ => _.ChildID).FirstOrDefault(),
					from p in db.Parent select p.Children.Where(c => c.ParentID > 0).Distinct().OrderBy(_ => _.ChildID).FirstOrDefault());
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void NestedFirstOrDefault5([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AreEqual(
				from p in GrandChild
				where p.ChildID > 0
				select p.Child!.Parent!.Children.OrderBy(c => c.ChildID).FirstOrDefault(),
				from p in db.GrandChild
				where p.ChildID > 0
				select p.Child!.Parent!.Children.OrderBy(c => c.ChildID).FirstOrDefault());
		}

		[YdbTableNotFound]
		[Test]
		public void NestedSingleOrDefault1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.Select(c => c.ParentID).Distinct().SingleOrDefault(),
					from p in db.Parent select p.Children.Select(c => c.ParentID).Distinct().SingleOrDefault());
		}

		[Test]
		public void FirstOrDefaultEntitySet([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.Customer.Select(c => c.Orders.FirstOrDefault()),
					db.Customer.Select(c => c.Orders.FirstOrDefault()));
			}
		}

		[Test]
		public void NestedSingleOrDefaultTest([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					dd.Customer.Select(c => c.Orders.Take(1).SingleOrDefault()),
					db.Customer.Select(c => c.Orders.Take(1).SingleOrDefault()));
			}
		}

		[Test]
		public void MultipleQuery([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var q =
					from p in db.Product
					select db.Category.Select(zrp => zrp.CategoryName).FirstOrDefault();

				var _ = q.ToList();
			}
		}
	}
}
