using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class SelectScalar : TestBase
	{
		[Test]
		public void Parameter1([DataContexts] string context)
		{
			var p = 1;
			using (var db = GetDataContext(context))
				Assert.AreEqual(p, db.Select(() => p));
		}

		[Test]
		public void Parameter2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p = 1;
				Assert.AreEqual(p, db.Select(() => new { p }).p);
			}
		}

		[Test]
		public void Constant1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, db.Select(() => 1));
		}

		[Test]
		public void Constant2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, db.Select(() => new { p = 1 }).p);
		}

		[Test]
		public void Constant3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, db.Select(() => new Person { ID = 1, FirstName = "John" }).ID);
		}

		[Test]
		public void StrLen([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual("1".Length, db.Select(() => "1".Length));
		}

		[Test]
		public void IntMaxValue([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(int.MaxValue, db.Select(() => int.MaxValue));
		}

		[Test]
		public void Substring([DataContexts] string context)
		{
			const string s = "123";
			using (var db = GetDataContext(context))
				Assert.AreEqual(s.Substring(1), db.Select(() => s.Substring(1)));
		}

		[Test]
		public void Add([DataContexts] string context)
		{
			const string s = "123";
			using (var db = GetDataContext(context))
				Assert.AreEqual(s.Substring(1).Length + 3, db.Select(() => s.Substring(1).Length + 3));
		}

		[Test]
		public void Scalar1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = (from p in db.Person select new { p } into p1 select p1.p).ToList().Where(p => p.ID == 1).First();
				Assert.AreEqual(1, q.ID);
			}
		}

		[Test]
		public void Scalar11([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var n = (from p in db.Person select p.ID).ToList().Where(id => id == 1).First();
				Assert.AreEqual(1, n);
			}
		}

		[Test]
		public void Scalar2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = (from p in db.Person select new { p }).ToList().Where(p => p.p.ID == 1).First();
				Assert.AreEqual(1, q.p.ID);
			}
		}

		[Test]
		public void Scalar21([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var n = (from p in db.Person select p.FirstName.Length).ToList().Where(len => len == 4).First();
				Assert.AreEqual(4, n);
			}
		}

		[Test]
		public void Scalar22([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Person
					select new { p1 = p, p2 = p }
					into p1
						where p1.p1.ID == 1 && p1.p2.ID == 1
						select p1;

				var result =
					from p in db.Person
					select new { p1 = p, p2 = p }
					into p1
						where p1.p1.ID == 1 && p1.p2.ID == 1
						select p1;

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void Scalar23([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Person
					select p.ID
					into p1
						where p1 == 1
						select new { p1 };

				var result =
					from p in db.Person
					select p.ID
					into p1
						where p1 == 1
						select new { p1 };

				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void Scalar3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in    Person where p.ID == 1 select 1;
				var result   = from p in db.Person where p.ID == 1 select 1;
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void Scalar31([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var n = 1;
				var expected = from p in    Person where p.ID == 1 select n;
				var result   = from p in db.Person where p.ID == 1 select n;
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void Scalar4([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from p in Parent
						join c in Child on p.ParentID equals c.ParentID
					where c.ChildID > 20
					select p;

				var result =
					from p in db.Parent
						join c in db.Child on p.ParentID equals c.ParentID
					where c.ChildID > 20
					select p;

				Assert.AreEqual(expected.Where(p => p.ParentID == 3).First(), result.Where(p => p.ParentID == 3).First());
			}
		}

		[Test]
		public void Function([DataContexts] string context)
		{
			var text = "123";

			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Select(c => string.Format("{0},{1}", c.ChildID, text)).FirstOrDefault(),
					db.Child.Select(c => string.Format("{0},{1}", c.ChildID, text)).FirstOrDefault());
		}
	}
}
