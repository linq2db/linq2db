using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class SelectScalarTests : TestBase
	{
		[Test]
		public void Parameter1([DataSources] string context)
		{
			var p = 1;
			using (var db = GetDataContext(context))
				Assert.AreEqual(p, db.Select(() => p));
		}

		[Test]
		public async Task Parameter1Async([DataSources] string context)
		{
			var p = 1;
			using (var db = GetDataContext(context))
				Assert.AreEqual(p, await db.SelectAsync(() => p));
		}

		[Test]
		public void Parameter2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p = 1;
				Assert.AreEqual(p, db.Select(() => new { p }).p);
			}
		}

		[Test]
		public void Constant1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, db.Select(() => 1));
		}

		[Test]
		public void Constant2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, db.Select(() => new { p = 1 }).p);
		}

		[Test]
		public void Constant3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, db.Select(() => new Person { ID = 1, FirstName = "John" }).ID);
		}

		[Test]
		public void StrLen([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual("1".Length, db.Select(() => "1".Length));
		}

		[Test]
		public void IntMaxValue([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(int.MaxValue, db.Select(() => int.MaxValue));
		}

		[Test]
		public void Substring([DataSources] string context)
		{
			const string s = "123";
			using (var db = GetDataContext(context))
				Assert.AreEqual(s.Substring(1), db.Select(() => s.Substring(1)));
		}

		[Test]
		public void Add([DataSources] string context)
		{
			const string s = "123";
			using (var db = GetDataContext(context))
				Assert.AreEqual(s.Substring(1).Length + 3, db.Select(() => s.Substring(1).Length + 3));
		}

		[Test]
		public void Scalar1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = (from p in db.Person select new { p } into p1 select p1.p).ToList().Where(p => p.ID == 1).First();
				Assert.AreEqual(1, q.ID);
			}
		}

		[Test]
		public void Scalar11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var n = (from p in db.Person select p.ID).ToList().Where(id => id == 1).First();
				Assert.AreEqual(1, n);
			}
		}

		[Test]
		public void Scalar2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = (from p in db.Person select new { p }).ToList().Where(p => p.p.ID == 1).First();
				Assert.AreEqual(1, q.p.ID);
			}
		}

		[Test]
		public void Scalar21([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var n = (from p in db.Person select p.FirstName.Length).ToList().Where(len => len == 4).First();
				Assert.AreEqual(4, n);
			}
		}

		[Test]
		public void Scalar22([DataSources] string context)
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
		public void Scalar23([DataSources] string context)
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
		public void Scalar3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in    Person where p.ID == 1 select 1;
				var result   = from p in db.Person where p.ID == 1 select 1;
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test]
		public void Scalar31([DataSources] string context)
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
		public void Scalar4([DataSources] string context)
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
		public void Function([DataSources] string context)
		{
			var text = "123";

			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Select(c => string.Format("{0},{1}", c.ChildID, text)).FirstOrDefault(),
					db.Child.Select(c => string.Format("{0},{1}", c.ChildID, text)).FirstOrDefault());
		}

		[Test]
		public void SubQueryTest([DataSources(
			ProviderName.Access, ProviderName.Informix, ProviderName.SqlCe,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SapHana)]
			string context)
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
