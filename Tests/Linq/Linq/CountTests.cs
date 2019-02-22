using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class CountTests : TestBase
	{
		[Test]
		public void Count1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.Count(),
					db.Parent.Count());
		}

		[Test]
		public async Task Count1Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					         Parent.Count(),
					await db.Parent.CountAsync());
		}

		[Test]
		public void Count2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.Count(p => p.ParentID > 2),
					db.Parent.Count(p => p.ParentID > 2));
		}

		[Test]
		public async Task Count2Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					         Parent.Count     (p => p.ParentID > 2),
					await db.Parent.CountAsync(p => p.ParentID > 2));
		}

		[Test]
		public void Count3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.Count(),
					from p in db.Parent select p.Children.Count());
		}

		[Test]
		public void Count4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Count(),
					from p in db.Parent select db.Child.Count());
		}

		[Test]
		public void Count5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from ch in    Child group ch by ch.ParentID).Count(),
					(from ch in db.Child group ch by ch.ParentID).Count());
		}

		[Test]
		public void Count6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from ch in    Child group ch by ch.ParentID).Count(g => g.Key > 2),
					(from ch in db.Child group ch by ch.ParentID).Count(g => g.Key > 2));
		}

		[Test]
		public void Count7([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.Children.Count > 2 select p,
					from p in db.Parent where p.Children.Count > 2 select p);
		}

		[ActiveIssue("not supported?", Configurations = new[] { ProviderName.SapHana })]
		[Test]
		public void SubQueryCount([IncludeDataSources(
				ProviderName.SqlServer2008, ProviderName.SqlServer2012,
				ProviderName.SqlServer2014, ProviderName.SapHana)]
			string context)
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

		[Test]
		public void GroupBy1([DataSources] string context)
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

		[Test]
		public void GroupBy101([DataSources] string context)
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

		[Test]
		public void GroupBy102([DataSources(ProviderName.SqlCe)] string context)
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

		[Test]
		public void GroupBy103([DataSources] string context)
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

		[ActiveIssue("Unsupported by Informix?", Configurations = new[] { ProviderName.Informix, ProviderName.SapHana })]
		[Test]
		public void GroupBy21([DataSources] string context)
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

		[ActiveIssue("Unsupported by Informix?", Configurations = new[] { ProviderName.Informix, ProviderName.SapHana })]
		[Test]
		public void GroupBy22([DataSources] string context)
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

		[ActiveIssue("Unsupported by Informix?", Configurations = new[] { ProviderName.Informix })]
		[Test]
		public void GroupBy23([DataSources(
			ProviderName.SqlCe, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.SqlServer2000, ProviderName.Sybase, ProviderName.Access)]
			string context)
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

		[ActiveIssue("Unsupported by Informix?", Configurations = new[] { ProviderName.Informix })]
		[Test]
		public void GroupBy3([DataSources] string context)
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

		[Test]
		public void GroupBy4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Child.Count();
				var result   = db.Child.Count();
				Assert.AreEqual(expected, result);
			}
		}

		[Test]
		public void GroupBy5([DataSources(ProviderName.SqlCe)] string context)
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

		[Test]
		public void GroupBy6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					(from ch in    Child group ch by ch.ParentID).Count(),
					(from ch in db.Child group ch by ch.ParentID).Count());
		}

		[Test]
		public void GroupBy7([DataSources] string context)
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

		[Test]
		public void GroupByWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = Child.Count(ch => ch.ChildID > 20);
				Assert.AreNotEqual(0, expected);

				var result = db.Child.Count(ch => ch.ChildID > 20);
				Assert.AreEqual(expected, result);
			}
		}

		[Test]
		public void GroupByWhere1([DataSources] string context)
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

		[Test]
		public void GroupByWhere2([DataSources] string context)
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

		[Test]
		public void GroupByWhere201([DataSources(ProviderName.SqlCe)] string context)
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

		[Test]
		public void GroupByWhere202([DataSources(ProviderName.SqlCe)] string context)
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

		[Test]
		public void GroupByWhere203([DataSources(ProviderName.SqlCe)] string context)
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

		[Test]
		public void GroupByWhere3([DataSources] string context)
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

		[Test]
		public void GroupByWhere301([DataSources] string context)
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

		[Test]
		public void GroupByWhere4([DataSources] string context)
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

		[Test]
		public void SubQuery1([DataSources] string context)
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

		[Test]
		public void SubQuery2([DataSources] string context)
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

		[Test]
		public void SubQuery3([DataSources] string context)
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

		[Test]
		public void SubQuery4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Count =    Parent.Count(p1 => p1.ParentID == p.ParentID) },
					from p in db.Parent select new { Count = db.Parent.Count(p1 => p1.ParentID == p.ParentID) });
		}

		[Test]
		public void SubQuery5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Count =    Parent.Where(p1 => p1.ParentID == p.ParentID).Count() },
					from p in db.Parent select new { Count = db.Parent.Where(p1 => p1.ParentID == p.ParentID).Count() });
		}

		[Test]
		public void SubQuery6([DataSources(
			ProviderName.SqlCe, ProviderName.SQLiteClassic, ProviderName.SQLiteMS,
			ProviderName.Sybase, ProviderName.SybaseManaged)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Take(5).OrderByDescending(p => p.ParentID).Select(p => p.Children.Count()),
					db.Parent.Take(5).OrderByDescending(p => p.ParentID).Select(p => p.Children.Count()));
		}

		[Test]
		public void SubQuery7([DataSources(
			ProviderName.SqlCe, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.Access /* Fix It*/)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child.Count(c => c.Parent == p),
					from p in db.Parent select db.Child.Count(c => c.Parent == p));
		}

		[Test]
		public void SubQueryMax1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.Max(p =>    Child.Count(c => c.Parent.ParentID == p.ParentID)),
					db.Parent.Max(p => db.Child.Count(c => c.Parent.ParentID == p.ParentID)));
		}

		[Test]
		public async Task SubQueryMax1Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					         Parent.Max     (p =>    Child.Count(c => c.Parent.ParentID == p.ParentID)),
					await db.Parent.MaxAsync(p => db.Child.Count(c => c.Parent.ParentID == p.ParentID)));
		}

		[Test]
		public void SubQueryMax2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Parent.Max(p => p.Children.Count()),
					db.Parent.Max(p => p.Children.Count()));
		}

		[Test]
		public void GroupJoin1([DataSources] string context)
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

		[Test]
		public void GroupJoin2([DataSources] string context)
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

		[Test]
		public void GroupJoin3([DataSources] string context)
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

		[Test]
		public void GroupJoin4([DataSources] string context)
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

		[Test]
		public void Count8([DataSources] string context)
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
			[Column] public int? ParentID;
			[Column] public int  ChildID;

			[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
			public Parent Parent;
		}

		[Test]
		public void Count9([DataSources] string context)
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
