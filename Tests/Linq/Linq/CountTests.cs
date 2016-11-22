using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class CountTests : TestBase
	{
		[Test, DataContextSource]
		public void Count1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.Count(),
					db.Parent.Count());
		}

		[Test, DataContextSource]
		public void Count2(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.Count(p => p.ParentID > 2),
					db.Parent.Count(p => p.ParentID > 2));
		}

		[Test, DataContextSource]
		public void Count3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.Count(),
					from p in db.Parent select p.Children.Count());
		}

		[Test, DataContextSource]
		public void Count4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Count(),
					from p in db.Parent select db.Child.Count());
		}

		[Test, DataContextSource]
		public void Count5(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from ch in    Child group ch by ch.ParentID).Count(),
					(from ch in db.Child group ch by ch.ParentID).Count());
		}

		[Test, DataContextSource]
		public void Count6(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from ch in    Child group ch by ch.ParentID).Count(g => g.Key > 2),
					(from ch in db.Child group ch by ch.ParentID).Count(g => g.Key > 2));
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void Count7(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.Children.Count > 2 select p,
					from p in db.Parent where p.Children.Count > 2 select p);
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SapHana)]
		public void SubQueryCount(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				AreEqual(
					from p in Parent
					select Parent.Where(t => t.ParentID == p.ParentID).Count()
					,
					from p in db.Parent
					select Sql.AsSql(db.GetParentByID(p.ParentID).Count()));
			}
		}

		[Test, DataContextSource]
		public void GroupBy1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					select g.Count(ch => ch.ChildID > 20),
					from ch in db.Child
					group ch by ch.ParentID into g
					select g.Count(ch => ch.ChildID > 20));
		}

		[Test, DataContextSource]
		public void GroupBy101(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					select g.Count(),
					from ch in db.Child
					group ch by ch.ParentID into g
					select g.Count());
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void GroupBy102(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
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
					});
		}

		[Test, DataContextSource]
		public void GroupBy103(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by new { Parent = ch.ParentID, ch.ChildID } into g
					select g.Count(ch => ch.ChildID > 20),
					from ch in db.Child
					group ch by new { Parent = ch.ParentID, ch.ChildID } into g
					select g.Count(ch => ch.ChildID > 20));
		}

		[Test, DataContextSource]
		public void GroupBy21(string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in
						from ch in Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
					where ch.ParentID + 1 > n
					group ch by ch into g
					select g.Count(p => p.ParentID < 3),
					from ch in
						from ch in db.Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
					where ch.ParentID + 1 > n
					group ch by ch into g
					select g.Count(p => p.ParentID < 3));
		}

		[Test, DataContextSource]
		public void GroupBy22(string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in
						from ch in Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
					where ch.ParentID + 1 > n
					group ch by new { ch.ParentID } into g
					select g.Count(p => p.ParentID < 3),
					from ch in
						from ch in db.Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
					where ch.ParentID + 1 > n
					group ch by new { ch.ParentID } into g
					select g.Count(p => p.ParentID < 3));
		}

		[Test, DataContextSource(
			ProviderName.SqlCe, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.SqlServer2000, ProviderName.Sybase, ProviderName.Access)]
		public void GroupBy23(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in
						from p in Parent select new { ParentID = p.ParentID + 1, p.Value1 }
					where p.ParentID + 1 > 1
					group p by new { p.Value1 } into g
					select g.Count(p => p.ParentID < 3),
					from p in
						from p in db.Parent select new { ParentID = p.ParentID + 1, p.Value1 }
					where p.ParentID + 1 > 1
					group p by new { p.Value1 } into g
					select g.Count(p => p.ParentID < 3));
		}

		[Test, DataContextSource]
		public void GroupBy3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in
						from ch in Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
					where ch.ParentID - 1 > 0
					group ch by new { ch.ParentID } into g
					select new
					{
						g.Key.ParentID,
						ChildMin   = g.Min(p => p.ChildID),
						ChildCount = g.Count(p => p.ChildID > 25)
					},
					from ch in
						from ch in db.Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
					where ch.ParentID - 1 > 0
					group ch by new { ch.ParentID } into g
					select new
					{
						g.Key.ParentID,
						ChildMin   = g.Min(p => p.ChildID),
						ChildCount = g.Count(p => p.ChildID > 25)
					});
		}

		[Test, DataContextSource]
		public void GroupBy4(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Child.Count();
				var result   = db.Child.Count();
				Assert.AreEqual(expected, result);
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void GroupBy5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
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
					});
		}

		[Test, DataContextSource]
		public void GroupBy6(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from ch in    Child group ch by ch.ParentID).Count(),
					(from ch in db.Child group ch by ch.ParentID).Count());
		}

		[Test, DataContextSource]
		public void GroupBy7(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
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
					});
		}

		[Test, DataContextSource]
		public void GroupByWhere(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Child.Count(ch => ch.ChildID > 20);
				Assert.AreNotEqual(0, expected);

				var result = db.Child.Count(ch => ch.ChildID > 20);
				Assert.AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void GroupByWhere1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					where g.Key > 2
					select g.Key,
					from ch in db.Child
					group ch by ch.ParentID into g
					where g.Key > 2
					select g.Key);
		}

		[Test, DataContextSource]
		public void GroupByWhere2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					where g.Count() > 2
					select g.Key,
					from ch in db.Child
					group ch by ch.ParentID into g
					where g.Count() > 2
					select g.Key);
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void GroupByWhere201(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					where g.Count(ch => ch.ChildID > 20) > 2
					select g.Key,
					from ch in db.Child
					group ch by ch.ParentID into g
					where g.Count(ch => ch.ChildID > 20) > 2
					select g.Key);
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void GroupByWhere202(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					where g.Count(ch => ch.ChildID > 20) > 2 || g.Count(ch => ch.ChildID == 20) > 2
					select g.Key,
					from ch in db.Child
					group ch by ch.ParentID into g
					where g.Count(ch => ch.ChildID > 20) > 2 || g.Count(ch => ch.ChildID == 20) > 2
					select g.Key);
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void GroupByWhere203(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					where g.Count(ch => ch.ChildID > 20) > 2 || g.Key > 2
					select g.Key,
					from ch in db.Child
					group ch by ch.ParentID into g
					where g.Count(ch => ch.ChildID > 20) > 2 || g.Key > 2
					select g.Key);
		}

		[Test, DataContextSource]
		public void GroupByWhere3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					where g.Count() > 2 && g.Key < 5
					select g.Key,
					from ch in db.Child
					group ch by ch.ParentID into g
					where g.Count() > 2 && g.Key < 5
					select g.Key);
		}

		[Test, DataContextSource]
		public void GroupByWhere301(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					where g.Count() > 3 || g.Key == 1
					select g.Key,
					from ch in db.Child
					group ch by ch.ParentID into g
					where g.Count() > 3 || g.Key == 1
					select g.Key);
		}

		[Test, DataContextSource]
		public void GroupByWhere4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where g.Count() > 2
					select g.Key.ParentID
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where g.Count() > 2
					select g.Key.ParentID);
		}

		[Test, DataContextSource]
		public void SubQuery1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID != 5
					select new { p.ParentID, Count = p.Children.Where(c => c.ParentID == p.ParentID && c.ChildID != 0m).Count() },
					from p in db.Parent
					where p.ParentID != 5
					select new { p.ParentID, Count = p.Children.Where(c => c.ParentID == p.ParentID && c.ChildID != 0m).Count() });
		}

		[Test, DataContextSource]
		public void SubQuery2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID != 5
					select new { Count = p.Value1 == null ? p.Children.Count : p.Children.Count(c => c.ParentID == p.ParentID) },
					from p in db.Parent
					where p.ParentID != 5
					select new { Count = p.Value1 == null ? p.Children.Count : p.Children.Count(c => c.ParentID == p.ParentID) });
		}

		[Test, DataContextSource]
		public void SubQuery3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID != 5
					select new { Count = p.Value1 == null ? p.Children.Count() : p.Children.Count(c => c.ParentID == p.ParentID) },
					from p in db.Parent
					where p.ParentID != 5
					select new { Count = p.Value1 == null ? p.Children.Count() : p.Children.Count(c => c.ParentID == p.ParentID) });
		}

		[Test, DataContextSource]
		public void SubQuery4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Count =    Parent.Count(p1 => p1.ParentID == p.ParentID) },
					from p in db.Parent select new { Count = db.Parent.Count(p1 => p1.ParentID == p.ParentID) });
		}

		[Test, DataContextSource]
		public void SubQuery5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Count =    Parent.Where(p1 => p1.ParentID == p.ParentID).Count() },
					from p in db.Parent select new { Count = db.Parent.Where(p1 => p1.ParentID == p.ParentID).Count() });
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.Sybase)]
		public void SubQuery6(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Take(5).OrderByDescending(p => p.ParentID).Select(p => p.Children.Count()),
					db.Parent.Take(5).OrderByDescending(p => p.ParentID).Select(p => p.Children.Count()));
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.Sybase, ProviderName.Access /* Fix It*/)]
		public void SubQuery7(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Count(c => c.Parent == p),
					from p in db.Parent select db.Child.Count(c => c.Parent == p));
		}

		[Test, DataContextSource]
		public void SubQueryMax1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.Max(p =>    Child.Count(c => c.Parent.ParentID == p.ParentID)),
					db.Parent.Max(p => db.Child.Count(c => c.Parent.ParentID == p.ParentID)));
		}

		[Test, DataContextSource]
		public void SubQueryMax2(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.Max(p => p.Children.Count()),
					db.Parent.Max(p => p.Children.Count()));
		}

		[Test, DataContextSource]
		public void GroupJoin1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
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
					});
		}

		[Test, DataContextSource]
		public void GroupJoin2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
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
					});
		}

		[Test, DataContextSource]
		public void GroupJoin3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
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
					});
		}

		[Test, DataContextSource]
		public void GroupJoin4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
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
					});
		}

		[Test, DataContextSource]
		public void Count8(string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(
					   Child.Select(ch => ch.Parent.ParentID).Count(p => p == 1),
					db.Child.Select(ch => ch.Parent.ParentID).Count(p => p == 1));
				Assert.AreEqual(
					db.Child.Select(ch => ch.Parent.ParentID).ToList().Count(p => p == 1),
					db.Child.Select(ch => ch.Parent.ParentID).Count(p => p == 1));
			}
		}

		[Table("Child")]
		class Child2
		{
#pragma warning disable 0649
			[Column] public int? ParentID;
			[Column] public int  ChildID;

			[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
			public Parent Parent;
#pragma warning restore 0649
		}

		[Test, DataContextSource]
		public void Count9(string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(
					db.GetTable<Child2>().Select(ch => ch.Parent.ParentID).ToList().Count(p => p == 1),
					db.GetTable<Child2>().Select(ch => ch.Parent.ParentID).Count(p => p == 1));
			}
		}
	}
}
