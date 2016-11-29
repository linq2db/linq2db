using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class SelectScalarTests : TestBase
	{
		[Test, DataContextSource]
		public void Parameter1(string context)
		{
			var p = 1;
			using (var db = GetDataContext(context))
				Assert.AreEqual(p, db.Select(() => p));
		}

		[Test, DataContextSource]
		public void Parameter2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var p = 1;
				Assert.AreEqual(p, db.Select(() => new { p }).p);
			}
		}

		[Test, DataContextSource]
		public void Constant1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, db.Select(() => 1));
		}

		[Test, DataContextSource]
		public void Constant2(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, db.Select(() => new { p = 1 }).p);
		}

		[Test, DataContextSource]
		public void Constant3(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, db.Select(() => new Person { ID = 1, FirstName = "John" }).ID);
		}

		[Test, DataContextSource]
		public void StrLen(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual("1".Length, db.Select(() => "1".Length));
		}

		[Test, DataContextSource]
		public void IntMaxValue(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(int.MaxValue, db.Select(() => int.MaxValue));
		}

		[Test, DataContextSource]
		public void Substring(string context)
		{
			const string s = "123";
			using (var db = GetDataContext(context))
				Assert.AreEqual(s.Substring(1), db.Select(() => s.Substring(1)));
		}

		[Test, DataContextSource]
		public void Add(string context)
		{
			const string s = "123";
			using (var db = GetDataContext(context))
				Assert.AreEqual(s.Substring(1).Length + 3, db.Select(() => s.Substring(1).Length + 3));
		}

		[Test, DataContextSource]
		public void Scalar1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = (from p in db.Person select new { p } into p1 select p1.p).ToList().Where(p => p.ID == 1).First();
				Assert.AreEqual(1, q.ID);
			}
		}

		[Test, DataContextSource]
		public void Scalar11(string context)
		{
			using (var db = GetDataContext(context))
			{
				var n = (from p in db.Person select p.ID).ToList().Where(id => id == 1).First();
				Assert.AreEqual(1, n);
			}
		}

		[Test, DataContextSource]
		public void Scalar2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = (from p in db.Person select new { p }).ToList().Where(p => p.p.ID == 1).First();
				Assert.AreEqual(1, q.p.ID);
			}
		}

		[Test, DataContextSource]
		public void Scalar21(string context)
		{
			using (var db = GetDataContext(context))
			{
				var n = (from p in db.Person select p.FirstName.Length).ToList().Where(len => len == 4).First();
				Assert.AreEqual(4, n);
			}
		}

		[Test, DataContextSource]
		public void Scalar22(string context)
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

		[Test, DataContextSource]
		public void Scalar23(string context)
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

		[Test, DataContextSource]
		public void Scalar3(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in    Person where p.ID == 1 select 1;
				var result   = from p in db.Person where p.ID == 1 select 1;
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void Scalar31(string context)
		{
			using (var db = GetDataContext(context))
			{
				var n = 1;
				var expected = from p in    Person where p.ID == 1 select n;
				var result   = from p in db.Person where p.ID == 1 select n;
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void Scalar4(string context)
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

		[Test, DataContextSource]
		public void Function(string context)
		{
			var text = "123";

			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Select(c => string.Format("{0},{1}", c.ChildID, text)).FirstOrDefault(),
					db.Child.Select(c => string.Format("{0},{1}", c.ChildID, text)).FirstOrDefault());
		}

		[Test, DataContextSource(ProviderName.Access, ProviderName.Informix, ProviderName.SqlCe, ProviderName.Sybase, ProviderName.SapHana)]
		public void SubQueryTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Select(() => new
				{
					f1 = db.Parent.Select(p => p.Value1).FirstOrDefault()
				});
			}
		}
	}
}
