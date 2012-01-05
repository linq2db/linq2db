using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ElementOperationTest : TestBase
	{
		[Test]
		public void First([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderByDescending(p => p.ParentID).First().ParentID,
					db.Parent.OrderByDescending(p => p.ParentID).First().ParentID);
		}

		[Test]
		public void FirstWhere([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, db.Parent.First(p => p.ParentID == 2).ParentID);
		}

		[Test]
		public void FirstOrDefault([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.IsNull((from p in db.Parent where p.ParentID == 100 select p).FirstOrDefault());
		}

		[Test]
		public void FirstOrDefaultWhere([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, db.Parent.FirstOrDefault(p => p.ParentID == 2).ParentID);
		}

		[Test]
		public void Single([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, db.Parent.Where(p => p.ParentID == 1).Single().ParentID);
		}

		[Test]
		public void SingleWhere([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, db.Parent.Single(p => p.ParentID == 2).ParentID);
		}

		[Test]
		public void SingleOrDefault([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.IsNull((from p in db.Parent where p.ParentID == 100 select p).SingleOrDefault());
		}

		[Test]
		public void SingleOrDefaultWhere([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, db.Parent.SingleOrDefault(p => p.ParentID == 2).ParentID);
		}

		[Test]
		public void FirstOrDefaultScalar([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.ParentID).FirstOrDefault().ParentID,
					db.Parent.OrderBy(p => p.ParentID).FirstOrDefault().ParentID);
		}

		[Test]
		public void NestedFirstOrDefaultScalar1([DataContexts(ProviderName.Informix, ProviderName.Sybase)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.FirstOrDefault().ChildID,
					from p in db.Parent select db.Child.FirstOrDefault().ChildID);
		}

		[Test]
		public void NestedFirstOrDefaultScalar2([DataContexts(ProviderName.Informix, ProviderName.Oracle, ProviderName.Sybase)] string context)
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
								.FirstOrDefault()
								.ChildID
					},
					from p in db.Parent
					select new
					{
						p.ParentID,
						MaxChild = db.Child
							.Where(c => c.Parent == p)
							.OrderByDescending(c => c.ChildID * c.ParentID)
							.FirstOrDefault()
							.ChildID
					});
		}

		[Test]
		public void NestedFirstOrDefault1([DataContexts] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.FirstOrDefault(),
					from p in db.Parent select db.Child.FirstOrDefault());

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void NestedFirstOrDefault2([DataContexts] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.FirstOrDefault(),
					from p in db.Parent select p.Children.FirstOrDefault());

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void NestedFirstOrDefault3([DataContexts(ProviderName.Informix, ProviderName.Firebird)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.Select(c => c.ParentID).Distinct().FirstOrDefault(),
					from p in db.Parent select p.Children.Select(c => c.ParentID).Distinct().FirstOrDefault());
		}

		[Test]
		public void NestedFirstOrDefault4([DataContexts(ProviderName.Informix, ProviderName.Firebird, ProviderName.PostgreSQL)] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.Where(c => c.ParentID > 0).Distinct().FirstOrDefault(),
					from p in db.Parent select p.Children.Where(c => c.ParentID > 0).Distinct().FirstOrDefault());

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void NestedFirstOrDefault5([DataContexts] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    GrandChild select p.Child.Parent.Children.FirstOrDefault(),
					from p in db.GrandChild select p.Child.Parent.Children.FirstOrDefault());

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void NestedSingleOrDefault1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.Select(c => c.ParentID).Distinct().SingleOrDefault(),
					from p in db.Parent select p.Children.Select(c => c.ParentID).Distinct().SingleOrDefault());
		}

		[Test]
		public void FirstOrDefaultEntitySet()
		{
			using (var db = new NorthwindDB())
			{
				AreEqual(
					   Customer.Select(c => c.Orders.FirstOrDefault()),
					db.Customer.Select(c => c.Orders.FirstOrDefault()));
			}
		}

		[Test]
		public void NestedSingleOrDefaultTest()
		{
			using (var db = new NorthwindDB())
			{
				AreEqual(
					   Customer.Select(c => c.Orders.Take(1).SingleOrDefault()),
					db.Customer.Select(c => c.Orders.Take(1).SingleOrDefault()));
			}
		}
	}
}
