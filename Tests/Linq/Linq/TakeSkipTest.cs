using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class TakeSkipTest : TestBase
	{
		[Test]
		public void Take1()
		{
			ForEachProvider(db =>
			{
				for (var i = 2; i <= 3; i++)
					Assert.AreEqual(i, (from ch in db.Child select ch).Take(i).ToList().Count);
			});
		}

		static void TakeParam(ITestDataContext db, int n)
		{
			Assert.AreEqual(n, (from ch in db.Child select ch).Take(n).ToList().Count);
		}

		[Test]
		public void Take2()
		{
			ForEachProvider(db => TakeParam(db, 1));
		}

		[Test]
		public void Take3()
		{
			ForEachProvider(db =>
				Assert.AreEqual(3, (from ch in db.Child where ch.ChildID > 3 || ch.ChildID < 4 select ch).Take(3).ToList().Count));
		}

		[Test]
		public void Take4()
		{
			ForEachProvider(db =>
				Assert.AreEqual(3, (from ch in db.Child where ch.ChildID >= 0 && ch.ChildID <= 100 select ch).Take(3).ToList().Count));
		}

		[Test]
		public void Take5()
		{
			ForEachProvider(db => Assert.AreEqual(3, db.Child.Take(3).ToList().Count));
		}

		[Test]
		public void Take6()
		{
			var expected = Child.OrderBy(c => c.ChildID).Take(3);

			ForEachProvider(db =>
			{
				var result = db.Child.OrderBy(c => c.ChildID).Take(3);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			});
		}

		[Test]
		public void Take7()
		{
			ForEachProvider(db => Assert.AreEqual(3, db.Child.Take(() => 3).ToList().Count));
		}

		[Test]
		public void Take8()
		{
			var n = 3;
			ForEachProvider(db => Assert.AreEqual(3, db.Child.Take(() => n).ToList().Count));
		}

		[Test]
		public void TakeCount()
		{
			ForEachProvider(new[] { ProviderName.Sybase }, db => Assert.AreEqual(
				   Child.Take(5).Count(),
				db.Child.Take(5).Count()));
		}

		[Test]
		public void Skip1()
		{
			ForEachProvider(db => AreEqual(Child.Skip(3), db.Child.Skip(3)));
		}

		[Test]
		public void Skip2()
		{
			var expected = (from ch in Child where ch.ChildID > 3 || ch.ChildID < 4 select ch).Skip(3);
			ForEachProvider(db => AreEqual(expected, (from ch in db.Child where ch.ChildID > 3 || ch.ChildID < 4 select ch).Skip(3)));
		}

		[Test]
		public void Skip3()
		{
			var expected = (from ch in Child where ch.ChildID >= 0 && ch.ChildID <= 100 select ch).Skip(3);
			ForEachProvider(db => AreEqual(expected, (from ch in db.Child where ch.ChildID >= 0 && ch.ChildID <= 100 select ch).Skip(3)));
		}

		[Test]
		public void Skip4()
		{
			var expected = Child.OrderByDescending(c => c.ChildID).Skip(3);

			ForEachProvider(db =>
			{
				var result = db.Child.OrderByDescending(c => c.ChildID).Skip(3);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			});
		}

		[Test]
		public void Skip5()
		{
			var expected = Child.OrderByDescending(c => c.ChildID).ThenBy(c => c.ParentID + 1).Skip(3);
			ForEachProvider(db => AreEqual(expected, db.Child.OrderByDescending(c => c.ChildID).ThenBy(c => c.ParentID + 1).Skip(3)));
		}

		[Test]
		public void Skip6()
		{
			ForEachProvider(db => AreEqual(Child.Skip(3), db.Child.Skip(() => 3)));
		}

		[Test]
		public void Skip7()
		{
			var n = 3;
			ForEachProvider(db => AreEqual(Child.Skip(n), db.Child.Skip(() => n)));
		}

		[Test]
		public void SkipCount()
		{
			ForEachProvider(new[] { ProviderName.Sybase, ProviderName.SQLite, ProviderName.Access }, db => Assert.AreEqual(
				   Child.Skip(2).Count(),
				db.Child.Skip(2).Count()));
		}

		[Test]
		public void SkipTake1()
		{
			var expected = Child.OrderByDescending(c => c.ChildID).Skip(2).Take(5);
			ForEachProvider(db =>
			{
				var result = db.Child.OrderByDescending(c => c.ChildID).Skip(2).Take(5);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			});
		}

		[Test]
		public void SkipTake2()
		{
			var expected = Child.OrderByDescending(c => c.ChildID).Take(7).Skip(2);
			ForEachProvider(db =>
			{
				var result = db.Child.OrderByDescending(c => c.ChildID).Take(7).Skip(2);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			});
		}

		[Test]
		public void SkipTake3()
		{
			var expected = Child.OrderBy(c => c.ChildID).Skip(1).Take(7).Skip(2);
			ForEachProvider(db =>
			{
				var result = db.Child.OrderBy(c => c.ChildID).Skip(1).Take(7).Skip(2);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			});
		}

		[Test]
		public void SkipTake4()
		{
			var expected = Child.OrderByDescending(c => c.ChildID).Skip(1).Take(7).OrderBy(c => c.ChildID).Skip(2);
			ForEachProvider(new[] { ProviderName.SQLite, ProviderName.Sybase, ProviderName.Access }, db =>
			{
				var result = db.Child.OrderByDescending(c => c.ChildID).Skip(1).Take(7).OrderBy(c => c.ChildID).Skip(2);
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			});
		}

		[Test]
		public void SkipTake5()
		{
			ForEachProvider(db =>
			{
				var list = db.Child.Skip(2).Take(5).ToList();
				Assert.AreEqual(5, list.Count);
			});
		}

		[Test]
		public void SkipTakeCount()
		{
			ForEachProvider(new[] { ProviderName.SqlCe, ProviderName.Sybase, ProviderName.SQLite, ProviderName.Access }, db => Assert.AreEqual(
				   Child.Skip(2).Take(5).Count(),
				db.Child.Skip(2).Take(5).Count()));
		}

		[Test]
		public void SkipFirst()
		{
			var expected = (from p in Parent where p.ParentID > 1 select p).Skip(1).First();

			ForEachProvider(db =>
			{
				var result = from p in db.GetTable<Parent>() select p;
				result = from p in result where p.ParentID > 1 select p;
				var b = result.Skip(1).First();

				Assert.AreEqual(expected, b);
			});
		}

		[Test]
		public void ElementAt1()
		{
			ForEachProvider(db => Assert.AreEqual(
				(from p in    Parent where p.ParentID > 1 select p).ElementAt(3),
				(from p in db.Parent where p.ParentID > 1 select p).ElementAt(3)));
		}

		[Test]
		public void ElementAt2()
		{
			var n = 3;
			ForEachProvider(db => Assert.AreEqual(
				(from p in    Parent where p.ParentID > 1 select p).ElementAt(n),
				(from p in db.Parent where p.ParentID > 1 select p).ElementAt(() => n)));
		}

		[Test]
		public void ElementAtDefault1()
		{
			ForEachProvider(db => Assert.AreEqual(
				(from p in    Parent where p.ParentID > 1 select p).ElementAtOrDefault(3),
				(from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefault(3)));
		}

		[Test]
		public void ElementAtDefault2()
		{
			ForEachProvider(db => Assert.IsNull((from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefault(300000)));
		}

		[Test]
		public void ElementAtDefault3()
		{
			var n = 3;
			ForEachProvider(db => Assert.AreEqual(
				(from p in    Parent where p.ParentID > 1 select p).ElementAtOrDefault(n),
				(from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefault(() => n)));
		}

		[Test]
		public void ElementAtDefault4()
		{
			var n = 300000;
			ForEachProvider(db => Assert.IsNull((from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefault(() => n)));
		}

		[Test]
		public void ElementAtDefault5()
		{
			ForEachProvider(db => Assert.AreEqual(
				   Person.ElementAtOrDefault(3),
				db.Person.ElementAtOrDefault(3)));
		}
	}
}
