using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class CountTest : TestBase
	{
		[Test]
		public void Count1()
		{
			ForEachProvider(db => Assert.AreEqual(
				   Parent.Count(),
				db.Parent.Count()));
		}

		[Test]
		public void Count2()
		{
			ForEachProvider(db => Assert.AreEqual(
				   Parent.Count(p => p.ParentID > 2),
				db.Parent.Count(p => p.ParentID > 2)));
		}

		[Test]
		public void Count3()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent select p.Children.Count(),
				from p in db.Parent select p.Children.Count()));
		}

		[Test]
		public void Count4()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent select    Child.Count(),
				from p in db.Parent select db.Child.Count()));
		}

		[Test]
		public void Count5()
		{
			ForEachProvider(db => Assert.AreEqual(
				(from ch in    Child group ch by ch.ParentID).Count(),
				(from ch in db.Child group ch by ch.ParentID).Count()));
		}

		[Test]
		public void Count6()
		{
			ForEachProvider(db => Assert.AreEqual(
				(from ch in    Child group ch by ch.ParentID).Count(g => g.Key > 2),
				(from ch in db.Child group ch by ch.ParentID).Count(g => g.Key > 2)));
		}

		[Test]
		public void Count7()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				from p in    Parent where p.Children.Count > 2 select p,
				from p in db.Parent where p.Children.Count > 2 select p));
		}

		[Test]
		public void GroupBy1()
		{
			var expected =
				from ch in Child
				group ch by ch.ParentID into g
				select g.Count(ch => ch.ChildID > 20);

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child
				group ch by ch.ParentID into g
				select g.Count(ch => ch.ChildID > 20)));
		}

		[Test]
		public void GroupBy101()
		{
			var expected =
				from ch in Child
				group ch by ch.ParentID into g
				select g.Count();

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child
				group ch by ch.ParentID into g
				select g.Count()));
		}

		[Test]
		public void GroupBy102()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select new
				{
					ID1 = g.Max  (ch => ch.ChildID),
					ID2 = g.Count(ch => ch.ChildID > 20) + 1,
					ID3 = g.Count(ch => ch.ChildID > 20),
					ID4 = g.Count(ch => ch.ChildID > 10),
				},
				from ch in db.Child
				group ch by ch.ParentID into g
				select new
				{
					ID1 = g.Max  (ch => ch.ChildID),
					ID2 = g.Count(ch => ch.ChildID > 20) + 1,
					ID3 = g.Count(ch => ch.ChildID > 20),
					ID4 = g.Count(ch => ch.ChildID > 10),
				}));
		}

		[Test]
		public void GroupBy103()
		{
			var expected =
				from ch in Child
				group ch by new { Parent = ch.ParentID, ch.ChildID } into g
				select g.Count(ch => ch.ChildID > 20);

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child
				group ch by new { Parent = ch.ParentID, ch.ChildID } into g
				select g.Count(ch => ch.ChildID > 20)));
		}


		[Test]
		public void GroupBy21()
		{
			var n = 1;

			ForEachProvider(db => AreEqual(
				from ch in
					from ch in Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
				where ch.ParentID + 1 > n
				group ch by ch into g
				select g.Count(p => p.ParentID < 3),
				from ch in
					from ch in db.Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
				where ch.ParentID + 1 > n
				group ch by ch into g
				select g.Count(p => p.ParentID < 3)));
		}

		[Test]
		public void GroupBy22()
		{
			var n = 1;

			var expected =
				from ch in
					from ch in Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
				where ch.ParentID + 1 > n
				group ch by new { ch.ParentID } into g
				select g.Count(p => p.ParentID < 3);

			ForEachProvider(db => AreEqual(expected,
				from ch in
					from ch in db.Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
				where ch.ParentID + 1 > n
				group ch by new { ch.ParentID } into g
				select g.Count(p => p.ParentID < 3)));
		}

		[Test]
		public void GroupBy23()
		{
			ForEachProvider(new[] { ProviderName.SqlCe, "Oracle", ProviderName.Sybase, ProviderName.Access }, db => AreEqual(
				from p in
					from p in Parent select new { ParentID = p.ParentID + 1, p.Value1 }
				where p.ParentID + 1 > 1
				group p by new { p.Value1 } into g
				select g.Count(p => p.ParentID < 3),
				from p in
					from p in db.Parent select new { ParentID = p.ParentID + 1, p.Value1 }
				where p.ParentID + 1 > 1
				group p by new { p.Value1 } into g
				select g.Count(p => p.ParentID < 3)));
		}

		[Test]
		public void GroupBy3()
		{
			var expected =
				from ch in
					from ch in Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
				where ch.ParentID - 1 > 0
				group ch by new { ch.ParentID } into g
				select new
				{
					g.Key.ParentID,
					ChildMin   = g.Min(p => p.ChildID),
					ChildCount = g.Count(p => p.ChildID > 25)
				};

			ForEachProvider(db => AreEqual(expected,
				from ch in
					from ch in db.Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
				where ch.ParentID - 1 > 0
				group ch by new { ch.ParentID } into g
				select new
				{
					g.Key.ParentID,
					ChildMin   = g.Min(p => p.ChildID),
					ChildCount = g.Count(p => p.ChildID > 25)
				}));
		}

		[Test]
		public void GroupBy4()
		{
			var expected = Child.Count();

			ForEachProvider(db =>
			{
				var result = db.Child.Count();
				Assert.AreEqual(expected, result);
			});
		}

		[Test]
		public void GroupBy5()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select new
				{
					ID1 = g.Max  (ch => ch.ChildID),
					ID2 = g.Count(ch => ch.ChildID > 20) + 1,
					ID3 = g.Count(ch => ch.ChildID > 20),
					ID4 = g.Count(ch => ch.ChildID > 10),
				},
				from ch in db.Child
				group ch by ch.ParentID into g
				select new
				{
					ID1 = g.Max  (ch => ch.ChildID),
					ID2 = g.Count(ch => ch.ChildID > 20) + 1,
					ID3 = g.Count(ch => ch.ChildID > 20),
					ID4 = g.Count(ch => ch.ChildID > 10),
				}));
		}

		[Test]
		public void GroupBy6()
		{
			ForEachProvider(db => Assert.AreEqual(
				(from ch in    Child group ch by ch.ParentID).Count(),
				(from ch in db.Child group ch by ch.ParentID).Count()));
		}

		[Test]
		public void GroupBy7()
		{
			ForEachProvider(db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select new
				{
					ID1 = g.Count(),
					ID2 = g.Max  (ch => ch.ChildID),
				},
				from ch in db.Child
				group ch by ch.ParentID into g
				select new
				{
					ID1 = g.Count(),
					ID2 = g.Max  (ch => ch.ChildID),
				}));
		}

		[Test]
		public void GroupByWhere()
		{
			var expected = Child.Count(ch => ch.ChildID > 20);
			Assert.AreNotEqual(0, expected);

			ForEachProvider(db =>
			{
				var result = db.Child.Count(ch => ch.ChildID > 20);
				Assert.AreEqual(expected, result);
			});
		}

		[Test]
		public void GroupByWhere1()
		{
			ForEachProvider(db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				where g.Key > 2
				select g.Key,
				from ch in db.Child
				group ch by ch.ParentID into g
				where g.Key > 2
				select g.Key));
		}

		[Test]
		public void GroupByWhere2()
		{
			ForEachProvider(db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				where g.Count() > 2
				select g.Key,
				from ch in db.Child
				group ch by ch.ParentID into g
				where g.Count() > 2
				select g.Key));
		}

		[Test]
		public void GroupByWhere201()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				where g.Count(ch => ch.ChildID > 20) > 2
				select g.Key,
				from ch in db.Child
				group ch by ch.ParentID into g
				where g.Count(ch => ch.ChildID > 20) > 2
				select g.Key));
		}

		[Test]
		public void GroupByWhere202()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				where g.Count(ch => ch.ChildID > 20) > 2 || g.Count(ch => ch.ChildID == 20) > 2
				select g.Key,
				from ch in db.Child
				group ch by ch.ParentID into g
				where g.Count(ch => ch.ChildID > 20) > 2 || g.Count(ch => ch.ChildID == 20) > 2
				select g.Key));
		}

		[Test]
		public void GroupByWhere203()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				where g.Count(ch => ch.ChildID > 20) > 2 || g.Key > 2
				select g.Key,
				from ch in db.Child
				group ch by ch.ParentID into g
				where g.Count(ch => ch.ChildID > 20) > 2 || g.Key > 2
				select g.Key));
		}

		[Test]
		public void GroupByWhere3()
		{
			ForEachProvider(db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				where g.Count() > 2 && g.Key < 5
				select g.Key,
				from ch in db.Child
				group ch by ch.ParentID into g
				where g.Count() > 2 && g.Key < 5
				select g.Key));
		}

		[Test]
		public void GroupByWhere301()
		{
			ForEachProvider(db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				where g.Count() > 3 || g.Key == 1
				select g.Key,
				from ch in db.Child
				group ch by ch.ParentID into g
				where g.Count() > 3 || g.Key == 1
				select g.Key));
		}

		[Test]
		public void GroupByWhere4()
		{
			ForEachProvider(db => AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent into g
				where g.Count() > 2
				select g.Key.ParentID
				,
				from ch in db.GrandChild1
				group ch by ch.Parent into g
				where g.Count() > 2
				select g.Key.ParentID));
		}

		[Test]
		public void SubQuery1()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				where p.ParentID != 5
				select new { p.ParentID, Count = p.Children.Where(c => c.ParentID == p.ParentID && c.ChildID != 0m).Count() },
				from p in db.Parent
				where p.ParentID != 5
				select new { p.ParentID, Count = p.Children.Where(c => c.ParentID == p.ParentID && c.ChildID != 0m).Count() }));
		}

		[Test]
		public void SubQuery2()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				where p.ParentID != 5
				select new { Count = p.Value1 == null ? p.Children.Count : p.Children.Count(c => c.ParentID == p.ParentID) },
				from p in db.Parent
				where p.ParentID != 5
				select new { Count = p.Value1 == null ? p.Children.Count : p.Children.Count(c => c.ParentID == p.ParentID) }));
		}

		[Test]
		public void SubQuery3()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				where p.ParentID != 5
				select new { Count = p.Value1 == null ? p.Children.Count() : p.Children.Count(c => c.ParentID == p.ParentID) },
				from p in db.Parent
				where p.ParentID != 5
				select new { Count = p.Value1 == null ? p.Children.Count() : p.Children.Count(c => c.ParentID == p.ParentID) }));
		}

		[Test]
		public void SubQuery4()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent select new { Count =    Parent.Count(p1 => p1.ParentID == p.ParentID) },
				from p in db.Parent select new { Count = db.Parent.Count(p1 => p1.ParentID == p.ParentID) }));
		}

		[Test]
		public void SubQuery5()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent select new { Count =    Parent.Where(p1 => p1.ParentID == p.ParentID).Count() },
				from p in db.Parent select new { Count = db.Parent.Where(p1 => p1.ParentID == p.ParentID).Count() }));
		}

		[Test]
		public void SubQuery6()
		{
			ForEachProvider(new[] { ProviderName.SqlCe, ProviderName.SQLite, ProviderName.Sybase }, db => AreEqual(
				   Parent.Take(5).OrderByDescending(p => p.ParentID).Select(p => p.Children.Count()),
				db.Parent.Take(5).OrderByDescending(p => p.ParentID).Select(p => p.Children.Count())));
		}

		[Test]
		public void SubQuery7()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe, "Oracle", ProviderName.Sybase, ProviderName.Access }, ///// Fix It
				db => AreEqual(
					from p in    Parent select    Child.Count(c => c.Parent == p),
					from p in db.Parent select db.Child.Count(c => c.Parent == p)));
		}

		[Test]
		public void SubQueryMax1()
		{
			ForEachProvider(
				db => Assert.AreEqual(
					   Parent.Max(p =>    Child.Count(c => c.Parent.ParentID == p.ParentID)),
					db.Parent.Max(p => db.Child.Count(c => c.Parent.ParentID == p.ParentID))));
		}

		[Test]
		public void SubQueryMax2()
		{
			ForEachProvider(
				db => Assert.AreEqual(
					   Parent.Max(p => p.Children.Count()),
					db.Parent.Max(p => p.Children.Count())));
		}

		[Test]
		public void GroupJoin1()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				join c in Child on p.ParentID equals c.ParentID into gc
				join g in GrandChild on p.ParentID equals g.ParentID into gg
				select new
				{
					Count1 = gc.Count(),
					Count2 = gg.Count()
				},
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID into gc
				join g in db.GrandChild on p.ParentID equals g.ParentID into gg
				select new
				{
					Count1 = gc.Count(),
					Count2 = gg.Count()
				}));
		}

		[Test]
		public void GroupJoin2()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				join c in Child on p.ParentID equals c.ParentID into gc
				join g in GrandChild on p.ParentID equals g.ParentID into gg
				let gc1 = gc
				let gg1 = gg
				select new
				{
					Count1 = gc1.Count(),
					Count2 = gg1.Count()
				} ,
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID into gc
				join g in db.GrandChild on p.ParentID equals g.ParentID into gg
				let gc1 = gc
				let gg1 = gg
				select new
				{
					Count1 = gc.Count(),
					Count2 = gg.Count()
				}));
		}

		[Test]
		public void GroupJoin3()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				join c in Child on p.ParentID equals c.ParentID into gc
				select new
				{
					Count1 = gc.Count(),
				},
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID into gc
				select new
				{
					Count1 = gc.Count(),
				}));
		}

		[Test]
		public void GroupJoin4()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				join c in Child on p.ParentID equals c.ParentID into gc
				select new
				{
					Count1 = gc.Count() + gc.Count(),
				},
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID into gc
				select new
				{
					Count1 = gc.Count() + gc.Count(),
				}));
		}
	}
}
