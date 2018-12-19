using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class OrderByTests : TestBase
	{
		[Test]
		public void OrderBy1([DataSources] string context)
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
		public void OrderBy2([DataSources] string context)
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
		public void OrderBy3([DataSources] string context)
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
		public void OrderBy4([DataSources] string context)
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
		public void OrderBy5([DataSources] string context)
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
		public void ConditionOrderBy([DataSources] string context)
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
		public void OrderBy6([DataSources(false)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				var q =
					from person in db.Person
					join patient in db.Patient on person.ID equals patient.PersonID into g
					from patient in g.DefaultIfEmpty()
					orderby person.MiddleName // if comment this line then "Diagnosis" is not selected.
					select new { person.ID, PatientID = patient != null ? (int?)patient.PersonID : null };

				q.ToList();

				Assert.IsFalse(db.LastQuery.Contains("Diagnosis"), "Why do we select Patient.Diagnosis??");
			}
		}

		[Test]
		public void OrderBy7([DataSources] string context)
		{
			try
			{
				LinqToDB.Common.Configuration.Linq.DoNotClearOrderBys = true;

				using (var db = GetDataContext(context))
				{

					var expected =
						from ch in Child
						orderby ch.ChildID%2, ch.ChildID
						select ch;

					var qry =
						from ch in db.Child
						orderby ch.ChildID%2
						select new {ch};

					var result = qry.OrderBy(x => x.ch.ChildID).Select(x => x.ch);

					AreEqual(expected, result);
				}
			}
			finally
			{
				LinqToDB.Common.Configuration.Linq.DoNotClearOrderBys = false;
			}
		}

		[Test]
		public void OrderBy8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var expected =
					from ch in Child
					orderby ch.ChildID%2, ch.ChildID
					select ch;

				var qry =
					from ch in db.Child
					orderby ch.ChildID%2
					select new {ch};

				var result = qry.ThenOrBy(x => x.ch.ChildID).Select(x => x.ch);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void OrderBy9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var expected =
					from ch in Child
					orderby ch.ChildID%2, ch.ChildID descending
					select ch;

				var qry =
					from ch in db.Child
					orderby ch.ChildID%2 descending
					select new {ch};

				var result = qry.ThenOrByDescending(x => x.ch.ChildID).Select(x => x.ch);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void OrderBy10([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var expected =
					(from ch in Child
					orderby ch.ChildID%2
					select ch).ThenByDescending(ch => ch.ChildID);

				var qry =
					from ch in db.Child
					orderby ch.ChildID%2
					select new {ch};

				var result = qry.ThenOrByDescending(x => x.ch.ChildID).Select(x => x.ch);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void OrderBy11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var expected =
					(from ch in Child
					orderby ch.ChildID%2
					select ch).ThenByDescending(ch => ch.ChildID);

				var qry =
					from ch in db.Child
					orderby ch.ChildID%2
					select ch;

				var result = qry.ThenOrByDescending(x => x.ChildID).Select(x => x);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void OrderBy12([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{

				var expected =
					from ch in Child
					orderby ch.ChildID%2 descending
					select ch;

				var qry =
					from ch in db.Child
					select ch;

				var result = qry.ThenOrByDescending(x => x.ChildID%2);

				AreEqual(expected, result);
			}
		}

		[Test]
		public void OrderBySelf1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in    Parent orderby p select p;
				var result   = from p in db.Parent orderby p select p;
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBySelf2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in    Parent1 orderby p select p;
				var result   = from p in db.Parent1 orderby p select p;
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void OrderBySelectMany1([DataSources(ProviderName.Access)] string context)
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
		public void OrderBySelectMany2([DataSources] string context)
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
		public void OrderBySelectMany3([DataSources(ProviderName.Access)] string context)
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
		public void OrderAscDesc([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =    Parent.OrderBy(p => p.ParentID).OrderByDescending(p => p.ParentID);
				var result   = db.Parent.OrderBy(p => p.ParentID).OrderByDescending(p => p.ParentID);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void Count1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.ParentID).Count(),
					db.Parent.OrderBy(p => p.ParentID).Count());
		}

		[Test]
		public void Count2([DataSources(ProviderName.Sybase, ProviderName.SybaseManaged)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.ParentID).Take(3).Count(),
					db.Parent.OrderBy(p => p.ParentID).Take(3).Count());
		}

		[Test]
		public void Min1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.ParentID).Min(p => p.ParentID),
					db.Parent.OrderBy(p => p.ParentID).Min(p => p.ParentID));
		}

		[Test]
		public void Min2([DataSources(ProviderName.Sybase, ProviderName.SybaseManaged)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.ParentID).Take(3).Min(p => p.ParentID),
					db.Parent.OrderBy(p => p.ParentID).Take(3).Min(p => p.ParentID));
		}

		[Test]
		public void Min3([DataSources(
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix)]
			string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.OrderBy(p => p.Value1).Take(3).Min(p => p.ParentID),
					db.Parent.OrderBy(p => p.Value1).Take(3).Min(p => p.ParentID));
		}

		[Test]
		public void Distinct([DataSources] string context)
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
		public void Take([DataSources] string context)
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
