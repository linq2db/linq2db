using System;
using System.Linq;

using NUnit.Framework;

using LinqToDB.Data.DataProvider;

namespace Data.Linq
{
	using Model;

	[TestFixture]
	public class GroupByTest : TestBase
	{
		[Test]
		public void Simple1()
		{
			LinqToDB.Common.Configuration.Linq.PreloadGroups = true;

			ForEachProvider(db =>
			{
				var q =
					from ch in db.Child
					group ch by ch.ParentID;

				var list = q.ToList().Where(n => n.Key < 6).OrderBy(n => n.Key).ToList();

				Assert.AreEqual(4, list.Count);

				for (var i = 0; i < list.Count; i++)
				{
					var values = list[i].OrderBy(c => c.ChildID).ToList();

					Assert.AreEqual(i + 1, list[i].Key);
					Assert.AreEqual(i + 1, values.Count);

					for (var j = 0; j < values.Count; j++)
						Assert.AreEqual((i + 1) * 10 + j + 1, values[j].ChildID);
				}
			});
		}

		[Test]
		public void Simple2()
		{
			LinqToDB.Common.Configuration.Linq.PreloadGroups = false;

			ForEachProvider(db =>
			{
				var q =
					from ch in db.GrandChild
					group ch by new { ch.ParentID, ch.ChildID };

				var list = q.ToList();

				Assert.AreEqual   (8, list.Count);
				Assert.AreNotEqual(0, list.OrderBy(c => c.Key.ParentID).First().ToList().Count);
			});
		}

		[Test]
		public void Simple3()
		{
			ForEachProvider(db =>
			{
				var q =
					from ch in db.Child
					group ch by ch.ParentID into g
					select g.Key;

				var list = q.ToList().Where(n => n < 6).OrderBy(n => n).ToList();

				Assert.AreEqual(4, list.Count);
				for (var i = 0; i < list.Count; i++) Assert.AreEqual(i + 1, list[i]);
			});
		}

		[Test]
		public void Simple4()
		{
			ForEachProvider(db =>
			{
				var q =
					from ch in db.Child
					group ch by ch.ParentID into g
					orderby g.Key
					select g.Key;

				var list = q.ToList().Where(n => n < 6).ToList();

				Assert.AreEqual(4, list.Count);
				for (var i = 0; i < list.Count; i++) Assert.AreEqual(i + 1, list[i]);
			});
		}

		[Test]
		public void Simple5()
		{
			var expected =
				from ch in GrandChild
				group ch by new { ch.ParentID, ch.ChildID } into g
				group g  by new { g.Key.ParentID }          into g
				select g.Key;

			ForEachProvider(db => AreEqual(expected,
				from ch in db.GrandChild
				group ch by new { ch.ParentID, ch.ChildID } into g
				group g  by new { g.Key.ParentID }          into g
				select g.Key));
		}

		[Test]
		public void Simple6()
		{
			ForEachProvider(db =>
			{
				var q    = db.GrandChild.GroupBy(ch => new { ch.ParentID, ch.ChildID }, ch => ch.GrandChildID);
				var list = q.ToList();

				Assert.AreNotEqual(0, list[0].Count());
				Assert.AreEqual   (8, list.Count);
			});
		}

		[Test]
		public void Simple7()
		{
			ForEachProvider(db =>
			{
				var q = db.GrandChild
					.GroupBy(ch => new { ch.ParentID, ch.ChildID }, ch => ch.GrandChildID)
					.Select (gr => new { gr.Key.ParentID, gr.Key.ChildID });

				var list = q.ToList();
				Assert.AreEqual(8, list.Count);
			});
		}

		[Test]
		public void Simple8()
		{
			ForEachProvider(db =>
			{
				var q = db.GrandChild.GroupBy(ch => new { ch.ParentID, ch.ChildID }, (g,ch) => g.ChildID);

				var list = q.ToList();
				Assert.AreEqual(8, list.Count);
			});
		}

