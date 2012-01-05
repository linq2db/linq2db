using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class OrderByTest : TestBase
	{
		[Test]
		public void OrderBy1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in Child
					orderby ch.ParentID descending, ch.ChildID ascending
					select ch;

				var result =
					from ch in db.Child
					orderby ch.ParentID descending, ch.ChildID ascending
					select ch;

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBy2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in Child
					orderby ch.ParentID descending, ch.ChildID ascending
					select ch;

				var result = 
					from ch in db.Child
					orderby ch.ParentID descending, ch.ChildID ascending
					select ch;

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBy3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in
						from ch in Child
						orderby ch.ParentID descending
						select ch
					orderby ch.ParentID descending , ch.ChildID
					select ch;

				var result =
					from ch in
						from ch in db.Child
						orderby ch.ParentID descending
						select ch
					orderby ch.ParentID descending , ch.ChildID
					select ch;

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBy4([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in
						from ch in Child
						orderby ch.ParentID descending
						select ch
					orderby ch.ParentID descending, ch.ChildID, ch.ParentID + 1 descending
					select ch;

				var result =
					from ch in
						from ch in db.Child
						orderby ch.ParentID descending
						select ch
					orderby ch.ParentID descending, ch.ChildID, ch.ParentID + 1 descending
					select ch;

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBy5([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in Child
					orderby ch.ChildID % 2, ch.ChildID
					select ch;

				var result =
					from ch in db.Child
					orderby ch.ChildID % 2, ch.ChildID
					select ch;

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void ConditionOrderBy([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from ch in Child
					orderby ch.ParentID > 0 && ch.ChildID != ch.ParentID descending, ch.ChildID
					select ch;

				var result =
					from ch in db.Child
					orderby ch.ParentID > 0 && ch.ChildID != ch.ParentID descending, ch.ChildID
					select ch;

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBySelf1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in    Parent orderby p select p;
				var result   = from p in db.Parent orderby p select p;
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBySelf2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in    Parent1 orderby p select p;
				var result   = from p in db.Parent1 orderby p select p;
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBySelectMany1([DataContexts(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent.OrderBy(p => p.ParentID)
					from c in Child. OrderBy(c => c.ChildID)
					where p == c.Parent
					select new { p.ParentID, c.ChildID };

				var result =
					from p in db.Parent.OrderBy(p => p.ParentID)
					from c in db.Child. OrderBy(c => c.ChildID)
					where p == c.Parent
					select new { p.ParentID, c.ChildID };

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBySelectMany2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent1.OrderBy(p => p.ParentID)
					from c in Child.  OrderBy(c => c.ChildID)
					where p.ParentID == c.Parent1.ParentID
					select new { p.ParentID, c.ChildID };

				var result =
					from p in db.Parent1.OrderBy(p => p.ParentID)
					from c in db.Child.  OrderBy(c => c.ChildID)
					where p == c.Parent1
					select new { p.ParentID, c.ChildID };

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBySelectMany3([DataContexts(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent.OrderBy(p => p.ParentID)
					from c in Child. OrderBy(c => c.ChildID)
					where c.Parent == p
					select new { p.ParentID, c.ChildID };

				var result =
					from p in db.Parent.OrderBy(p => p.ParentID)
					from c in db.Child. OrderBy(c => c.ChildID)
					where c.Parent == p
					select new { p.ParentID, c.ChildID };

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderAscDesc([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Parent.OrderBy(p => p.ParentID).OrderByDescending(p => p.ParentID);
				var result   = db.Parent.OrderBy(p => p.ParentID).OrderByDescending(p => p.ParentID);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void Count1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.ParentID).Count(),
					db.Parent.OrderBy(p => p.ParentID).Count());
		}

		[Test]
		public void Count2([DataContexts(ProviderName.Sybase)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.ParentID).Take(3).Count(),
					db.Parent.OrderBy(p => p.ParentID).Take(3).Count());
		}

		[Test]
		public void Min1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.ParentID).Min(p => p.ParentID),
					db.Parent.OrderBy(p => p.ParentID).Min(p => p.ParentID));
		}

		[Test]
		public void Min2([DataContexts(ProviderName.Sybase)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.ParentID).Take(3).Min(p => p.ParentID),
					db.Parent.OrderBy(p => p.ParentID).Take(3).Min(p => p.ParentID));
		}

		[Test]
		public void Min3([DataContexts(ProviderName.Sybase, ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.Value1).Take(3).Min(p => p.ParentID),
					db.Parent.OrderBy(p => p.Value1).Take(3).Min(p => p.ParentID));
		}

		[Test]
		public void Distinct([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p in Parent
					join c in Child on p.ParentID equals c.ParentID
					join g in GrandChild on c.ChildID equals  g.ChildID
					select p).Distinct().OrderBy(p => p.ParentID)
					,
					(from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					join g in db.GrandChild on c.ChildID equals  g.ChildID
					select p).Distinct().OrderBy(p => p.ParentID));
		}

		[Test]
		public void Take([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					(from p in db.Parent
					 join c in db.Child on p.ParentID equals c.ParentID
					 join g in db.GrandChild on c.ChildID equals g.ChildID
					 select p).Take(3).OrderBy(p => p.ParentID);

				Assert.AreEqual(3, q.AsEnumerable().Count());
			}
		}
	}
}
