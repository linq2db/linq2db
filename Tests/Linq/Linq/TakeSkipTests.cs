using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Linq;
	using Model;

	[TestFixture]
	public class TakeSkipTests : TestBase
	{
		[Test, DataContextSource]
		public void Take1(string context)
		{
			using (var db = GetDataContext(context))
			{
				for (var i = 2; i <= 3; i++)
					Assert.AreEqual(i, (from ch in db.Child select ch).Take(i).ToList().Count);
			}
		}

		static void TakeParam(ITestDataContext db, int n)
		{
			Assert.AreEqual(n, (from ch in db.Child select ch).Take(n).ToList().Count);
		}

		[Test, DataContextSource]
		public void Take2(string context)
		{
			using (var db = GetDataContext(context))
				TakeParam(db, 1);
		}

		[Test, DataContextSource]
		public void Take3(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(3, (from ch in db.Child where ch.ChildID > 3 || ch.ChildID < 4 select ch).Take(3).ToList().Count);
		}

		[Test, DataContextSource]
		public void Take4(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(3, (from ch in db.Child where ch.ChildID >= 0 && ch.ChildID <= 100 select ch).Take(3).ToList().Count);
		}

		[Test, DataContextSource]
		public void Take5(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(3, db.Child.Take(3).ToList().Count);
		}

		[Test, DataContextSource]
		public void Take6(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =    Child.OrderBy(c => c.ChildID).Take(3);
				var result   = db.Child.OrderBy(c => c.ChildID).Take(3);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void Take7(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(3, db.Child.Take(() => 3).ToList().Count);
		}

		[Test, DataContextSource]
		public void Take8(string context)
		{
			var n = 3;
			using (var db = GetDataContext(context))
				Assert.AreEqual(3, db.Child.Take(() => n).ToList().Count);
		}

		[Test, DataContextSource(ProviderName.Sybase)]
		public void TakeCount(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Take(5).Count(),
					db.Child.Take(5).Count());
		}

		[Test, DataContextSource]
		public void Skip1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(Child.Skip(3), db.Child.Skip(3));
		}

		[Test, DataContextSource]
		public void Skip2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in    Child where ch.ChildID > 3 || ch.ChildID < 4 select ch).Skip(3),
					(from ch in db.Child where ch.ChildID > 3 || ch.ChildID < 4 select ch).Skip(3));
		}

		[Test, DataContextSource]
		public void Skip3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in    Child where ch.ChildID >= 0 && ch.ChildID <= 100 select ch).Skip(3),
					(from ch in db.Child where ch.ChildID >= 0 && ch.ChildID <= 100 select ch).Skip(3));
		}

		[Test, DataContextSource]
		public void Skip4(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Child.OrderByDescending(c => c.ChildID).Skip(3);
				var result   = db.Child.OrderByDescending(c => c.ChildID).Skip(3);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void Skip5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.OrderByDescending(c => c.ChildID).ThenBy(c => c.ParentID + 1).Skip(3),
					db.Child.OrderByDescending(c => c.ChildID).ThenBy(c => c.ParentID + 1).Skip(3));
		}

		[Test, DataContextSource]
		public void Skip6(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(Child.Skip(3), db.Child.Skip(() => 3));
		}

		[Test, DataContextSource]
		public void Skip7(string context)
		{
			var n = 3;
			using (var db = GetDataContext(context))
				AreEqual(Child.Skip(n), db.Child.Skip(() => n));
		}

		[Test, DataContextSource(ProviderName.SqlServer2000, ProviderName.Sybase, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.Access)]
		public void SkipCount(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Skip(2).Count(),
					db.Child.Skip(2).Count());
		}

		[Test, DataContextSource]
		public void SkipTake1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =    Child.OrderByDescending(c => c.ChildID).Skip(2).Take(5);
				var result   = db.Child.OrderByDescending(c => c.ChildID).Skip(2).Take(5);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SkipTake2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =    Child.OrderByDescending(c => c.ChildID).Take(7).Skip(2);
				var result   = db.Child.OrderByDescending(c => c.ChildID).Take(7).Skip(2);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SkipTake3(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Child.OrderBy(c => c.ChildID).Skip(1).Take(7).Skip(2);
				var result   = db.Child.OrderBy(c => c.ChildID).Skip(1).Take(7).Skip(2);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test, DataContextSource(ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.SqlServer2000, ProviderName.Sybase, ProviderName.Access)]
		public void SkipTake4(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected =    Child.OrderByDescending(c => c.ChildID).Skip(1).Take(7).OrderBy(c => c.ChildID).Skip(2);
				var result   = db.Child.OrderByDescending(c => c.ChildID).Skip(1).Take(7).OrderBy(c => c.ChildID).Skip(2);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		[Test, DataContextSource]
		public void SkipTake5(string context)
		{
			using (var db = GetDataContext(context))
			{
				var list = db.Child.Skip(2).Take(5).ToList();
				Assert.AreEqual(5, list.Count);
			}
		}

		void SkipTake6(ITestDataContext db, bool doSkip)
		{
			var q1 = from g in db.GrandChild select g;

			if (doSkip)
				q1 = q1.Skip(12);
			q1 = q1.Take(3);

			var q2 =
				from c in db.Child
				from p in q1
				where c.ParentID == p.ParentID
				select c;

			var q3 = from g in GrandChild select g;

			if (doSkip)
				q3 = q3.Skip(12);
			q3 = q3.Take(3);

			var q4 =
				from c in Child
				from p in q3
				where c.ParentID == p.ParentID
				select c;

			AreEqual(q4, q2);
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.Sybase, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.Access)]
		public void SkipTake6(string context)
		{
			using (var db = GetDataContext(context))
			{
				SkipTake6(db, false);
				SkipTake6(db, true);
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.SqlServer2000, ProviderName.Sybase, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.Access)]
		public void SkipTakeCount(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Skip(2).Take(5).Count(),
					db.Child.Skip(2).Take(5).Count());
		}

		[Test, DataContextSource]
		public void SkipFirst(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = (from p in Parent where p.ParentID > 1 select p).Skip(1).First();
				var result = from p in db.GetTable<Parent>() select p;
				result = from p in result where p.ParentID > 1 select p;
				var b = result.Skip(1).First();

				Assert.AreEqual(expected, b);
			}
		}

		[Test, DataContextSource]
		public void ElementAt1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from p in    Parent where p.ParentID > 1 select p).ElementAt(3),
					(from p in db.Parent where p.ParentID > 1 select p).ElementAt(3));
		}

		[Test, DataContextSource]
		public void ElementAt2(string context)
		{
			var n = 3;
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from p in    Parent where p.ParentID > 1 select p).ElementAt(n),
					(from p in db.Parent where p.ParentID > 1 select p).ElementAt(() => n));
		}

		[Test, DataContextSource]
		public async Task ElementAt2Async(string context)
		{
			var n = 3;
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					      (from p in    Parent where p.ParentID > 1 select p).ElementAt(n),
					await (from p in db.Parent where p.ParentID > 1 select p).ElementAtAsync(() => n));
		}

		[Test, DataContextSource]
		public void ElementAtDefault1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from p in    Parent where p.ParentID > 1 select p).ElementAtOrDefault(3),
					(from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefault(3));
		}

		[Test, DataContextSource]
		public void ElementAtDefault2(string context)
		{
			using (var db = GetDataContext(context))
				Assert.IsNull((from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefault(300000));
		}

		[Test, DataContextSource]
		public void ElementAtDefault3(string context)
		{
			var n = 3;
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from p in    Parent where p.ParentID > 1 select p).ElementAtOrDefault(n),
					(from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefault(() => n));
		}

		[Test, DataContextSource]
		public async Task ElementAtDefault3Async(string context)
		{
			var n = 3;
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					      (from p in    Parent where p.ParentID > 1 select p).ElementAtOrDefault(n),
					await (from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefaultAsync(() => n));
		}

		[Test, DataContextSource]
		public void ElementAtDefault4(string context)
		{
			var n = 300000;
			using (var db = GetDataContext(context))
				Assert.IsNull((from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefault(() => n));
		}

		[Test, DataContextSource]
		public void ElementAtDefault5(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					Person.   OrderBy(p => p.LastName).ElementAtOrDefault(3),
					db.Person.OrderBy(p => p.LastName).ElementAtOrDefault(3));
		}

		[Test, IncludeDataContextSource(ProviderName.Access, ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void TakeWithPercent(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Person.Take(50, TakeHints.Percent).Select(_ => _);

				Assert.IsNotEmpty(q);

				var qry = q.ToString();
				Assert.That(qry.Contains("PERCENT"));
			}

		}

		[Test, IncludeDataContextSource(ProviderName.Access, ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void TakeWithPercent1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Person.Take(() => 50, TakeHints.Percent).Select(_ => _);

				Assert.IsNotEmpty(q);

				var qry = q.ToString();
				Assert.That(qry.Contains("PERCENT"));
			}

		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void TakeWithTies(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Person.OrderBy(_ => _.FirstName).Take(50, TakeHints.WithTies | TakeHints.Percent).Select(_ => _);

				Assert.IsNotEmpty(q);

				var qry = q.ToString();
				Assert.That(qry.Contains("PERCENT"));
				Assert.That(qry.Contains("WITH"));
			}

		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void TakeWithTies2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Person.OrderBy(_ => _.FirstName).Take(() => 50, TakeHints.WithTies | TakeHints.Percent).Select(_ => _);

				Assert.IsNotEmpty(q);

				var qry = q.ToString();
				Assert.That(qry.Contains("PERCENT"));
				Assert.That(qry.Contains("WITH"));
			}

		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void SkipTakeWithTies(string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.Throws<LinqException>(() => db.Person.Skip(1).Take(() => 50, TakeHints.WithTies | TakeHints.Percent).Select(_ => _).ToList());

				Assert.Throws<LinqException>(() => db.Person.Take(() => 50, TakeHints.WithTies | TakeHints.Percent).Skip(1).Select(_ => _).ToList());
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SQLite, ProviderName.SqlCe, TestProvName.SQLiteMs)]
		public void TakeWithHintsFails(string context)
		{
			using (var db = GetDataContext(context))
				Assert.Throws<LinqException>(() => db.Parent.Take(10, TakeHints.Percent).ToList());
		}

		[Test, DataContextSource(ProviderName.Sybase)]
		public void TakeSkipJoin(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 =    Types.Concat(   Types).Take(15);
				var q2 = db.Types.Concat(db.Types).Take(15);

				AreEqual(
					from e in q1
					from p in q1.Where(_ => _.ID == e.ID).DefaultIfEmpty()
					select new {e.ID, p.SmallIntValue},
					from e in q2
					from p in q2.Where(_ => _.ID == e.ID).DefaultIfEmpty()
					select new { e.ID, p.SmallIntValue }
					);
			}
		}
	}
}