		[Test]
		public void Simple9()
		{
			ForEachProvider(db =>
			{
				var q    = db.GrandChild.GroupBy(ch => new { ch.ParentID, ch.ChildID }, ch => ch.GrandChildID,  (g,ch) => g.ChildID);
				var list = q.ToList();

				Assert.AreEqual(8, list.Count);
			});
		}

		[Test]
		public void Simple10()
		{
			var expected = (from ch in Child group ch by ch.ParentID into g select g).ToList().OrderBy(p => p.Key).ToList();

			ForEachProvider(db =>
			{
				var result = (from ch in db.Child group ch by ch.ParentID into g select g).ToList().OrderBy(p => p.Key).ToList();

				AreEqual(expected[0], result[0]);
				AreEqual(expected.Select(p => p.Key), result.Select(p => p.Key));
				AreEqual(expected[0].ToList(), result[0].ToList());
			});
		}

		[Test]
		public void Simple11()
		{
			ForEachProvider(db =>
			{
				var q1 = GrandChild
					.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, ch => ch.ChildID);

				var q2 = db.GrandChild
					.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, ch => ch.ChildID);

				var list1 = q1.AsEnumerable().OrderBy(_ => _.Key.ChildID).ToList();
				var list2 = q2.AsEnumerable().OrderBy(_ => _.Key.ChildID).ToList();

				Assert.AreEqual(list1.Count,       list2.Count);
				Assert.AreEqual(list1[0].ToList(), list2[0].ToList());
			});
		}

		[Test]
		public void Simple12()
		{
			ForEachProvider(db =>
			{
				var q = db.GrandChild
					.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, (g,ch) => g.ChildID);

				var list = q.ToList();
				Assert.AreEqual(8, list.Count);
			});
		}

		[Test]
		public void Simple13()
		{
			ForEachProvider(db =>
			{
				var q = db.GrandChild
					.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, ch => ch.ChildID, (g,ch) => g.ChildID);

				var list = q.ToList();
				Assert.AreEqual(8, list.Count);
			});
		}

		//[Test]
		public void Simple14()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent
				select
					from c in p.Children
					group c by c.ParentID into g
					select g.Key,
				from p in db.Parent
				select
					from c in p.Children
					group c by c.ParentID into g
					select g.Key));
		}

		[Test]
		public void MemberInit()
		{
			ForEachProvider(db => AreEqual(
				from ch in Child
				group ch by new Child { ParentID = ch.ParentID } into g
				select g.Key,
				from ch in db.Child
				group ch by new Child { ParentID = ch.ParentID } into g
				select g.Key));
		}

		[Test]
		public void SubQuery1()
		{
			var n = 1;

			var expected =
				from ch in
					from ch in Child select ch.ParentID + 1
				where ch + 1 > n
				group ch by ch into g
				select g.Key;

			ForEachProvider(db => AreEqual(expected,
				from ch in
					from ch in db.Child select ch.ParentID + 1
				where ch > n
				group ch by ch into g
				select g.Key));
		}

		[Test]
		public void SubQuery2()
		{
			var n = 1;

			var expected =
				from ch in Child select new { ParentID = ch.ParentID + 1 } into ch
				where ch.ParentID > n
				group ch by ch into g
				select g.Key;

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child select new { ParentID = ch.ParentID + 1 } into ch
				where ch.ParentID > n
				group ch by ch into g
				select g.Key));
		}

		[Test]
		public void SubQuery3()
		{
			ForEachProvider(db => AreEqual(
				from ch in
					from ch in Child
					select new { ch, n = ch.ChildID + 1 }
				group ch by ch.n into g
				select new
				{
					g.Key,
					Sum = g.Sum(_ => _.ch.ParentID)
				},
				from ch in
					from ch in db.Child
					select new { ch, n = ch.ChildID + 1 }
				group ch by ch.n into g
				select new
				{
					g.Key,
					Sum = g.Sum(_ => _.ch.ParentID)
				}));
		}

		[Test]
		public void SubQuery31()
		{
			ForEachProvider(db => AreEqual(
				from ch in
					from ch in Child
					select new { ch, n = ch.ChildID + 1 }
				group ch.ch by ch.n into g
				select new
				{
					g.Key,
					Sum = g.Sum(_ => _.ParentID)
				},
				from ch in
					from ch in db.Child
					select new { ch, n = ch.ChildID + 1 }
				group ch.ch by ch.n into g
				select new
				{
					g.Key,
					Sum = g.Sum(_ => _.ParentID)
				}));
		}

		[Test]
		public void SubQuery32()
		{
			ForEachProvider(db => AreEqual(
				from ch in
					from ch in Child
					select new { ch, n = ch.ChildID + 1 }
				group ch.ch.ParentID by ch.n into g
				select new
				{
					g.Key,
					Sum = g.Sum(_ => _)
				},
				from ch in
					from ch in db.Child
					select new { ch, n = ch.ChildID + 1 }
				group ch.ch.ParentID by ch.n into g
				select new
				{
					g.Key,
					Sum = g.Sum(_ => _)
				}));
		}

		[Test]
		public void SubQuery4()
		{
			ForEachProvider(db => AreEqual(
				from ch in Child
				group ch by new { n = ch.ChildID + 1 } into g
				select new
				{
					g.Key,
					Sum = g.Sum(_ => _.ParentID)
				},
				from ch in db.Child
				group ch by new { n = ch.ChildID + 1 } into g
				select new
				{
					g.Key,
					Sum = g.Sum(_ => _.ParentID)
				}));
		}

		[Test]
		public void SubQuery5()
		{
			ForEachProvider(db => AreEqual(
				from ch in Child
				join p in Parent on ch.ParentID equals p.ParentID into pg
				from p in pg.DefaultIfEmpty()
				group ch by ch.ChildID into g
				select g.Sum(_ => _.ParentID),
				from ch in db.Child
				join p in db.Parent on ch.ParentID equals p.ParentID into pg
				from p in pg.DefaultIfEmpty()
				group ch by ch.ChildID into g
				select g.Sum(_ => _.ParentID)));
		}

		[Test]
		public void SubQuery6()
		{
			var expected =
				from ch in Child select new { ParentID = ch.ParentID + 1 } into ch
				group ch.ParentID by ch into g
				select g.Key;

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child select new { ParentID = ch.ParentID + 1 } into ch
				group ch.ParentID by ch into g
				select g.Key));
		}

		[Test]
		public void SubQuery7()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				join c in 
					from c in Child
					where c.ParentID == 1
					select c
				on p.ParentID equals c.ParentID into g
				from c in g.DefaultIfEmpty()
				group p by c == null ? 0 : c.ChildID into gg
				select new { gg.Key },
				from p in db.Parent
				join c in 
					from c in db.Child
					where c.ParentID == 1
					select c
				on p.ParentID equals c.ParentID into g
				from c in g.DefaultIfEmpty()
				group p by c.ChildID into gg
				select new { gg.Key }));
		}

		[Test]
		public void Calculated1()
		{
			var expected = 
				(
					from ch in Child
					group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
					into g select g
				).ToList().OrderBy(p => p.Key).ToList();

			ForEachProvider(db =>
			{
				var result =
					(
						from ch in db.Child
						group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
						into g select g
					).ToList().OrderBy(p => p.Key).ToList();

				AreEqual(expected[0], result[0]);
				AreEqual(expected.Select(p => p.Key), result.Select(p => p.Key));
			});
		}

		[Test]
		public void Calculated2()
		{
			var expected =
				from p in
					from ch in
						from ch in Child
						group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
						into g select g
					select ch.Key + "2"
				where p == "22"
				select p;

			ForEachProvider(db => AreEqual(expected,
				from p in
					from ch in
						from ch in db.Child
						group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
						into g select g
					select ch.Key + "2"
				where p == "22"
				select p));
		}

		[Test]
		public void GroupBy1()
		{
			ForEachProvider(db => AreEqual(
				   Child.GroupBy(ch => ch.ParentID).GroupBy(ch => ch).GroupBy(ch => ch).Select(p => p.Key.Key.Key),
				db.Child.GroupBy(ch => ch.ParentID).GroupBy(ch => ch).GroupBy(ch => ch).Select(p => p.Key.Key.Key)));
		}

		[Test]
		public void GroupBy2()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				join c in Child on p.ParentID equals c.ParentID
				group p by new
				{
					ID = p.Value1 ?? c.ChildID
				} into gr
				select new
				{
					gr.Key.ID,
					ID1 = gr.Key.ID + 1,
				},
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				group p by new
				{
					ID = p.Value1 ?? c.ChildID
				} into gr
				select new
				{
					gr.Key.ID,
					ID1 = gr.Key.ID + 1,
				}));
		}

		[Test]
		public void GroupBy3()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				join c in Child on p.ParentID equals c.ParentID
				group p by p.Value1 ?? c.ChildID into gr
				select new
				{
					gr.Key
				},
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				group p by p.Value1 ?? c.ChildID into gr
				select new
				{
					gr.Key
				}));
		}

		[Test]
		public void Sum1()
		{
			var expected =
				from ch in Child
				group ch by ch.ParentID into g
				select g.Sum(p => p.ChildID);

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child
				group ch by ch.ParentID into g
				select g.Sum(p => p.ChildID)));
		}

		[Test]
		public void Sum2()
		{
			var expected =
				from ch in Child
				group ch by ch.ParentID into g
				select new { Sum = g.Sum(p => p.ChildID) };

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child
				group ch by ch.ParentID into g
				select new { Sum = g.Sum(p => p.ChildID) }));
		}

		[Test]
		public void Sum3()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe },
				db => AreEqual(
					from ch in Child
					group ch by ch.Parent into g
					select g.Key.Children.Sum(p => p.ChildID),
					from ch in db.Child
					group ch by ch.Parent into g
					select g.Key.Children.Sum(p => p.ChildID)));
		}

		[Test]
		public void SumSubQuery1()
		{
			var n = 1;

			var expected =
				from ch in
					from ch in Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
				where ch.ParentID + 1 > n group ch by ch into g
				select g.Sum(p => p.ParentID - 3);

			ForEachProvider(db => AreEqual(expected,
				from ch in
					from ch in db.Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
				where ch.ParentID + 1 > n group ch by ch into g
				select g.Sum(p => p.ParentID - 3)));
		}

		[Test]
		public void GroupByMax()
		{
			ForEachProvider(db => AreEqual(
				from ch in    Child group ch.ParentID by ch.ChildID into g select new { Max = g.Max() },
				from ch in db.Child group ch.ParentID by ch.ChildID into g select new { Max = g.Max() }));
		}

		[Test]
		public void Aggregates1()
		{
			ForEachProvider(db => AreEqual(
				from  ch in Child
				group ch by ch.ParentID into g
				select new
				{
					Cnt = g.Count(),
					Sum = g.Sum(c => c.ChildID),
					Min = g.Min(c => c.ChildID),
					Max = g.Max(c => c.ChildID),
					Avg = (int)g.Average(c => c.ChildID),
				},
				from  ch in db.Child
				group ch by ch.ParentID into g
				select new
				{
					Cnt = g.Count(),
					Sum = g.Sum(c => c.ChildID),
					Min = g.Min(c => c.ChildID),
					Max = g.Max(c => c.ChildID),
					Avg = (int)g.Average(c => c.ChildID),
				}));
		}

		[Test]
		public void Aggregates2()
		{
			ForEachProvider(db => AreEqual(
				from  ch in Child
				group ch by ch.ParentID into g
				select new
				{
					Sum = g.Select(c => c.ChildID).Sum(),
					Min = g.Select(c => c.ChildID).Min(),
					Max = g.Select(c => c.ChildID).Max(),
					Avg = (int)g.Select(c => c.ChildID).Average(),
					Cnt = g.Count()
				},
				from  ch in db.Child
				group ch by ch.ParentID into g
				select new
				{
					Sum = g.Select(c => c.ChildID).Sum(),
					Min = g.Select(c => c.ChildID).Min(),
					Max = g.Select(c => c.ChildID).Max(),
					Avg = (int)g.Select(c => c.ChildID).Average(),
					Cnt = g.Count()
				}));
		}

		[Test]
		public void Aggregates3()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe },
				db => AreEqual(
					from  ch in Child
					where ch.ChildID > 30
					group ch by ch.ParentID into g
					select new
					{
						Sum =      g.Select(c => c.ChildID).Where(_ => _ > 30).Sum(),
						Min =      g.Select(c => c.ChildID).Where(_ => _ > 30).Min(),
						Max =      g.Select(c => c.ChildID).Where(_ => _ > 30).Max(),
						Avg = (int)g.Select(c => c.ChildID).Where(_ => _ > 30).Average(),
					},
					from  ch in db.Child
					where ch.ChildID > 30
					group ch by ch.ParentID into g
					select new
					{
						Sum =      g.Select(c => c.ChildID).Where(_ => _ > 30).Sum(),
						Min =      g.Select(c => c.ChildID).Where(_ => _ > 30).Min(),
						Max =      g.Select(c => c.ChildID).Where(_ => _ > 30).Max(),
						Avg = (int)g.Select(c => c.ChildID).Where(_ => _ > 30).Average(),
					}));
		}

		[Test]
		public void Aggregates4()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe },
				db => AreEqual(
					from  ch in Child
					group ch by ch.ParentID into g
					select new
					{
						Count = g.Count(_ => _.ChildID > 30),
						Sum   = g.Where(_ => _.ChildID > 30).Sum(c => c.ChildID),
					},
					from  ch in db.Child
					group ch by ch.ParentID into g
					select new
					{
						Count = g.Count(_ => _.ChildID > 30),
						Sum   = g.Where(_ => _.ChildID > 30).Sum(c => c.ChildID),
					}));
		}

		[Test]
		public void SelectMax()
		{
			var expected =
				from ch in Child
				group ch by ch.ParentID into g
				select g.Max(c => c.ChildID);

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child
				group ch by ch.ParentID into g
				select g.Max(c => c.ChildID)));
		}

		[Test]
		public void JoinMax()
		{
			var expected =
				from ch in Child
					join max in
						from ch in Child
						group ch by ch.ParentID into g
						select g.Max(c => c.ChildID)
					on ch.ChildID equals max
				select ch;

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child
					join max in
						from ch in db.Child
						group ch by ch.ParentID into g
						select g.Max(c => c.ChildID)
					on ch.ChildID equals max
				select ch));
		}

		[Test]
		public void Min1()
		{
			var expected = Child.Min(c => c.ChildID);
			ForEachProvider(db => Assert.AreEqual(expected, db.Child.Min(c => c.ChildID)));
		}

		[Test]
		public void Min2()
		{
			var expected = Child.Select(c => c.ChildID).Min();
			ForEachProvider(db => Assert.AreEqual(expected, db.Child.Select(c => c.ChildID).Min()));
		}

		[Test]
		public void Max1()
		{
			var expected = Child.Max(c => c.ChildID);
			Assert.AreNotEqual(0, expected);
			ForEachProvider(db => Assert.AreEqual(expected, db.Child.Max(c => c.ChildID)));
		}

		[Test]
		public void Max11()
		{
			ForEachProvider(db => Assert.AreEqual(
				   Child.Max(c => c.ChildID > 20),
				db.Child.Max(c => c.ChildID > 20)));
		}

		[Test]
		public void Max12()
		{
			ForEachProvider(db => Assert.AreEqual(
				   Child.Max(c => (bool?)(c.ChildID > 20)),
				db.Child.Max(c => (bool?)(c.ChildID > 20))));
		}

		[Test]
		public void Max2()
		{
			var expected =
				from p in Parent
					join c in Child on p.ParentID equals c.ParentID
				where c.ChildID > 20
				select p;

			ForEachProvider(db =>
			{
				var result =
					from p in db.Parent
						join c in db.Child on p.ParentID equals c.ParentID
					where c.ChildID > 20
					select p;

				Assert.AreEqual(expected.Max(p => p.ParentID), result.Max(p => p.ParentID));
			});
		}

		[Test]
		public void Max3()
		{
			ForEachProvider(db => Assert.AreEqual(
				Child.Select(c => c.ChildID).Max(),
				db.Child.Select(c => c.ChildID).Max()));
		}

		[Test]
		public void Max4()
		{
			ForEachProvider(db => Assert.AreEqual(
				from t1 in Types
				join t2 in
					from sub in Types
					where
						sub.ID == 1 &&
						sub.DateTimeValue <= DateTime.Today
					group sub by new
					{
						sub.ID
					} into g
					select new
					{
						g.Key.ID,
						DateTimeValue = g.Max( p => p.DateTimeValue )
					}
				on new { t1.ID, t1.DateTimeValue } equals new { t2.ID, t2.DateTimeValue }
				select t1.MoneyValue,
				from t1 in db.Types
				join t2 in
					from sub in db.Types
					where
						sub.ID == 1 &&
						sub.DateTimeValue <= DateTime.Today
					group sub by new
					{
						sub.ID
					} into g
					select new
					{
						g.Key.ID,
						DateTimeValue = g.Max( p => p.DateTimeValue )
					}
				on new { t1.ID, t1.DateTimeValue } equals new { t2.ID, t2.DateTimeValue }
				select t1.MoneyValue
				));
		}

		[Test]
		public void Average1()
		{
			ForEachProvider(db => Assert.AreEqual(
				(int)db.Child.Average(c => c.ChildID),
				(int)   Child.Average(c => c.ChildID)));
		}

		[Test]
		public void Average2()
		{
			var expected = Child.Select(c => c.ChildID).Average();
			ForEachProvider(db => Assert.AreEqual((int)expected, (int)db.Child.Select(c => c.ChildID).Average()));
		}

		[Test]
		public void GrooupByAssociation1()
		{
			ForEachProvider(db => AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent into g
				where g.Count() > 2
				select g.Key.Value1
				,
				from ch in db.GrandChild1
				group ch by ch.Parent into g
				where g.Count() > 2
				select g.Key.Value1));
		}

		[Test]
		public void GrooupByAssociation101()
		{
			ForEachProvider(db => AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent into g
				where g.Max(_ => _.ParentID) > 2
				select g.Key.Value1
				,
				from ch in db.GrandChild1
				group ch by ch.Parent into g
				where g.Max(_ => _.ParentID) > 2
				select g.Key.Value1));
		}

		[Test]
		public void GrooupByAssociation102()
		{
			ForEachProvider(
				new[] { ProviderName.Informix },
				db => AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where g.Count(_ => _.ChildID >= 20) > 2
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where g.Count(_ => _.ChildID >= 20) > 2
					select g.Key.Value1));
		}

		[Test]
		public void GrooupByAssociation1022()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe, ProviderName.Access, ProviderName.Informix }, // Can be fixed.
					db => AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where g.Count(_ => _.ChildID >= 20) > 2 && g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where g.Count(_ => _.ChildID >= 20) > 2 && g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0
					select g.Key.Value1));
		}

		[Test]
		public void GrooupByAssociation1023()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe, ProviderName.Access, ProviderName.Informix }, // Can be fixed.
				db => AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where
						g.Count(_ => _.ChildID >= 20) > 2 &&
						g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where
						g.Count(_ => _.ChildID >= 20) > 2 &&
						g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0
					select g.Key.Value1));
		}

		[Test]
		public void GrooupByAssociation1024()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe, ProviderName.Access, ProviderName.Informix }, // Can be fixed.
				db => AreEqual(
					from ch in GrandChild1
					group ch by ch.Parent into g
					where
						g.Count(_ => _.ChildID >= 20) > 2 &&
						g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 18).Max(p => p.ParentID) > 0
					select g.Key.Value1
					,
					from ch in db.GrandChild1
					group ch by ch.Parent into g
					where
						g.Count(_ => _.ChildID >= 20) > 2 &&
						g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0 &&
						g.Where(_ => _.ChildID >= 18).Max(p => p.ParentID) > 0
					select g.Key.Value1));
		}

		[Test]
		public void GrooupByAssociation2()
		{
			ForEachProvider(db => AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent into g
				where g.Count() > 2 && g.Key.ParentID != 1
				select g.Key.Value1
				,
				from ch in db.GrandChild1
				group ch by ch.Parent into g
				where g.Count() > 2 && g.Key.ParentID != 1
				select g.Key.Value1));
		}

		[Test]
		public void GrooupByAssociation3()
		{
			using (var db = new NorthwindDB())
			{
				var result = 
					from p in db.Product
					group p by p.Category into g
					where g.Count() == 12
					select g.Key.CategoryName;

				var list = result.ToList();
				Assert.AreEqual(3, list.Count);
			}
		}

		[Test]
		public void GrooupByAssociation4()
		{
			using (var db = new NorthwindDB())
			{
				var result = 
					from p in db.Product
					group p by p.Category into g
					where g.Count() == 12
					select g.Key.CategoryID;

				var list = result.ToList();
				Assert.AreEqual(3, list.Count);
			}
		}

		[Test]
		public void GroupByAggregate1()
		{
			var expected =
				from p in Parent
				group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key;

			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(expected,
				from p in db.Parent
				group p by p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key));
		}

		[Test]
		public void GroupByAggregate11()
		{
			var expected =
				from p in Parent
				where p.Children.Count > 0
				group p by p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key;

			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(expected,
				from p in db.Parent
				where p.Children.Count > 0
				group p by p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key));
		}

		[Test]
		public void GroupByAggregate12()
		{
			var expected =
				from p in Parent
				group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key;

			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(expected,
				from p in db.Parent
				group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key));
		}

		[Test]
		public void GroupByAggregate2()
		{
			using (var db = new NorthwindDB())
				AreEqual(
					(
						from c in Customer
						group c by c.Orders.Count > 0 && c.Orders.Average(o => o.Freight) >= 80
					).ToList().Select(k => k.Key),
					(
						from c in db.Customer
						group c by c.Orders.Average(o => o.Freight) >= 80
					).ToList().Select(k => k.Key));
		}

		[Test]
		public void GroupByAggregate3()
		{
			var expected =
				(
					from p in Parent
					group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3
				).ToList().First(g => !g.Key);

			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(expected,
				(
					from p in db.Parent
					group p by p.Children.Average(c => c.ParentID) > 3
				).ToList().First(g => !g.Key)));
		}

		[Test]
		public void ByJoin()
		{
			ForEachProvider(db => AreEqual(
				from c1 in Child
				join c2 in Child on c1.ChildID equals c2.ChildID + 1
				group c2 by c1.ParentID into g
				select g.Sum(_ => _.ChildID),
				from c1 in db.Child
				join c2 in db.Child on c1.ChildID equals c2.ChildID + 1
				group c2 by c1.ParentID into g
				select g.Sum(_ => _.ChildID)));
		}

		[Test]
		public void SelectMany()
		{
			ForEachProvider(db => AreEqual(
				   Child.GroupBy(ch => ch.ParentID).SelectMany(g => g),
				db.Child.GroupBy(ch => ch.ParentID).SelectMany(g => g)));
		}

		[Test]
		public void Scalar1()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Max()),
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Max())));
		}

		[Test]
		public void Scalar101()
		{
			ForEachProvider(db => AreEqual(
				(from ch in Child
				 select ch.ChildID into id
				 group id by id into g
				 select g.Max()),
				(from ch in db.Child
				 select ch.ChildID into id
				 group id by id into g
				 select g.Max())));
		}

		[Test]
		public void Scalar2()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select new
					 {
						 Max1 = g.Select(ch => ch.ChildID              ).Max(),
						 Max2 = g.Select(ch => ch.ChildID + ch.ParentID).Max()
					 }),
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select new
					 {
						 Max1 = g.Select(ch => ch.ChildID              ).Max(),
						 Max2 = g.Select(ch => ch.ChildID + ch.ParentID).Max()
					 })));
		}

		[Test]
		public void Scalar3()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe },
				db => AreEqual(
					(from ch in Child
					 group ch by ch.ParentID into g
					 select g.Select(ch => ch.ChildID).Where(id => id > 0).Max()),
					(from ch in db.Child
					 group ch by ch.ParentID into g
					 select g.Select(ch => ch.ChildID).Where(id => id > 0).Max())));
		}

		[Test]
		public void Scalar4()
		{
			ForEachProvider(
				new[] { ProviderName.SqlCe, ProviderName.Access, ProviderName.Informix },
				db => AreEqual(
					from ch in Child
					group ch by ch.ParentID into g
					where g.Where(ch => ch.ParentID > 2).Select(ch => (int?)ch.ChildID).Min() != null
					select g.Where(ch => ch.ParentID > 2).Select(ch => ch.ChildID).Min()
					,
					from ch in db.Child
					group ch by ch.ParentID into g
					where g.Where(ch => ch.ParentID > 2).Select(ch => (int?)ch.ChildID).Min() != null
					select g.Where(ch => ch.ParentID > 2).Select(ch => ch.ChildID).Min()));
		}

		[Test]
		public void Scalar5()
		{
			ForEachProvider(db => AreEqual(
				from ch in Child
				select ch.ParentID into id
				group id by id into g
				select g.Max()
				,
				from ch in db.Child
				select ch.ParentID into id
				group id by id into g
				select g.Max()));
		}

		//[Test]
		public void Scalar51()
		{
			ForEachProvider(db => AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select g.Max()
				,
				from ch in db.Child
				group ch by ch.ParentID into g
				select g.Max()));
		}

		[Test]
		public void Scalar6()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				(from ch in Child
				 where ch.ParentID < 3
				 group ch by ch.ParentID into g
				 select g.Where(ch => ch.ParentID < 3).Max(ch => ch.ChildID)),
				(from ch in db.Child
				 where ch.ParentID < 3
				 group ch by ch.ParentID into g
				 select g.Where(ch => ch.ParentID < 3).Max(ch => ch.ChildID))));
		}

		[Test]
		public void Scalar7()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select new { max = g.Select(ch => ch.ChildID).Max()}).Select(id => id.max),
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select new { max = g.Select(ch => ch.ChildID).Max()}).Select(id => id.max)));
		}

		[Test]
		public void Scalar8()
		{
			ForEachProvider(db => AreEqual(
				(from ch in Child
				group ch by ch.ParentID into g
				select new { max = g.Max(ch => ch.ChildID)}).Select(id => id.max),
				(from ch in db.Child
				group ch by ch.ParentID into g
				select new { max = g.Max(ch => ch.ChildID)}).Select(id => id.max)));
		}

		[Test]
		public void Scalar9()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count()),
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count())));
		}

		[Test]
		public void Scalar10()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count(id => id >= 20)),
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count(id => id >= 20))));
		}

		[Test]
		public void GroupByExtraFieldBugTest()
		{
			// https://github.com/igor-tkachev/LinqToDB/issues/42
			// extra field is generated in the GROUP BY clause, for example:
			// GROUP BY p.LastName, p.LastName <--- the second one is redundant

			using (var db = new TestDbManager("MySql"))
			{
				var q =
					from d in db.Doctor
					join p in db.Person on d.PersonID equals p.ID
					group d by p.LastName into g
					select g.Key;

				q.ToList();

				const string fieldName = "LastName";

				var lastQuery  = db.LastQuery;
				var groupByPos = lastQuery.IndexOf("GROUP BY");
				var fieldPos   = lastQuery.IndexOf(fieldName, groupByPos);
				
				// check that our field does not present in the GROUP BY clause second time
				Assert.AreEqual(-1, lastQuery.IndexOf(fieldName, fieldPos + 1));
			}
		}
	}
}
