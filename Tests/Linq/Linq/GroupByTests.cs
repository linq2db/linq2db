using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class GroupByTests : TestBase
	{
		[Test]
		public void Simple1([DataSources] string context)
		{
			using var db = GetDataContext(context, options => options.UseGuardGrouping(false));
			db.BeginTransaction();

			var q =
				from ch in db.Child
				group ch by ch.ParentID;

			var list = q.ToList().Where(n => n.Key < 6).OrderBy(n => n.Key).ToList();

			Assert.That(list, Has.Count.EqualTo(4));

			for (var i = 0; i < list.Count; i++)
			{
				var values = list[i].OrderBy(c => c.ChildID).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list[i].Key, Is.EqualTo(i + 1));
					Assert.That(values, Has.Count.EqualTo(i + 1));
				}

				for (var j = 0; j < values.Count; j++)
					Assert.That(values[j].ChildID, Is.EqualTo((i + 1) * 10 + j + 1));
			}
		}

		[Test]
		public void Simple2([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context).UseGuardGrouping(false));
			var q =
				from ch in db.GrandChild
				group ch by new { ch.ParentID, ch.ChildID };

			var list = q.ToList();

			Assert.That(list, Has.Count.EqualTo(8));
			Assert.That(list.OrderBy(c => c.Key.ParentID).First().ToList(), Is.Not.Empty);
		}

		[Test]
		public void Simple3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q =
				from ch in db.Child
				group ch by ch.ParentID into g
				select g.Key;

			var list = q.ToList().Where(n => n < 6).OrderBy(n => n).ToList();

			Assert.That(list, Has.Count.EqualTo(4));
			for (var i = 0; i < list.Count; i++) Assert.That(list[i], Is.EqualTo(i + 1));
		}

		[Test]
		public void Simple4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q =
				from ch in db.Child
				group ch by ch.ParentID into g
				orderby g.Key
				select g.Key;

			var list = q.ToList().Where(n => n < 6).ToList();

			Assert.That(list, Has.Count.EqualTo(4));
			for (var i = 0; i < list.Count; i++) Assert.That(list[i], Is.EqualTo(i + 1));
		}

		[Test]
		public void Simple5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in GrandChild
				group ch by new { ch.ParentID, ch.ChildID } into g
				group g by new { g.Key.ParentID } into g
				select g.Key,

				from ch in db.GrandChild
				group ch by new { ch.ParentID, ch.ChildID } into g
				group g by new { g.Key.ParentID } into g
				select g.Key
			);
		}

		[Test]
		public void Simple6([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context).UseGuardGrouping(false));
			var q    = db.GrandChild.GroupBy(ch => new { ch.ParentID, ch.ChildID }, ch => ch.GrandChildID);
			var list = q.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(list[0].Count(), Is.Not.Zero);
				Assert.That(list, Has.Count.EqualTo(8));
			}
		}

		[Test]
		public void Simple7([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = db.GrandChild
				.GroupBy(ch => new { ch.ParentID, ch.ChildID }, ch => ch.GrandChildID)
				.Select (gr => new { gr.Key.ParentID, gr.Key.ChildID });

			var list = q.ToList();
			Assert.That(list, Has.Count.EqualTo(8));
		}

		[Test]
		public void Simple8([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));
			var q = db.GrandChild.GroupBy(ch => new { ch.ParentID, ch.ChildID }, (g,ch) => g.ChildID);

			var list = q.ToList();
			Assert.That(list, Has.Count.EqualTo(8));
		}

		[Test]
		public void Simple9([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));
			var q    = db.GrandChild.GroupBy(ch => new { ch.ParentID, ch.ChildID }, ch => ch.GrandChildID,  (g,ch) => g.ChildID);
			var list = q.ToList();

			Assert.That(list, Has.Count.EqualTo(8));
		}

		[Test]
		public void Simple10([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));
			var expected = (from ch in    Child group ch by ch.ParentID into g select g).ToList().OrderBy(p => p.Key).ToList();
			var result   = (from ch in db.Child group ch by ch.ParentID into g select g).ToList().OrderBy(p => p.Key).ToList();

			AreEqual(expected[0], result[0]);
			AreEqual(expected.Select(p => p.Key), result.Select(p => p.Key));
			AreEqual(expected[0].ToList(), result[0].ToList());
		}

		[Test]
		public void Simple11([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context).UseGuardGrouping(false));
			var q1 = GrandChild
				.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, ch => ch.ChildID);

			var q2 = db.GrandChild
				.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, ch => ch.ChildID);

			//var list1 = q1.AsEnumerable().OrderBy(_ => _.Key.ChildID).ToList();
			var list2 = q2.AsEnumerable().OrderBy(_ => _.Key.ChildID).ToList();

			// Assert.AreEqual(list1.Count,       list2.Count);
			// Assert.AreEqual(list1[0].ToList(), list2[0].ToList());
		}

		[Test]
		public void Simple12([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));
			var q = db.GrandChild
					.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, (g,ch) => g.ChildID);

			var list = q.ToList();
			Assert.That(list, Has.Count.EqualTo(8));
		}

		[Test]
		public void Simple13([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));
			var q = db.GrandChild
				.GroupBy(ch => new { ParentID = ch.ParentID + 1, ch.ChildID }, ch => ch.ChildID, (g,ch) => g.ChildID);

			var list = q.ToList();
			Assert.That(list, Has.Count.EqualTo(8));
		}

		[Test]
		public void Simple14([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				select
					from c in p.Children
					group c by c.ParentID into g
					select g.Key,

				from p in db.Parent
				select
					from c in p.Children
					group c by c.ParentID into g
					select g.Key
			);
		}

		[Test]
		public void MemberInit1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by new Child { ParentID = ch.ParentID } into g
				select g.Key,

				from ch in db.Child
				group ch by new Child { ParentID = ch.ParentID } into g
				select g.Key
			);
		}

		sealed class GroupByInfo
		{
			public GroupByInfo? Prev;
			public object?      Field;

			public override bool Equals(object? obj)
			{
				return Equals(obj as GroupByInfo);
			}

			public bool Equals(GroupByInfo? other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return Equals(other.Prev, Prev) && Equals(other.Field, Field);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Prev, Field);
			}
		}

		[Test]
		public void MemberInit2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by new GroupByInfo { Prev = new GroupByInfo { Field = ch.ParentID }, Field = ch.ChildID } into g
				select g.Key,

				from ch in db.Child
				group ch by new GroupByInfo { Prev = new GroupByInfo { Field = ch.ParentID }, Field = ch.ChildID } into g
				select g.Key, q => q.OrderBy(x => x.Prev!.Field).ThenBy(x => x.Field)
			);
		}

		[Test]
		public void MemberInit3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by new { Prev = new { Field = ch.ParentID }, Field = ch.ChildID } into g
				select g.Key,

				from ch in db.Child
				group ch by new { Prev = new { Field = ch.ParentID }, Field = ch.ChildID } into g
				select g.Key
			);
		}

		[Test]
		public void SubQuery1([DataSources] string context)
		{
			var n = 1;

			using var db = GetDataContext(context);
			AreEqual(
				from ch in
					from ch in Child select ch.ParentID + 1
				where ch + 1 > n
				group ch by ch into g
				select g.Key,

				from ch in
					from ch in db.Child select ch.ParentID + 1
				where ch > n
				group ch by ch into g
				select g.Key
			);
		}

		[Test]
		public void SubQuery2([DataSources] string context)
		{
			var n = 1;

			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				select new { ParentID = ch.ParentID + 1 } into ch
				where ch.ParentID > n
				group ch by ch into g
				select g.Key,

				from ch in db.Child
				select new { ParentID = ch.ParentID + 1 } into ch
				where ch.ParentID > n
				group ch by ch into g
				select g.Key
			);
		}

		[Test]
		public void SubQuery3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
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
				}
			);
		}

		[Test]
		public void SubQuery31([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
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
				}
			);
		}

		[Test]
		public void SubQuery32([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
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
				}
			);
		}

		[Test]
		public void SubQuery4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
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
				}
			);
		}

		[Test]
		public void SubQuery5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				join p in Parent on ch.ParentID equals p.ParentID into pg
				from p in pg.DefaultIfEmpty()
				group ch by ch.ChildID into g
				select g.Sum(_ => _.ParentID),

				from ch in db.Child
				join p in db.Parent on ch.ParentID equals p.ParentID into pg
				from p in pg.DefaultIfEmpty()
				group ch by ch.ChildID into g
				select g.Sum(_ => _.ParentID)
			);
		}

		[Test]
		public void SubQuery6([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				select new { ParentID = ch.ParentID + 1 } into ch
				group ch.ParentID by ch into g
				select g.Key,

				from ch in db.Child
				select new { ParentID = ch.ParentID + 1 } into ch
				group ch.ParentID by ch into g
				select g.Key
			);
		}

		[Test]
		public void SubQuery7([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
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
				select new { gg.Key }
			);
		}

		[Test]
		public void Calculated1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));
			db.BeginTransaction();

			var result =
				(
					from ch in db.Child
					group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
					into g
					select g
				).ToList().OrderBy(p => p.Key).ToList();

			var expected =
				(
					from ch in Child
					group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
					into g
					select g
				).ToList().OrderBy(p => p.Key).ToList();

			AreEqual(expected[0], result[0]);
			AreEqual(expected.Select(p => p.Key), result.Select(p => p.Key));
		}

		[Test]
		public void Calculated2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in
					from ch in
						from ch in Child
						group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
					into g
						select g
					select ch.Key + "2"
				where p == "22"
				select p,

				from p in
					from ch in
						from ch in db.Child
						group ch by ch.ParentID > 2 ? ch.ParentID > 3 ? "1" : "2" : "3"
					into g
						select g
					select ch.Key + "2"
				where p == "22"
				select p
			);
		}

		[Test]
		public void GroupBy1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.GroupBy(ch => ch.ParentID).GroupBy(ch => ch).GroupBy(ch => ch).Select(p => p.Key.Key.Key),
				db.Child.GroupBy(ch => ch.ParentID).GroupBy(ch => ch).GroupBy(ch => ch).Select(p => p.Key.Key.Key)
			);
		}

		[Test]
		public void GroupBy2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
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
				}
			);
		}

		[Test]
		public void GroupBy3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
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
				}
			);
		}

		[Test]
		public void Sum1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select g.Sum(p => p.ChildID),

				from ch in db.Child
				group ch by ch.ParentID into g
				select g.Sum(p => p.ChildID)
			);
		}

		[Test]
		public void Sum2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select new { Sum = g.Sum(p => p.ChildID) },

				from ch in db.Child
				group ch by ch.ParentID into g
				select new { Sum = g.Sum(p => p.ChildID) }
			);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void Sum3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by ch.Parent
				into g
				select g.Key.Children.Sum(p => p.ChildID),

				from ch in db.Child
				group ch by ch.Parent
				into g
				select g.Key.Children.Sum(p => p.ChildID)
			);
		}

		[Test]
		public void SumSubQuery1([DataSources] string context)
		{
			var n = 1;

			using var db = GetDataContext(context);
			AreEqual(
				from ch in
					from ch in Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
				where ch.ParentID + 1 > n
				group ch by ch into g
				select g.Sum(p => p.ParentID - 3),

				from ch in
					from ch in db.Child select new { ParentID = ch.ParentID + 1, ch.ChildID }
				where ch.ParentID + 1 > n
				group ch by ch into g
				select g.Sum(p => p.ParentID - 3)
			);
		}

		[Test]
		public void GroupByMax([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child group ch.ParentID by ch.ChildID into g select new { Max = g.Max() },
				from ch in db.Child group ch.ParentID by ch.ChildID into g select new { Max = g.Max() }
			);
		}

		[Test]
		public void Aggregates1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select new
				{
					Cnt = g.Count(),
					Sum = g.Sum(c => c.ChildID),
					Min = g.Min(c => c.ChildID),
					Max = g.Max(c => c.ChildID),
					Avg = (int)g.Average(c => c.ChildID),
				},

				from ch in db.Child
				group ch by ch.ParentID into g
				select new
				{
					Cnt = g.Count(),
					Sum = g.Sum(c => c.ChildID),
					Min = g.Min(c => c.ChildID),
					Max = g.Max(c => c.ChildID),
					Avg = (int)g.Average(c => c.ChildID),
				}
			);
		}

		[Test]
		public void Aggregates2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select new
				{
					Sum = g.Select(c => c.ChildID).Sum(),
					Min = g.Select(c => c.ChildID).Min(),
					Max = g.Select(c => c.ChildID).Max(),
					Avg = (int)g.Select(c => c.ChildID).Average(),
					Cnt = g.Count()
				},
				from ch in db.Child
				group ch by ch.ParentID into g
				select new
				{
					Sum = g.Select(c => c.ChildID).Sum(),
					Min = g.Select(c => c.ChildID).Min(),
					Max = g.Select(c => c.ChildID).Max(),
					Avg = (int)g.Select(c => c.ChildID).Average(),
					Cnt = g.Count()
				}
			);
		}

		[Test]
		public void Aggregates3([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				where ch.ChildID > 30
				group ch by ch.ParentID into g
				select new
				{
					Cnt = g.Select(c => c.ChildID).Where(_ => _ > 30).Count(),
					Sum = g.Select(c => c.ChildID).Where(_ => _ > 30).Sum(),
					Min = g.Select(c => c.ChildID).Where(_ => _ > 30).Min(),
					Max = g.Select(c => c.ChildID).Where(_ => _ > 30).Max(),
					Avg = (int)g.Select(c => c.ChildID).Where(_ => _ > 30).Average(),
				},
				from ch in db.Child
				where ch.ChildID > 30
				group ch by ch.ParentID into g
				select new
				{
					Cnt = g.Select(c => c.ChildID).Where(_ => _ > 30).Count(),
					Sum = g.Select(c => c.ChildID).Where(_ => _ > 30).Sum(),
					Min = g.Select(c => c.ChildID).Where(_ => _ > 30).Min(),
					Max = g.Select(c => c.ChildID).Where(_ => _ > 30).Max(),
					Avg = (int)g.Select(c => c.ChildID).Where(_ => _ > 30).Average(),
				}
			);
		}

		[Test]
		public void Aggregates4([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select new
				{
					Count = g.Count(_ => _.ChildID > 30),
					Sum = g.Where(_ => _.ChildID > 30).Sum(c => c.ChildID),
				},
				from ch in db.Child
				group ch by ch.ParentID into g
				select new
				{
					Count = g.Count(_ => _.ChildID > 30),
					Sum = g.Where(_ => _.ChildID > 30).Sum(c => c.ChildID),
				}
			);
		}

		[Test]
		public void Aggregates5([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select new
				{
					Count1 = g.Count(c => c.ChildID > 30),
					Count2 = g.Select(c => c.ChildID).Where(_ => _ > 30).Count(),
					Count3 = g.Count()
				},
				from ch in db.Child
				group ch by ch.ParentID into g
				select new
				{
					Count1 = g.Count(c => c.ChildID > 30),
					Count2 = g.Select(c => c.ChildID).Where(_ => _ > 30).Count(),
					Count3 = g.Count()
				}
			);
		}

		class AggregationData
		{
			[PrimaryKey] public int Id { get; set; }
			public int GroupId { get; set; }
			public double? DataValue { get; set; }

			public static AggregationData[] Data = new[]
			{
				new AggregationData { Id = 1, GroupId = 1, DataValue = 1 },
				new AggregationData { Id = 2, GroupId = 1, DataValue = null },
				new AggregationData { Id = 3, GroupId = 1, DataValue = 3 },
				new AggregationData { Id = 4, GroupId = 1, DataValue = 1 },
				new AggregationData { Id = 5, GroupId = 1, DataValue = 5 },
				new AggregationData { Id = 6, GroupId = 1, DataValue = 6 },

				new AggregationData { Id = 7, GroupId = 2, DataValue = 7 },
				new AggregationData { Id = 8, GroupId = 2, DataValue = 8 },
				new AggregationData { Id = 9, GroupId = 2, DataValue = 9 },
				new AggregationData { Id = 10, GroupId = 2, DataValue = null },
				new AggregationData { Id = 11, GroupId = 2, DataValue = 11 },
				new AggregationData { Id = 12, GroupId = 2, DataValue = 7 },

				new AggregationData { Id = 13, GroupId = 3, DataValue = 13 },
				new AggregationData { Id = 14, GroupId = 3, DataValue = 16 },
				new AggregationData { Id = 15, GroupId = 3, DataValue = 16 },
				new AggregationData { Id = 16, GroupId = 3, DataValue = 16 },
				new AggregationData { Id = 17, GroupId = 3, DataValue = null },
				new AggregationData { Id = 18, GroupId = 3, DataValue = 18 },
			};
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void CountInGroup([DataSources] string context)
		{
			var data = AggregationData.Data;

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				where t.DataValue != null
				group t by t.GroupId
				into g
				let filtered = g.Where(x => x.DataValue % 2 == 0)
				let filteredDistinct = filtered.Select(x => x.DataValue).Distinct()
				let nonfilteredDistinct = g.Select(x => x.DataValue).Distinct()
				select new
				{
					GroupId                  = g.Key,
					Simple                   = g.Count(),
					WithFilter               = g.Count(x => x.DataValue % 2 == 0),
					Projection               = g.Select(x => x.DataValue).Count(),
					Distinct                 = g.Select(x => x.DataValue).Distinct().Count(),
					DistinctWithFilter       = g.Select(x => x.DataValue).Distinct().Count(x => x % 2 == 0),
					FilterDistinct           = g.Select(x => x.DataValue).Where(x => x      % 2 == 0).Distinct().Count(),
					FilterDistinctWithFilter = g.Select(x => x.DataValue).Where(x => x      % 2 == 0).Distinct().Count(x => x % 2 == 0),

					SubFilter           = filtered.Count(),
					SubFilterDistinct   = filteredDistinct.Count(),
					SubNoFilterDistinct = nonfilteredDistinct.Count(),
				};

			AssertQuery(query);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void SumInGroup([DataSources] string context)
		{
			var data = AggregationData.Data;

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by new { t.GroupId }  into g
				select new
				{
					GroupId          = g.Key,
					Simple           = g.Sum(x => x.DataValue),
					Projection       = g.Select(x => x.DataValue).Sum(),
					Filter           = g.Where(x => x.DataValue % 2 == 0).Sum(x => x.DataValue),
					FilterProjection = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Sum(),
					Distinct         = g.Select(x=> x.DataValue).Distinct().Sum(),
					FilterDistinct1   = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Distinct().Sum(),
					FilterDistinct2   = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Distinct().Sum(x => x),
				};

			AssertQuery(query);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void MinInGroup([DataSources] string context)
		{
			var data = AggregationData.Data;

			using var db    = GetDataContext(context, o => o.UseGuardGrouping(false));
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by new { t.GroupId }  into g
				select new
				{
					GroupId          = g.Key,
					Simple           = g.Min(x => x.DataValue),
					Projection       = g.Select(x => x.DataValue).Min(),
					Filter           = g.Where(x => x.DataValue % 2 == 0).Min(x => x.DataValue),
					FilterProjection = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Min(),
					Distinct         = g.Select(x=> x.DataValue).Distinct().Min(),
					FilterDistinct1  = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Distinct().Min(),
					FilterDistinct2  = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Distinct().Min(x => x),
				};

			AssertQuery(query);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void MaxInGroup([DataSources] string context)
		{
			var data = AggregationData.Data;

			using var db    = GetDataContext(context, o => o.UseGuardGrouping(false));
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by new { t.GroupId }  into g
				select new
				{
					GroupId          = g.Key,
					Simple           = g.Max(x => x.DataValue),
					Projection       = g.Select(x => x.DataValue).Max(),
					Filter           = g.Where(x => x.DataValue % 2 == 0).Max(x => x.DataValue),
					FilterProjection = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Max(),
					Distinct         = g.Select(x=> x.DataValue).Distinct().Max(),
					FilterDistinct1  = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Distinct().Max(),
					FilterDistinct2  = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Distinct().Max(x => x),
				};

			AssertQuery(query);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void AverageInGroup([DataSources] string context)
		{
			var data = AggregationData.Data;

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				where t.DataValue != null
				group t by t.GroupId into g
				select new
				{
					GroupId          = g.Key,
					Simple           = g.Average(x => x.DataValue),
					Projection       = g.Select(x => x.DataValue).Average(),
					Filter           = g.Where(x => x.DataValue % 2 == 0).Average(x => x.DataValue),
					FilterProjection = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Average(),
					Distinct         = Math.Round((decimal)g.Select(x=> x.DataValue).Distinct().Average()!, 4),
					FilterDistinct1  = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Distinct().Average(),
					FilterDistinct2  = g.Where(x => x.DataValue % 2 == 0).Select(x => x.DataValue).Distinct().Average(x => x),
				};

			AssertQuery(query);
		}

		[Test]
		public void SelectMax([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select g.Max(c => c.ChildID),
				from ch in db.Child
				group ch by ch.ParentID into g
				select g.Max(c => c.ChildID)
			);
		}

		[Test]
		public void JoinMax([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				join max in
						from ch1 in Child
						group ch1 by ch1.ParentID into g
						select g.Max(c => c.ChildID)
					on ch.ChildID equals max
				select ch,
				from ch in db.Child
				join max in
						from ch1 in db.Child
						group ch1 by ch1.ParentID into g
						select g.Max(c => c.ChildID)
					on ch.ChildID equals max
				select ch
			);
		}

		[Test]
		public void Min1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(
				db.Child.Min(c => c.ChildID), Is.EqualTo(Child.Min(c => c.ChildID))
			);
		}

		[Test]
		public void Min2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(
				db.Child.Select(c => c.ChildID).Min(), Is.EqualTo(Child.Select(c => c.ChildID).Min())
			);
		}

		[Test]
		public void Max1([DataSources] string context)
		{
			var expected = Child.Max(c => c.ChildID);
			Assert.That(expected, Is.Not.Zero);

			using var db = GetDataContext(context);
			Assert.That(db.Child.Max(c => c.ChildID), Is.EqualTo(expected));
		}

		[Test]
		public void Max11([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(
				db.Child.Max(c => c.ChildID > 20), Is.EqualTo(Child.Max(c => c.ChildID > 20))
			);
		}

		[Test]
		public void Max12([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(
				db.Child.Max(c => (bool?)(c.ChildID > 20)), Is.EqualTo(Child.Max(c => (bool?)(c.ChildID > 20)))
			);
		}

		[Test]
		public void Max2([DataSources] string context)
		{
			using var db = GetDataContext(context);

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

			Assert.That(result.Max(p => p.ParentID), Is.EqualTo(expected.Max(p => p.ParentID)));
		}

		[Test]
		public void Max3([DataSources] string context)
		{
			using var db = GetDataContext(context);

			Assert.That(
				db.Child.Select(c => c.ChildID).Max(), Is.EqualTo(Child.Select(c => c.ChildID).Max())
			);
		}

		[Test]
		public void Max4([DataSources] string context)
		{
			using var db = GetDataContext(context);

			Assert.That(
					from t1 in db.Types
					join t2 in
						from sub in db.Types
						where
							sub.ID == 1 &&
							sub.DateTimeValue <= TestData.Date
						group sub by new
						{
							sub.ID
						} into g
						select new
						{
							g.Key.ID,
							DateTimeValue = g.Max(p => p.DateTimeValue)
						}
					on new { t1.ID, t1.DateTimeValue } equals new { t2.ID, t2.DateTimeValue }
					select t1.MoneyValue,
				Is.EqualTo(
					from t1 in Types
					join t2 in
						from sub in Types
						where
							sub.ID == 1 &&
							sub.DateTimeValue <= TestData.Date
						group sub by new
						{
							sub.ID
						} into g
						select new
						{
							g.Key.ID,
							DateTimeValue = g.Max(p => p.DateTimeValue)
						}
					on new { t1.ID, t1.DateTimeValue } equals new { t2.ID, t2.DateTimeValue }
					select t1.MoneyValue
				)
			);
		}

		[Test]
		public void Average1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			Assert.That(
				(int)Child.Average(c => c.ChildID), Is.EqualTo((int)db.Child.Average(c => c.ChildID))
			);
		}

		[Test]
		public void Average2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			Assert.That(
				(int)db.Child.Select(c => c.ChildID).Average(), Is.EqualTo((int)Child.Select(c => c.ChildID).Average())
			);
		}

		[Test]
		public void GroupByAssociation1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent into g
				where g.Count() > 2
				select g.Key.Value1,

				from ch in db.GrandChild1
				group ch by ch.Parent into g
				where g.Count() > 2
				select g.Key.Value1
			);
		}

		[Test]
		public void GroupByAssociation101([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent into g
				where g.Max(_ => _.ParentID) > 2
				select g.Key.Value1,

				from ch in db.GrandChild1
				group ch by ch.Parent into g
				where g.Max(_ => _.ParentID) > 2
				select g.Key.Value1
			);
		}

		[Test]
		public void GroupByAssociation102([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent
				into g
				where g.Count(_ => _.ChildID >= 20) > 2
				select g.Key.Value1,

				from ch in db.GrandChild1
				group ch by ch.Parent
				into g
				where g.Count(_ => _.ChildID >= 20) > 2
				select g.Key.Value1
			);
		}

		[Test]
		public void GroupByAssociation1022([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent into g
				where g.Count(_ => _.ChildID >= 20) > 2 && g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0
				select g.Key.Value1,

				from ch in db.GrandChild1
				group ch by ch.Parent into g
				where g.Count(_ => _.ChildID >= 20) > 2 && g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0
				select g.Key.Value1
			);
		}

		[Test]
		public void GroupByAssociation1023([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent into g
				where
					g.Count(_ => _.ChildID >= 20) > 2 &&
					g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
					g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0
				select g.Key.Value1,

				from ch in db.GrandChild1
				group ch by ch.Parent into g
				where
					g.Count(_ => _.ChildID >= 20) > 2 &&
					g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
					g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0
				select g.Key.Value1
			);
		}

		[Test]
		public void GroupByAssociation1024([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent into g
				where
					g.Count(_ => _.ChildID >= 20) > 2 &&
					g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
					g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0 &&
					g.Where(_ => _.ChildID >= 18).Max(p => p.ParentID) > 0
				select g.Key.Value1,

				from ch in db.GrandChild1
				group ch by ch.Parent into g
				where
					g.Count(_ => _.ChildID >= 20) > 2 &&
					g.Where(_ => _.ChildID >= 19).Sum(p => p.ParentID) > 0 &&
					g.Where(_ => _.ChildID >= 19).Max(p => p.ParentID) > 0 &&
					g.Where(_ => _.ChildID >= 18).Max(p => p.ParentID) > 0
				select g.Key.Value1
			);
		}

		[Test]
		public void GroupByAssociation2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in GrandChild1
				group ch by ch.Parent into g
				where g.Count() > 2 && g.Key.ParentID != 1
				select g.Key.Value1,

				from ch in db.GrandChild1
				group ch by ch.Parent into g
				where g.Count() > 2 && g.Key.ParentID != 1
				select g.Key.Value1
			);
		}

		[Test]
		public void GroupByAssociation3([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var result =
				from p in db.Product
				group p by p.Category into g
				where g.Count() == 12
				select g.Key.CategoryName;

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(3));
		}

		[Test]
		public void GroupByAssociation4([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var result =
				from p in db.Product
				group p by p.Category into g
				where g.Count() == 12
				select g.Key.CategoryID;

			var list = result.ToList();
			Assert.That(list, Has.Count.EqualTo(3));
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void GroupByAggregate1([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key,
				from p in db.Parent
				group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key
			);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void GroupByAggregate11([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where p.Children.Count > 0
				group p by p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key,

				from p in db.Parent
				where p.Children.Count > 0
				group p by p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key
			);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void GroupByAggregate12([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key,
				from p in db.Parent
				group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3 into g
				select g.Key
			);
		}

		[Test]
		public void GroupByAggregate2([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(new DataOptions().UseConfiguration(context).UseGuardGrouping(false));
			var dd = GetNorthwindAsList(context);
			AreEqual(
				(
					from c in dd.Customer
					group c by c.Orders.Count > 0 && c.Orders.Average(o => o.Freight) >= 80
				).ToList().Select(k => k.Key),
				(
					from c in db.Customer
					group c by c.Orders.Average(o => o.Freight) >= 80
				).ToList().Select(k => k.Key)
			);
		}

		[Test]
		public void GroupByAggregate21([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(new DataOptions().UseConfiguration(context).UseGuardGrouping(false));
			var dd = GetNorthwindAsList(context);
			AreEqual(
				(
					from c in dd.Customer
					group c by c.Orders.Count > 0 && c.Orders.Average(o => o.Freight) == 33.25m
				).ToList().Select(k => k.Key),
				(
					from c in db.Customer
					group c by c.Orders.Average(o => o.Freight) == 33.25m
				).ToList().Select(k => k.Key)
			);
		}

		[Test]
		public void GroupByAggregate22([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(new DataOptions().UseConfiguration(context).UseGuardGrouping(false));
			var dd = GetNorthwindAsList(context);
			AreEqual(
				(
					from c in dd.Customer
					group c by c.Orders.Count > 0 && c.Orders.Average(o => o.Freight) != 33.25m
				).ToList().Select(k => k.Key),
				(
					from c in db.Customer
					group c by c.Orders.Average(o => o.Freight) != 33.25m
				).ToList().Select(k => k.Key)
			);
		}

		[Test]
		public void GroupByAggregate3([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				group p by p.Children.Average(c => c.ParentID) > 3
				into g
				orderby g.Key
				select g.Key;

			var expected =
				from p in Parent
				group p by p.Children.Count > 0 && p.Children.Average(c => c.ParentID) > 3
				into g
				orderby g.Key
				select g.Key;

			AreEqual(expected, query);
		}

		[Test]
		public void ByJoin([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from c1 in Child
				join c2 in Child on c1.ChildID equals c2.ChildID + 1
				group c2 by c1.ParentID into g
				select g.Sum(_ => _.ChildID),
				from c1 in db.Child
				join c2 in db.Child on c1.ChildID equals c2.ChildID + 1
				group c2 by c1.ParentID into g
				select g.Sum(_ => _.ChildID)
			);
		}

		[Test]
		public void SelectMany([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Child.GroupBy(ch => ch.ParentID).SelectMany(g => g),
				db.Child.GroupBy(ch => ch.ParentID).SelectMany(g => g)
			);
		}

		[Test]
		public void Scalar1([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Max()),
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Max())
			);
		}

		[Test]
		public void Scalar101([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));
			AreEqual(
				from ch in Child
				select ch.ChildID into id
				group id by id into g
				select g.Max(),
				from ch in db.Child
				select ch.ChildID into id
				group id by id into g
				select g.Max()
			);
		}

		[Test]
		public void Scalar2([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select new
				 {
					 Max1 = g.Select(ch => ch.ChildID).Max(),
					 Max2 = g.Select(ch => ch.ChildID + ch.ParentID).Max()
				 }),
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select new
				 {
					 Max1 = g.Select(ch => ch.ChildID).Max(),
					 Max2 = g.Select(ch => ch.ChildID + ch.ParentID).Max()
				 })
			);
		}

		[Test]
		public void Scalar3([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Where(id => id > 0).Max()),
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Where(id => id > 0).Max())
			);
		}

		[Test]
		public void Scalar4([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				where g.Where(ch => ch.ParentID > 2).Select(ch => (int?)ch.ChildID).Min() != null
				select g.Where(ch => ch.ParentID > 2).Select(ch => ch.ChildID).Min(),
				from ch in db.Child
				group ch by ch.ParentID into g
				where g.Where(ch => ch.ParentID > 2).Select(ch => (int?)ch.ChildID).Min() != null
				select g.Where(ch => ch.ParentID > 2).Select(ch => ch.ChildID).Min()
			);
		}

		[Test]
		public void Scalar41([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				group ch by ch.ParentID into g
				select new { g } into g
				where g.g.Where(ch => ch.ParentID > 2).Select(ch => (int?)ch.ChildID).Min() != null
				select g.g.Where(ch => ch.ParentID > 2).Select(ch => ch.ChildID).Min(),
				from ch in db.Child
				group ch by ch.ParentID into g
				select new { g } into g
				where g.g.Where(ch => ch.ParentID > 2).Select(ch => (int?)ch.ChildID).Min() != null
				select g.g.Where(ch => ch.ParentID > 2).Select(ch => ch.ChildID).Min()
			);
		}

		[Test]
		public void Scalar5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				select ch.ParentID into id
				group id by id into g
				select g.Max(),
				from ch in db.Child
				select ch.ParentID into id
				group id by id into g
				select g.Max()
			);
		}

		//[Test]
		//public void Scalar51([DataSources] string context)
		//{
		//	using var db = GetDataContext(context);
		//	AreEqual(
		//		from ch in Child
		//		group ch by ch.ParentID into g
		//		select g.Max(),
		//		from ch in db.Child
		//		group ch by ch.ParentID into g
		//		select g.Max()
		//	);
		//}

		[Test]
		public void Scalar6([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from ch in Child
				 where ch.ParentID < 3
				 group ch by ch.ParentID into g
				 select g.Where(ch => ch.ParentID < 3).Max(ch => ch.ChildID))
				 ,
				(from ch in db.Child
				 where ch.ParentID < 3
				 group ch by ch.ParentID into g
				 select g.Where(ch => ch.ParentID < 3).Max(ch => ch.ChildID)));
		}

		[Test]
		public void Scalar7([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select new { max = g.Select(ch => ch.ChildID).Max() }).Select(id => id.max)
				 ,
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select new { max = g.Select(ch => ch.ChildID).Max() }).Select(id => id.max));
		}

		[Test]
		public void Scalar8([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select new { max = g.Max(ch => ch.ChildID) }).Select(id => id.max)
				,
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select new { max = g.Max(ch => ch.ChildID) }).Select(id => id.max));
		}

		[Test]
		public void Scalar9([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count()),
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count()));
		}

		[Test]
		public void Scalar10([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				(from ch in Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count(id => id >= 20))
				 ,
				(from ch in db.Child
				 group ch by ch.ParentID into g
				 select g.Select(ch => ch.ChildID).Where(id => id < 30).Count(id => id >= 20)));
		}

		[Test]
		public void GroupByExtraFieldBugTest([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			// https://github.com/igor-tkachev/LinqToDB/issues/42
			// extra field is generated in the GROUP BY clause, for example:
			// GROUP BY p.LastName, p.LastName <--- the second one is redundant
			// Update: GroupBy is converted to DISTINCT, so we check that only one column is present

			using var db = GetDataConnection(context);

			var q =
				from d in db.Doctor
				join p in db.Person on d.PersonID equals p.ID
				group d by p.LastName into g
				select g.Key;

			AssertQuery(q);

			var selectQuery = q.GetSelectQuery();

			selectQuery.Select.IsDistinct.ShouldBeTrue();
			selectQuery.Select.Columns.Count.ShouldBe(1);

			selectQuery.Select.Columns[0].Expression.ShouldBeOfType<SqlField>().Name.ShouldBe("LastName");
		}

		[Test]
		public void DoubleGroupBy1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in
					from p in Parent
					where p.Value1 != null
					group p by p.ParentID into g
					select new
					{
						ID = g.Key,
						Max = g.Max(t => t.Value1)
					}
				group t by t.ID into g
				select new
				{
					g.Key,
					Sum = g.Sum(t => t.Max)
				},
				from t in
					from p in db.Parent
					where p.Value1 != null
					group p by p.ParentID into g
					select new
					{
						ID = g.Key,
						Max = g.Max(t => t.Value1)
					}
				group t by t.ID into g
				select new
				{
					g.Key,
					Sum = g.Sum(t => t.Max)
				});
		}

		[Test]
		public void DoubleGroupBy2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				where p.Value1 != null
				group p by p.ParentID into g
				select new
				{
					ID = g.Key,
					Max = g.Max(t => t.Value1)
				} into t
				group t by t.ID into g
				select new
				{
					g.Key,
					Sum = g.Sum(t => t.Max)
				},
				from p in db.Parent
				where p.Value1 != null
				group p by p.ParentID into g
				select new
				{
					ID = g.Key,
					Max = g.Max(t => t.Value1)
				} into t
				group t by t.ID into g
				select new
				{
					g.Key,
					Sum = g.Sum(t => t.Max)
				});
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void InnerQuery([DataSources(ProviderName.SqlCe, TestProvName.AllSapHana, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Doctor.GroupBy(s => s.PersonID).Select(s => s.Select(d => d.Taxonomy).First()),
				db.Doctor.GroupBy(s => s.PersonID).Select(s => s.Select(d => d.Taxonomy).First()));
		}

		[Test]
		public void CalcMember([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from parent in Parent
				from child in Person
				where child.ID == parent.ParentID
				let data = new
				{
					parent.Value1,
					Value = child.FirstName == "John" ? child.FirstName : "a"
				}
				group data by data.Value into groupedData
				select new
				{
					groupedData.Key,
					Count = groupedData.Count()
				},
				from parent in db.Parent
				from child in db.Person
				where child.ID == parent.ParentID
				let data = new
				{
					parent.Value1,
					Value = child.FirstName == "John" ? child.FirstName : "a"
				}
				group data by data.Value into groupedData
				select new
				{
					groupedData.Key,
					Count = groupedData.Count()
				});
		}

		[Test]
		public void GroupByNone([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from parent in Parent
				group parent by Sql.GroupBy.None into gr
				select new
				{
					Min = gr.Min(p => p.ParentID),
					Max = gr.Max(p => p.ParentID),
				},
				from parent in db.Parent
				group parent by Sql.GroupBy.None into gr
				select new
				{
					Min = gr.Min(p => p.ParentID),
					Max = gr.Max(p => p.ParentID),
				});
		}

		[Test]
		public void EmptySetAggregateNullability([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);

			var query = from parent in db.Parent
						where parent.ParentID == -1
						group parent by Sql.GroupBy.None into gr
						select new
						{
							Min = gr.Min(p => p.ParentID),
							Max = gr.Max(p => p.ParentID),
							Avg = gr.Average(p => p.ParentID),
							Sum = gr.Sum(p => p.ParentID),
							Count = gr.Count(),
						};

			// aggregates (except count) return null on empty set
			var result = query.AsSubQuery().Where(r => r.Min != 0).Count();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result, Is.EqualTo(1));
				Assert.That(db.LastQuery, Contains.Substring("IS NULL"));
			}

			result = query.AsSubQuery().Where(r => r.Max != 0).Count();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result, Is.EqualTo(1));
				Assert.That(db.LastQuery, Contains.Substring("IS NULL"));
			}

			result = query.AsSubQuery().Where(r => r.Avg != 0).Count();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result, Is.EqualTo(1));
				Assert.That(db.LastQuery, Contains.Substring("IS NULL"));
			}

			result = query.AsSubQuery().Where(r => r.Sum != 0).Count();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result, Is.EqualTo(1));
				Assert.That(db.LastQuery, Contains.Substring("IS NULL"));
			}

			result = query.AsSubQuery().Where(r => r.Count != 0).Count();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result, Is.Zero);
				Assert.That(db.LastQuery, Does.Not.Contains("IS NULL"));
			}
		}

		[Test]
		public void GroupByExpression([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var defValue = 10;

			AreEqual(
				from parent in Parent
				group parent by Sql.GroupBy.None into gr
				select new
				{
					Min = Sql.AsSql(gr.Min(p => (int?)p.ParentID) ?? defValue),
				},
				from parent in db.Parent
				group parent by Sql.GroupBy.None into gr
				select new
				{
					Min = Sql.AsSql(gr.Min(p => (int?)p.ParentID) ?? defValue),
				});
		}

		[Test]
		public void GroupByDate1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types
				group t by new { t.DateTimeValue.Month, t.DateTimeValue.Year } into grp
				select new
				{
					Total = grp.Sum(_ => _.MoneyValue),
					year = grp.Key.Year,
					month = grp.Key.Month
				},
				from t in db.Types
				group t by new { t.DateTimeValue.Month, t.DateTimeValue.Year } into grp
				select new
				{
					Total = grp.Sum(_ => _.MoneyValue),
					year = grp.Key.Year,
					month = grp.Key.Month
				});
		}

		[Test]
		public void GroupByDate2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types2
				group t by new { t.DateTimeValue!.Value.Month, t.DateTimeValue.Value.Year } into grp
				select new
				{
					Total = grp.Sum(_ => _.MoneyValue),
					year = grp.Key.Year,
					month = grp.Key.Month
				},
				from t in db.Types2
				group t by new { t.DateTimeValue!.Value.Month, t.DateTimeValue.Value.Year } into grp
				select new
				{
					Total = grp.Sum(_ => _.MoneyValue),
					year = grp.Key.Year,
					month = grp.Key.Month
				});
		}

		[Test]
		public void GroupByDate3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query = from t in db.Types2
						group t by new { Date = Sql.MakeDateTime(t.DateTimeValue!.Value.Year, t.DateTimeValue.Value.Month, 1) } into grp
						select new
						{
							Total = grp.Sum(_ => _.MoneyValue),
							year  = grp.Key.Date!.Value.Year,
							month = grp.Key.Date.Value.Month
						};

			var result = query.ToList();

			AreEqual(
				from t in Types2
				group t by new { Date = Sql.MakeDateTime(t.DateTimeValue!.Value.Year, t.DateTimeValue.Value.Month, 1) } into grp
				select new
				{
					Total = grp.Sum(_ => _.MoneyValue),
					year = grp.Key.Date!.Value.Year,
					month = grp.Key.Date.Value.Month
				},
				from t in db.Types2
				group t by new { Date = Sql.MakeDateTime(t.DateTimeValue!.Value.Year, t.DateTimeValue.Value.Month, 1) } into grp
				select new
				{
					Total = grp.Sum(_ => _.MoneyValue),
					year = grp.Key.Date!.Value.Year,
					month = grp.Key.Date.Value.Month
				});
		}

		[Test]
		public void GroupByCount([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(
					       (from t in db.Child group t by t.ParentID into gr select new { gr.Key, List = gr.ToList() }).Count(),
				Is.EqualTo((from t in    Child group t by t.ParentID into gr select new { gr.Key, List = gr.ToList() }).Count())
			);
		}

		[Test]
		public void AggregateAssociation([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Child
				group t by t.ParentID into grp
				select new
				{
					Value = grp.Sum(c => c.Parent!.Value1 ?? 0)
				},
				from t in db.Child
				group t by t.ParentID into grp
				select new
				{
					Value = grp.Sum(c => c.Parent!.Value1 ?? 0)
				});
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void FirstGroupBy([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));
			Assert.That(
					(
						from t in db.Child
						group t by t.ParentID into gr
						select gr.OrderByDescending(g => g.ChildID).First()
					).AsEnumerable().OrderBy(t => t.ChildID),
				Is.EqualTo(
					(
						from t in Child
						group t by t.ParentID into gr
						select gr.OrderByDescending(g => g.ChildID).First()
					).AsEnumerable().OrderBy(t => t.ChildID)
				)
			);
		}

		public class ChildEntity
		{
			public int ParentID;
			public int ChildID;
			public int RandValue;
		}

		[Test]
		public void GroupByCustomEntity1([DataSources] string context)
		{
			// I definitly selected it by random
			var rand = 3;
			//var rand = new Random().Next(5);
			//var rand = new Random();

			using var db = GetDataContext(context);
			AreEqual(
				from e in
					from c in Child
					select new ChildEntity
					{
						RandValue = rand//.Next(5)
						,
						ParentID = c.ParentID,
					}
				group e by new { e.ParentID, e.RandValue } into g
				select new
				{
					Count = g.Count()
				},
				from e in
					from c in db.Child
					select new ChildEntity
					{
						RandValue = rand,
						ParentID = c.ParentID,
					}
				group e by new { e.ParentID, e.RandValue } into g
				select new
				{
					Count = g.Count()
				});
		}

		static int GetID(int id)
		{
			return id;
		}

		[Test]
		public void GroupByCustomEntity2([DataSources(TestProvName.AllSybase)] string context)
		{
			// pure random
			var rand = 3;

			using var db = GetDataContext(context);
			AreEqual(
				from e in
					from c in Child
					select new ChildEntity
					{
						RandValue = GetID(rand),
						ParentID = c.ParentID,
					}
				group e by new { e.ParentID, e.RandValue } into g
				select new
				{
					Count = g.Count()
				},
				from e in
					from c in db.Child
					select new ChildEntity
					{
						RandValue = GetID(rand),
						ParentID = c.ParentID,
					}
				group e by new { e.ParentID, e.RandValue } into g
				select new
				{
					Count = g.Count()
				});
		}

		[Test]
		public void JoinGroupBy1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from c in Child
				from g in c.GrandChildren
				group c by g.ParentID into gc
				select gc.Key,
				from c in db.Child
				from g in c.GrandChildren
				group c by g.ParentID into gc
				select gc.Key
			);
		}

		[Test]
		public void JoinGroupBy2([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from c in Child
				from g in c.Parent!.Children
				group g by g.ParentID into gc
				select gc.Key,
				from c in db.Child
				from g in c.Parent!.Children
				group g by g.ParentID into gc
				select gc.Key
			);
		}

		[Test]
		public void OrderByGroupBy([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query1 =
				from c in db.Child
				orderby c.ChildID, c.ParentID
				select c;

			var query2 =
				from c1 in query1
				group c1 by c1.ParentID into c2
				select c2.Key;

			Assert.DoesNotThrow(() => query2.ToArray());

			var orderItems = query2.GetSelectQuery().OrderBy.Items;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(orderItems, Has.Count.EqualTo(1));
				Assert.That(QueryHelper.GetUnderlyingField(orderItems[0].Expression)!.Name, Is.EqualTo("ParentID"));
			}
		}

		[Test]
		public void CountGroupBy1([DataSources()] string context)
		{
			using var db = GetDataContext(context);
			var query =
				from c in db.Child
				orderby c.ChildID
				select c;

			Assert.DoesNotThrow(() => query.Count());
		}

		[Test]
		public void CountGroupBy2([DataSources()] string context)
		{
			using var db = GetDataContext(context);
			var query =
				from c in db.Child.OrderBy(c => c.ChildID)
				join p in db.Parent on c.ParentID equals p.ParentID
				select new { c, p };

			Assert.DoesNotThrow(() => query.Count());
		}

		[Test]
		public void CountGroupBy3([DataSources()] string context)
		{
			using var db = GetDataContext(context);
			var query = from p in db.Parent
						join c in db.Child.OrderBy(c => c.ChildID) on p.ParentID equals c.ParentID
						select new { c, p };

			Assert.DoesNotThrow(() => query.Count());
		}

		void CheckGuardedQuery<TKey, TEntity>(IQueryable<IGrouping<TKey, TEntity>> grouping)
			where TKey : notnull
		{
			Assert.Throws<LinqToDBException>(() =>
			{
				grouping.ToDictionary(_ => _.Key, _ => _.ToList());
			});

			Assert.DoesNotThrow(() =>
			{
				grouping.DisableGuard().ToDictionary(_ => _.Key, _ => _.ToList());
			});
		}

		[Test]
		public void GroupByGuard([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(true));
			// group on client
			var dictionary1 = db.Person
					.AsEnumerable()
					.GroupBy(_ => _.Gender)
					.ToDictionary(_ => _.Key, _ => _.ToList());

			var dictionary2 = Person
					.AsEnumerable()
					.GroupBy(_ => _.Gender)
					.ToDictionary(_ => _.Key, _ => _.ToList());

			Assert.That(dictionary1, Has.Count.EqualTo(dictionary2.Count));
			Assert.That(dictionary1.First().Value, Has.Count.EqualTo(dictionary2.First().Value.Count));

			var __ =
				(
					from p in db.Person
					group p by p.Gender into gr
					select new { gr.Key, Count = gr.Count() }
				)
				.ToDictionary(_ => _.Key);

			CheckGuardedQuery(db.Person.GroupBy(_ => _.Gender));
			CheckGuardedQuery(db.Person.GroupBy(_ => _));

			Assert.Throws<LinqToDBException>(() =>
			{
				db.Person
					.GroupBy(_ => _)
					.ToList();
			});

			Assert.DoesNotThrow(() =>
			{
				db.Person
					.GroupBy(_ => _)
					.DisableGuard()
					.ToList();
			});
		}

		[Test]
		public void GroupByGuardCheckOptions([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool guard)
		{
			using var db = GetDataContext(context, b => b.UseGuardGrouping(guard));
			// group on client
			var query = db.Person
					.GroupBy(_ => _.Gender);

			var act = () => query.ToList();
			if (guard)
				act.ShouldThrow<LinqToDBException>();
			else
				act();
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		private static int Noop(int value)
		{
			throw new InvalidOperationException();
		}

		[Test]
		public void GroupByExpression2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.Child
				.GroupBy(_ => Noop(_.ChildID))
				.Select(_ => new
				{
					x = _.Key,
					y = _.Average(r => r.ParentID)
				})
				.ToList();
		}

		[Table("Stone")]
		public class Stone
		{
			[PrimaryKey, Identity] public int     Id           { get; set; } // int
			[Column, NotNull     ] public string  Name         { get; set; } = null!; // nvarchar(256)
			[Column, Nullable    ] public bool?   Enabled      { get; set; } // bit
			[Column, Nullable    ] public string? ImageFullUrl { get; set; } // nvarchar(255)
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void Issue672Test([DataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context, o => o.UseGuardGrouping(false)))
			using (db.CreateLocalTable<Stone>())
			{
				db.Insert(new Stone() { Id = 1, Name = "group1", Enabled = true, ImageFullUrl = "123" });
				db.Insert(new Stone() { Id = 2, Name = "group1", Enabled = true, ImageFullUrl = "123" });
				db.Insert(new Stone() { Id = 3, Name = "group2", Enabled = true, ImageFullUrl = "123" });

				IQueryable<Stone> stones;
				stones = from s in db.GetTable<Stone>() where s.Enabled == true select s;

				stones = from s in stones
						 where !s.Name.StartsWith("level - ") && s.ImageFullUrl!.Length > 0
						 group s by s.Name
							  into sG
						 select sG.First();

				var list = stones.ToList();
			}
		}

		[Table]
		sealed class Issue680Table
		{
			[PrimaryKey] public int Id;
			[Column] public DateTime TimeStamp;
		}

		[Test]
		public void Issue680Test([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var table = db.CreateLocalTable<Issue680Table>();
			var result = (from record in table
						  group record by record.TimeStamp into g
						  select new
						  {
							  res = g.Count(r => r.TimeStamp > TestData.DateTime),
						  }).ToList();

			var index = db.LastQuery!.IndexOf("SELECT");
			Assert.That(index, Is.Not.EqualTo(-1));
			index = db.LastQuery.IndexOf("SELECT", index + 1);
			Assert.That(index, Is.EqualTo(-1));
		}

		[Test]
		public void Issue434Test1([DataSources] string context)
		{
			var input = "test";

			using var db = GetDataContext(context);
			var result = db.Person.GroupJoin(db.Patient, re => re.ID, ri => ri.PersonID, (re, ri) => new
			{
				Name = re.FirstName,
				Roles = ri.ToList().Select(p => p.Diagnosis)
			}).Where(p => p.Name.ToLower().Contains(input.ToLower())).ToList();
		}

		[Test]
		public void Issue434Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Person.GroupJoin(db.Patient, re => re.ID, ri => ri.PersonID, (re, ri) => new
			{
				Name = re.FirstName,
				Roles = ri.ToList().Select(p => p.Diagnosis)
			}).Where(p => p.Name.ToLower().Contains("test".ToLower())).ToList();
		}

		[Table(Name = "Issue913Test")]
		public class Instrument
		{
			[Column, PrimaryKey, NotNull] public int InstrumentID { get; set; } // int
			[Column(Length = 1), Nullable] public TradingStatus? TradingStatus { get; set; } // char(1)

			public static readonly Instrument[] Data = new[]
			{
				new Instrument() { InstrumentID = 1 },
				new Instrument() { InstrumentID = 2, TradingStatus = GroupByTests.TradingStatus.Active },
				new Instrument() { InstrumentID = 3, TradingStatus = GroupByTests.TradingStatus.Delisted }
			};
		}

		public enum TradingStatus
		{
			[MapValue("A")] Active,
			[MapValue("D")] Delisted,
		}

		[Test]
		public void Issue913Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(Instrument.Data);
			var q =
				from i in table
				group i by new
				{
					IsDelisted = i.TradingStatus == TradingStatus.Delisted
				}
				into g
				select new
				{
					g.Key.IsDelisted,
					Count = g.Count(),
				};

			var x = q.ToList().OrderBy(_ => _.Count).ToArray();

			Assert.That(x, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(x[0].IsDelisted, Is.True);
				Assert.That(x[0].Count, Is.EqualTo(1));
				Assert.That(x[1].IsDelisted, Is.False);
				Assert.That(x[1].Count, Is.EqualTo(2));
			}
		}

		sealed class Issue1078Table
		{
			[PrimaryKey]
			public int UserID { get; set; }
			[Column]
			public int SiteID { get; set; }
			[Column]
			public bool Active { get; set; }

			public static readonly Issue1078Table[] TestData = new []
			{
				new Issue1078Table() { UserID = 1, SiteID = 1, Active = true  },
				new Issue1078Table() { UserID = 2, SiteID = 1, Active = false },
				new Issue1078Table() { UserID = 3, SiteID = 1, Active = true  },
				new Issue1078Table() { UserID = 4, SiteID = 2, Active = false },
				new Issue1078Table() { UserID = 5, SiteID = 2, Active = true  },
				new Issue1078Table() { UserID = 6, SiteID = 2, Active = false },
				new Issue1078Table() { UserID = 7, SiteID = 2, Active = false },
				new Issue1078Table() { UserID = 8, SiteID = 3, Active = false },
				new Issue1078Table() { UserID = 9, SiteID = 4, Active = true  },
			};
		}

		[Test]
		public void Issue1078Test([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(Issue1078Table.TestData);
			var query =
				from u in table
				group u.Active ? 1 : 0 by u.SiteID into grp
				select new
				{
					SiteID   = grp.Key,
					Total    = grp.Count(),
					Inactive = grp.Count(_ => _ == 0)
				};

			var res = query.ToList().OrderBy(_ => _.SiteID).ToArray();

			Assert.That(res, Has.Length.EqualTo(4));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].SiteID, Is.EqualTo(1));
				Assert.That(res[0].Total, Is.EqualTo(3));
				Assert.That(res[0].Inactive, Is.EqualTo(1));

				Assert.That(res[1].SiteID, Is.EqualTo(2));
				Assert.That(res[1].Total, Is.EqualTo(4));
				Assert.That(res[1].Inactive, Is.EqualTo(3));

				Assert.That(res[2].SiteID, Is.EqualTo(3));
				Assert.That(res[2].Total, Is.EqualTo(1));
				Assert.That(res[2].Inactive, Is.EqualTo(1));

				Assert.That(res[3].SiteID, Is.EqualTo(4));
				Assert.That(res[3].Total, Is.EqualTo(1));
				Assert.That(res[3].Inactive, Is.Zero);
			}
		}

		sealed class Issue1192Table
		{
			[PrimaryKey] public int IdId { get; internal set; }
			public int MyOtherId { get; internal set; }
			public int Status { get; internal set; }
		}

		[Test]
		public void Issue1198Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<Issue1192Table>();
			var stats = (from t in table
						 where t.MyOtherId == 12
						 group t by 1 into g
						 select new
						 {
							 MyGroupedCount = g.Count(i => i.Status == 3),
						 }).FirstOrDefault();
		}

		[Test]
		public void Issue2306Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context, o => o.UseGuardGrouping(false).UseDisableQueryCache(true)))
			{
				db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}

			using (var db = GetDataContext(context, o => o.UseGuardGrouping(true).UseDisableQueryCache(true)))
			{
				Assert.Throws<LinqToDBException>(() => db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList()));
			}

			using (var db = GetDataContext(context, o => o.UseGuardGrouping(true).UseDisableQueryCache(true)))
			{
				db.Person.GroupBy(p => p.ID).DisableGuard().ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}
		}

		[Test]
		public void Issue2306Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context, o => o.UseGuardGrouping(false).UseDisableQueryCache(true)))
			{
				db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}

			using (var db = GetDataContext(context, o => o.UseGuardGrouping(true).UseDisableQueryCache(true)))
			{
				Assert.Throws<LinqToDBException>(() => db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList()));
			}

			using (var db = GetDataContext(context, o => o.UseGuardGrouping(true).UseDisableQueryCache(true)))
			{
				db.Person.GroupBy(p => p.ID).DisableGuard().ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}
		}

		[Test]
		public void Issue2306Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context, o => o.UseGuardGrouping(true).UseDisableQueryCache(true)))
			{
				Assert.Throws<LinqToDBException>(() => db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList()));
			}

			using (var db = GetDataContext(context, o => o.UseGuardGrouping(false).UseDisableQueryCache(true)))
			{
				db.Person.GroupBy(p => p.ID).ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}

			using (var db = GetDataContext(context, o => o.UseGuardGrouping(true).UseDisableQueryCache(true)))
			{
				db.Person.GroupBy(p => p.ID).DisableGuard().ToDictionary(g => g.Key, g => g.Select(p => p.LastName).ToList());
			}
		}

		[Test]
		public void IssueGroupByNonTableColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query = db.Person
				.Select(_ => 1)
				.Concat(db.Person.Select(_ => 2))
				.GroupBy(_ => _)
				.Select(_ => new { _.Key, Count = _.Count() })
				.Where(_ => _.Key == 1)
				.Select(_ => _.Count)
				.Where(_ => _ > 1)
				.Count();
		}

		[Test]
		public void GroupByWithSubquery([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var emptyEquery = db.Person
				.Select(_ => 1)
				.Where(_ => false);

			var query = emptyEquery
				.GroupBy(_ => _)
				.Select(_ => new { _.Key, Count = _.Count() })
				.ToList();

			Assert.That(query, Is.Empty);
		}

		[Test]
		public void Issue3668Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var id   = 1;
			var name = "test";

			// use two parameters with different types to ensure fix works with positional parameters
			var result = db.Person
				.Where(x => (x.ID == id && x.LastName != name) || (x.FirstName != name && x.ID - 1 == id))
				.GroupBy(x => x.ID, x => x).DisableGuard()
				.ToList();

			foreach (var x in result)
			{
				foreach (var y in x)
				{
				}
			}
		}

		[Table]
		public class Issue3761Table
		{
			[Column, NotNull, PrimaryKey] public int?      LETO     { get; set; }
			[Column, NotNull, PrimaryKey] public int?      STEVILKA { get; set; }
			[Column                     ] public DateTime? DATUM    { get; set; }
			[Column                     ] public decimal?  SKUPAJ   { get; set; }
		}

		[Test]
		public void Issue3761Test1([DataSources(ProviderName.Ydb, TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSqlServer2005, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<Issue3761Table>();

			var query = table.Where(n => n.DATUM < new DateTime(2019, 1, 1))
				.GroupBy(
					n => new
					{
						n.DATUM.GetValueOrDefault().Year,
						n.DATUM.GetValueOrDefault().Month
					},
					(k, n) => new
					{
						k.Year,
						k.Month,
						Sum = n.Sum(nal => nal.SKUPAJ)
					});

			query.ToList();
			Assert.That(query.GetSelectQuery().GroupBy.Items, Has.Count.EqualTo(2));
		}

		[Test]
		public void Issue3761Test2([DataSources(ProviderName.Ydb, TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSqlServer2005)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<Issue3761Table>();

			var query = table.Where(n => n.DATUM < new DateTime(2019, 1, 1))
				.GroupBy(
					n => new
					{
						n.DATUM.GetValueOrDefault().Year,
						n.DATUM.GetValueOrDefault().Month
					},
					(k, n) => new
					{
						k.Year,
						k.Month,
						Sum = n.Sum(nal => nal.SKUPAJ)
					})
				.UnionAll(
					table
						.Where(n => n.DATUM >= new DateTime(2019, 1, 1))
						.GroupBy(
							n => new
							{
								n.DATUM.GetValueOrDefault().Year,
								n.DATUM.GetValueOrDefault().Month
							},
							(k, n) => new
							{
								k.Year,
								k.Month,
								Sum = n.Sum(nal => nal.SKUPAJ)
							}));

			query.ToList();

			var sql = query.GetSelectQuery();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sql.GroupBy.Items, Has.Count.EqualTo(2));
				Assert.That(sql.SetOperators[0].SelectQuery.GroupBy.Items, Has.Count.EqualTo(2));
			}
		}

		[Test]
		public void Issue3872([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<Issue3761Table>();

			var query1 = db.Person.GroupBy(_ => 1).Select(r => r.Max(r => r.ID));

			var query2 = db.Person.Select(r => r.ID);
			var query  = query1.Concat(query2);

			query.ToList();

			var ast = query.GetSelectQuery();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ast.GroupBy.Items, Is.Empty);
				Assert.That(ast.From.Tables, Has.Count.EqualTo(1));
			}

			if (ast.From.Tables[0] is not SqlTableSource source)
			{
				Assert.Fail("fail");
			}
			else
			{
				Assert.That(source.Source.ElementType, Is.EqualTo(QueryElementType.SqlTable));
			}
		}

		#region Issue 4098
		sealed class Transaction
		{
			[PrimaryKey]				public int     Id         { get; set; }
										public string? InvestorId   { get; set; }
			[Column(CanBeNull = false)] public string SecurityClass { get; set; } = null!;
									public int     Units        { get; set; }

			public static readonly Transaction[] Data = new []
			{
				new Transaction() { Id = 1, InvestorId = "inv1", SecurityClass = "test", Units = 100 },
				new Transaction() { Id = 2, InvestorId = "inv1", SecurityClass = "test", Units = 200 },
				new Transaction() { Id = 3, InvestorId = "inv2", SecurityClass = "test", Units = 300 },
				new Transaction() { Id = 4, InvestorId = "inv2", SecurityClass = "test", Units = 400 },
			};
		}

		[Table(IsColumnAttributeRequired = false)]
		sealed class InvestorPayment
		{
			[PrimaryKey]				public int     Id         { get; set; }
			[Column(CanBeNull = false)] public string  InvestorId { get; set; } = null!;
										public int     NetPayment { get; set; }

			public static readonly InvestorPayment[] Data = new []
			{
				new InvestorPayment() { Id = 1, InvestorId = "inv1", NetPayment = 100 },
				new InvestorPayment() { Id = 2, InvestorId = "inv2", NetPayment = 200 },
			};
		}

		sealed class PaymentEvent
		{
			[PrimaryKey]				public int     Id           { get; set; }
										public string? Description  { get; set; }
			[Column(CanBeNull = false)] public string SecurityClass { get; set; } = null!;

			public static readonly PaymentEvent[] Data = new []
			{
				new PaymentEvent() { Id = 1, Description = "one", SecurityClass = "test" },
				new PaymentEvent() { Id = 2, Description = "two", SecurityClass = "test" },
			};
		}

		sealed class InvestorPaymentDetail
		{
			public string? InvestorId    { get; set; }
			[PrimaryKey] public int     CalculationId { get; set; }

			public static readonly InvestorPaymentDetail[] Data = new []
			{
				new InvestorPaymentDetail() { InvestorId = "inv1", CalculationId = 1 },
				new InvestorPaymentDetail() { InvestorId = "inv2", CalculationId = 2 },
			};
		}

		sealed class PaymentCalculation
		{
			[PrimaryKey] public int Id      { get; set; }
			public int EventId { get; set; }

			public static readonly PaymentCalculation[] Data = new []
			{
				new PaymentCalculation() { Id = 1, EventId = 1 },
				new PaymentCalculation() { Id = 2, EventId = 2 },
			};
		}

		[Test]
		public void Issue4098WithCte([CteContextSource] string context)
		{
			using var db = GetDataContext(context);

			using var transactions           = db.CreateLocalTable(Transaction.Data);
			using var investorPayments       = db.CreateLocalTable(InvestorPayment.Data);
			using var paymentEvents          = db.CreateLocalTable(PaymentEvent.Data);
			using var investorPaymentDetails = db.CreateLocalTable(InvestorPaymentDetail.Data);
			using var paymentCalculations    = db.CreateLocalTable(PaymentCalculation.Data);

			var balances = (from x in transactions
							group x by new { x.SecurityClass, x.InvestorId } into g
							select new
							{
								g.Key.InvestorId,
								g.Key.SecurityClass,
								Units = g.Sum(x => x.Units)
							});

			balances = balances.AsCte();

			var payments = (from pe in paymentEvents
							join ip in investorPayments on pe.Id equals ip.Id
							join ipd in investorPaymentDetails on ip.InvestorId equals ipd.InvestorId
							join pc in paymentCalculations on new { calc = ipd.CalculationId, eid = pe.Id } equals new { calc = pc.Id, eid = pc.EventId }
							join b in balances on new { inv = ip.InvestorId, cls = pe.SecurityClass } equals new { inv = b.InvestorId, cls = b.SecurityClass }
							select new
							{
								ip.InvestorId,
								pe.Description,
								ip.NetPayment,
								TotalUnits = b.Units
							});

			var grouppedPayments = (from x in payments
									group x by new { x.InvestorId, x.TotalUnits } into g
									select new
									{
										g.Key.InvestorId,
										TotalAmount = g.Sum(x => x.NetPayment),
										TotalUnits  = g.Key.TotalUnits
									});

			var retval = (from p in grouppedPayments
						  select new
						  {
							  INVESTORID    = p.InvestorId,
							  TOTALUNITS    = p.TotalUnits,
							  PAYMENTAMOUNT = p.TotalAmount,
						  }).ToList().OrderBy(r => r.INVESTORID).ToArray();

			Assert.That(retval, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(retval[0].INVESTORID, Is.EqualTo("inv1"));
				Assert.That(retval[0].PAYMENTAMOUNT, Is.EqualTo(100));
				Assert.That(retval[0].TOTALUNITS, Is.EqualTo(300));
				Assert.That(retval[1].INVESTORID, Is.EqualTo("inv2"));
				Assert.That(retval[1].PAYMENTAMOUNT, Is.EqualTo(200));
				Assert.That(retval[1].TOTALUNITS, Is.EqualTo(700));
			}
		}

		[Test]
		public void Issue4098([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			using var transactions           = db.CreateLocalTable(Transaction.Data);
			using var investorPayments       = db.CreateLocalTable(InvestorPayment.Data);
			using var paymentEvents          = db.CreateLocalTable(PaymentEvent.Data);
			using var investorPaymentDetails = db.CreateLocalTable(InvestorPaymentDetail.Data);
			using var paymentCalculations    = db.CreateLocalTable(PaymentCalculation.Data);

			var balances = (from x in transactions
							group x by new { x.SecurityClass, x.InvestorId } into g
							select new
							{
								g.Key.InvestorId,
								g.Key.SecurityClass,
								Units = g.Sum(x => x.Units)
							});

			var payments = (from pe in paymentEvents
							join ip in investorPayments on pe.Id equals ip.Id
							join ipd in investorPaymentDetails on ip.InvestorId equals ipd.InvestorId
							join pc in paymentCalculations on new { calc = ipd.CalculationId, eid = pe.Id } equals new { calc = pc.Id, eid = pc.EventId }
							join b in balances on new { inv = ip.InvestorId, cls = pe.SecurityClass } equals new { inv = b.InvestorId, cls = b.SecurityClass }
							select new
							{
								ip.InvestorId,
								pe.Description,
								ip.NetPayment,
								TotalUnits = b.Units
							});

			var grouppedPayments = (from x in payments
									group x by new { x.InvestorId, x.TotalUnits } into g
									select new
									{
										g.Key.InvestorId,
										TotalAmount = g.Sum(x => x.NetPayment),
										TotalUnits  = g.Key.TotalUnits
									});

			var retval = (from p in grouppedPayments
						  select new
						  {
							  INVESTORID    = p.InvestorId,
							  TOTALUNITS    = p.TotalUnits,
							  PAYMENTAMOUNT = p.TotalAmount,
						  }).ToList().OrderBy(r => r.INVESTORID).ToArray();

			Assert.That(retval, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(retval[0].INVESTORID, Is.EqualTo("inv1"));
				Assert.That(retval[0].PAYMENTAMOUNT, Is.EqualTo(100));
				Assert.That(retval[0].TOTALUNITS, Is.EqualTo(300));
				Assert.That(retval[1].INVESTORID, Is.EqualTo("inv2"));
				Assert.That(retval[1].PAYMENTAMOUNT, Is.EqualTo(200));
				Assert.That(retval[1].TOTALUNITS, Is.EqualTo(700));
			}
		}
		#endregion

		[Test]
		public void GroupSubqueryTest1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AssertQuery(
				from pmp in
				(
					from pmp in db.Child
					group pmp by pmp.ParentID into g
					select g.Key
				)
				from pmp1 in db.Child
				select new { pmp1.ChildID });
		}

		[Test]
		public void GroupSubqueryTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AssertQuery(
				from pmp1 in db.Child
				from pmp in
				(
					from pmp in db.Child
					group pmp by pmp.ParentID into g
					select g.Key
				)
				select new { pmp1.ChildID });
		}

		[Test]
		public void GroupSubqueryTest3([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AssertQuery(
				from pmp in
				(
					from pmp in db.Child
					group pmp by pmp.ParentID into g
					select g.Key
				)
				select new { pmp });
		}

		[Test]
		public void GroupByConstants([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var constants =
				from c in db.Child
				select new { ChildId = 1, ParentId = 2 };

			var query =
				from c in constants
				group c by new { c.ChildId, c.ParentId }
				into g
				select new { g.Key, Count = g.Count() };

			AssertQuery(query);
		}

		[Test]
		public void GroupByConstantsEmpty([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var constants =
				from c in db.Child.Where(c => false)
				select new { ChildId = 1, ParentId = 2 };

			var query =
				from c in constants
				group c by new { c.ChildId, c.ParentId }
				into g
				select new { g.Key, Count = g.Count() };

			AssertQuery(query);
		}

		[Test]
		public void GroupByInOuterApply([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				let cItems = p.Children.GroupBy(c => c.ParentID, (key, grouped) => new { Id = key, Count = grouped.Count() })
				select new
				{
					p.ParentID,
					First = cItems.OrderBy(x => x.Count).ThenBy(x => x.Id).FirstOrDefault()
				};

			query.ToArray();

			var selectQuery = query.GetSelectQuery();

			// We check that grouping is left in the subquery

			var sqlJoinedTable = selectQuery.From.Tables[0].Joins[0];
			sqlJoinedTable.JoinType.ShouldBe(JoinType.OuterApply);
			var joinQuery = (SelectQuery)sqlJoinedTable.Table.Source;
			joinQuery.GroupBy.IsEmpty.ShouldBeTrue();
			joinQuery.OrderBy.Items.Count.ShouldBe(2);
		}

		#region issue 4256
		[Test]
		public void TestIssue4256AnonymousClass([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AssertQuery(
				db.Types
					.Select(it => new
					{
						IsActive = true,
						Other = Convert.ToBoolean(it.SmallIntValue)
					})
					.GroupBy(it => it)
					.Select(it => it.Key));
		}

		class GroupByTypeTestClass
		{
			public required bool IsActive { get; set; }
			public required bool Other    { get; set; }

			// needed for client-side group-by by AssertQuery
			public override bool Equals(object? obj) => obj is GroupByTypeTestClass other && IsActive == other.IsActive && Other == other.Other;
			public override int GetHashCode() => IsActive.GetHashCode() ^ Other.GetHashCode();
		}

		[Test]
		public void TestIssue4256Class([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AssertQuery(
				db.Types
					.Select(it => new GroupByTypeTestClass()
					{
						IsActive = true,
						Other = Convert.ToBoolean(it.SmallIntValue)
					})
					.GroupBy(it => it)
					.Select(it => it.Key));
		}

		class GroupByTypeTestClassNullable
		{
			public required bool? IsActive { get; set; }
			public required bool  Other    { get; set; }

			// needed for client-side group-by by AssertQuery
			public override bool Equals(object? obj) => obj is GroupByTypeTestClassNullable other && IsActive == other.IsActive && Other == other.Other;
			public override int GetHashCode() => (IsActive?.GetHashCode() ?? 0) ^ Other.GetHashCode();
		}

		[Test]
		public void TestIssue4256ClassNullableFlag([DataSources] string context)
		{
			using var db = GetDataContext(context);

			AssertQuery(
				db.Types
					.Select(it => new GroupByTypeTestClassNullable()
					{
						IsActive = true,
						Other = Convert.ToBoolean(it.SmallIntValue)
					})
					.GroupBy(it => it)
					.Select(it => it.Key));
		}
		#endregion

		[Test]
		public void NoGuardException([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from outer in db.Types
				join innerGroup in
					(
						from r in db.Types
						group r by r.GuidValue into g
						select new
						{
							Id    = g.Key,
							Count = g.Count(d => d.BoolValue)
						}
					)
					on outer.GuidValue equals innerGroup.Id into rightGroup
				from inner in rightGroup.DefaultIfEmpty()
				select new
				{
					Result = (int?)inner.Count ?? 0
				};

			AssertQuery(query);
		}

		[Sql.Expression("(COUNT_BIG(*) * 100E0 / SUM(COUNT_BIG(*)) OVER())", ServerSideOnly = true, IsAggregate = true)]
		private static double CountPercentsAggregate()
		{
			throw new InvalidOperationException("This function should be used only in database code");
		}

		[Sql.Expression("(COUNT_BIG(*) * 100E0 / SUM(COUNT_BIG(*)) OVER())", ServerSideOnly = true, IsWindowFunction = true)]
		private static double CountPercentsWindow()
		{
			throw new InvalidOperationException("This function should be used only in database code");
		}

		[Sql.Expression("(COUNT_BIG(*) * 100E0 / SUM(COUNT_BIG(*)) OVER())", ServerSideOnly = true)]
		private static double CountPercentsNoAggregate()
		{
			throw new InvalidOperationException("This function should be used only in database code");
		}

		[Test]
		public void CustomAggregate_Having_AsAggregate([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var groupId = 2;

			var query = db.Child
				.GroupBy(c => new
				{
					id = new { c.ChildID },
					group = c.Parent!.ParentID,
				})
				.Having(g => g.Key.group == groupId)
				.Select(g => new
				{
					id = g.Key.id.ChildID,
					reference = (int?)g.Key.group,
					cnt = new
					{
						count = g.LongCount(),
						percents = CountPercentsAggregate()
					}
				})
				.OrderByDescending(_ => _.cnt.count);

			query.ToList().Count().ShouldBe(2);
		}

		[Test]
		public void CustomAggregate_Having_AsWindow([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var groupId = 2;

			var query = db.Child
				.GroupBy(c => new
				{
					id = new { c.ChildID },
					group = c.Parent!.ParentID,
				})
				.Having(g => g.Key.group == groupId)
				.Select(g => new
				{
					id = g.Key.id.ChildID,
					reference = (int?)g.Key.group,
					cnt = new
					{
						count = g.LongCount(),
						percents = CountPercentsWindow()
					}
				})
				.OrderByDescending(_ => _.cnt.count);

			query.ToList().Count().ShouldBe(2);
		}

		[Test]
		public void CustomAggregate_Having_AsExpression([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var groupId = 2;

			var query = db.Child
				.GroupBy(c => new
				{
					id = new { c.ChildID },
					group = c.Parent!.ParentID,
				})
				.Having(g => g.Key.group == groupId)
				.Select(g => new
				{
					id = g.Key.id.ChildID,
					reference = (int?)g.Key.group,
					cnt = new
					{
						count = g.LongCount(),
						percents = CountPercentsNoAggregate()
					}
				})
				.OrderByDescending(_ => _.cnt.count);

			query.ToList().Count().ShouldBe(2);
		}

		[Test]
		public void CustomAggregate_Where_AsAggregate([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var groupId = 2;

			var query = db.Child
				.GroupBy(c => new
				{
					id = new { c.ChildID },
					group = c.Parent!.ParentID,
				})
				.Where(g => g.Key.group == groupId)
				.Select(g => new
				{
					id = g.Key.id.ChildID,
					reference = (int?)g.Key.group,
					cnt = new
					{
						count = g.LongCount(),
						percents = CountPercentsAggregate()
					}
				})
				.OrderByDescending(_ => _.cnt.count);

			query.ToList().Count().ShouldBe(2);
		}

		[Test]
		public void CustomAggregate_Where_AsWindow([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var groupId = 2;

			var query = db.Child
				.GroupBy(c => new
				{
					id = new { c.ChildID },
					group = c.Parent!.ParentID,
				})
				.Where(g => g.Key.group == groupId)
				.Select(g => new
				{
					id = g.Key.id.ChildID,
					reference = (int?)g.Key.group,
					cnt = new
					{
						count = g.LongCount(),
						percents = CountPercentsWindow()
					}
				})
				.OrderByDescending(_ => _.cnt.count);

			query.ToList().Count().ShouldBe(2);
		}

		[Test]
		public void CustomAggregate_Where_AsExpression([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var groupId = 2;

			var query = db.Child
				.GroupBy(c => new
				{
					id = new { c.ChildID },
					group = c.Parent!.ParentID,
				})
				.Where(g => g.Key.group == groupId)
				.Select(g => new
				{
					id = g.Key.id.ChildID,
					reference = (int?)g.Key.group,
					cnt = new
					{
						count = g.LongCount(),
						percents = CountPercentsNoAggregate()
					}
				})
				.OrderByDescending(_ => _.cnt.count);

			query.ToList().Count().ShouldBe(2);
		}

		[Test]
		public void Issue_WithToList([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TestAggregateTable>();

			var id  = TestData.Guid1;
			var id2 = TestData.Guid2;

			var now = TestData.DateTimeOffsetUtc;
			var tz  = "UTC";

			db.Insert(new TestAggregateTable()
			{
				Id          = id,
				ReferenceId = null,
				DateTime    = now
			});

			db.Insert(new TestAggregateTable()
			{
				Id          = id2,
				ReferenceId = id,
				DateTime    = now
			});

			var results = db
				.GetTable<TestAggregateTable>()
				.GroupBy(_ => new
				{
					key   = (Guid?)_.Reference!.Id,
					sort  = _.ReferenceId,
				})
				.Select(_ => _.Key)
				.OrderBy(_ => _.sort)
				.ToList()
				.Select(group => new
				{
					data = db.GetTable<TestAggregateTable>()
						.GroupBy(_ => new
						{
							id    = new { _.Id },
							group = _.Reference!.Id,
							key   = new
							{
								hours   = ByHour(_.DateTime, tz),
								minutes = ByMinute(_.DateTime, tz)
							}
						})
						.Having(_ => _.Key.group == group.key)
						.Select(_ => new
						{
							id        = _.Key.id.Id,
							reference = (Guid?)_.Key.group,
							cnt = new
							{
								count    = _.LongCount(),
								percents = CountPercents()
							},
							x = _.Key.key
						})
						.OrderByDescending(_ => _.cnt.count)
						.ToList()
				}).ToList();

			Assert.That(results, Has.Count.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(results[0].data, Has.Count.EqualTo(1));
				Assert.That(results[1].data, Has.Count.EqualTo(1));
				Assert.That(results[0].data[0].id, Is.EqualTo(id));
				Assert.That(results[0].data[0].reference, Is.Null);
				Assert.That(results[0].data[0].cnt.count, Is.EqualTo(1));
				Assert.That(results[0].data[0].cnt.percents, Is.EqualTo(100));
				Assert.That(results[0].data[0].x.hours, Is.EqualTo(now.Hour));
				Assert.That(results[0].data[0].x.minutes, Is.EqualTo(now.Minute));

				Assert.That(results[1].data[0].id, Is.EqualTo(id2));
				Assert.That(results[1].data[0].reference, Is.EqualTo(id));
				Assert.That(results[1].data[0].cnt.count, Is.EqualTo(1));
				Assert.That(results[1].data[0].cnt.percents, Is.EqualTo(100));
				Assert.That(results[1].data[0].x.hours, Is.EqualTo(now.Hour));
				Assert.That(results[1].data[0].x.minutes, Is.EqualTo(now.Minute));
			}
		}

		[Test]
		public void Issue_WithoutToList([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TestAggregateTable>();

			var id  = TestData.Guid1;
			var id2 = TestData.Guid2;

			var now = TestData.DateTimeOffsetUtc;
			var tz  = "UTC";

			db.Insert(new TestAggregateTable()
			{
				Id          = id,
				ReferenceId = null,
				DateTime    = now
			});

			db.Insert(new TestAggregateTable()
			{
				Id          = id2,
				ReferenceId = id,
				DateTime    = now
			});

			var results = db
				.GetTable<TestAggregateTable>()
				.GroupBy(_ => new
				{
					key   = (Guid?)_.Reference!.Id,
					sort  = _.ReferenceId,
				})
				.Select(_ => _.Key)
				.OrderBy(_ => _.sort)
				.Select(group => new
				{
					data = db.GetTable<TestAggregateTable>()
						.GroupBy(_ => new
						{
							id    = new { _.Id },
							group = _.Reference!.Id,
							key   = new
							{
								hours   = ByHour(_.DateTime, tz),
								minutes = ByMinute(_.DateTime, tz)
							}
						})
						.Having(_ => _.Key.group == group.key)
						.Select(_ => new
						{
							id        = _.Key.id.Id,
							reference = (Guid?)_.Key.group,
							cnt = new
							{
								count    = _.LongCount(),
								percents = CountPercents()
							},
							x = _.Key.key
						})
						.OrderByDescending(_ => _.cnt.count)
						.ToList()
				}).ToList();

			Assert.That(results, Has.Count.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(results[0].data, Has.Count.EqualTo(1));
				Assert.That(results[1].data, Has.Count.EqualTo(1));
				Assert.That(results[0].data[0].id, Is.EqualTo(id));
				Assert.That(results[0].data[0].reference, Is.Null);
				Assert.That(results[0].data[0].cnt.count, Is.EqualTo(1));
				Assert.That(results[0].data[0].cnt.percents, Is.EqualTo(100));
				Assert.That(results[0].data[0].x.hours, Is.EqualTo(now.Hour));
				Assert.That(results[0].data[0].x.minutes, Is.EqualTo(now.Minute));

				Assert.That(results[1].data[0].id, Is.EqualTo(id2));
				Assert.That(results[1].data[0].reference, Is.EqualTo(id));
				Assert.That(results[1].data[0].cnt.count, Is.EqualTo(1));
				Assert.That(results[1].data[0].cnt.percents, Is.EqualTo(100));
				Assert.That(results[1].data[0].x.hours, Is.EqualTo(now.Hour));
				Assert.That(results[1].data[0].x.minutes, Is.EqualTo(now.Minute));
			}
		}

		[Sql.Expression("COUNT_BIG(*) * 100E0 / SUM(COUNT_BIG(*)) OVER()", ServerSideOnly = true, Precedence = Precedence.Multiplicative, IsWindowFunction = true)]
		static double CountPercents()
		{
			throw new InvalidOperationException("This function should be used only in database code");
		}

		[Sql.Expression("DATEPART(minute, {0} AT TIME ZONE {1})", ServerSideOnly = true, IsNullable = Sql.IsNullableType.SameAsFirstParameter)]
		static int? ByMinute(DateTimeOffset? datetime, string tzId)
		{
			return datetime.HasValue ? ByMinute(datetime.Value, tzId) : null;
		}

		[Sql.Expression("DATEPART(hour, {0} AT TIME ZONE {1})", ServerSideOnly = true, IsNullable = Sql.IsNullableType.SameAsFirstParameter)]
		static int? ByHour(DateTimeOffset? datetime, string tzId)
		{
			return datetime.HasValue ? ByHour(datetime.Value, tzId) : null;
		}

		[Table]
		public class TestAggregateTable
		{
			[Column]
			public Guid Id { get; set; }

			[Column]
			public Guid? ReferenceId { get; set; }

			[Column]
			public DateTimeOffset? DateTime { get; set; }

			[Association(ThisKey = nameof(ReferenceId), OtherKey = nameof(Id), CanBeNull = true)]
			public TestAggregateTable? Reference { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2821")]
		public void Issue2821Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var currentDate = TestData.DateTime;

			var query = db.Types2
				.Where(o => (o.DateTimeValue ?? o.DateTimeValue2) <= currentDate && (!o.DateTimeValue2.HasValue || o.DateTimeValue2.Value >= currentDate));

			query = from allowance in query
					join t in (from x in query
							   group x by x.ID into grp
							   select new
							   {
								   ID = grp.Key,
								   DateTimeValue2 = grp.Max(x => x.DateTimeValue2)
							   })
					on new { allowance.ID, allowance.DateTimeValue2 } equals new { t.ID, t.DateTimeValue2 }
					select allowance;

			query = query = query.OrderBy(x => x.DateTimeValue2);

			var result = query.ToList();
			Assert.That(result, Has.Count.EqualTo(12));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4349")]
		public void Issue4349Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent
				.Select(f1 => new { A = 0, B = f1.ParentID })
				.GroupBy(r => new { r.A, r.B })
				.Select(g => new
				{
					A = g.Key.A == 0 ? 0 : 1,
					g.Key.B
				})
				.OrderBy(i => i.A);

			query.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3486")]
		public void Issue3486Test1([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);

			var query = (
				from t in db.Person
				group t by new { t.FirstName, t.LastName } into gr
				select new
				{
					gr.Key.FirstName,
					gr.Key.LastName,
					Sum = gr.Sum(it => it.ID)
				});

			query.ToList();

			db.LastQuery!.ShouldContain("SELECT", Exactly.Once());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3486")]
		public void Issue3486Test2([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);

			var query = (
				from t in db.Person
				group t by new { t.FirstName, t.LastName } into gr
				let common = gr.Key
				select new
				{
					common.FirstName,
					common.LastName,
					Sum = gr.Sum(it => it.ID)
				});

			query.ToList();

			db.LastQuery!.ShouldContain("SELECT", Exactly.Once());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3250")]
		public void Issue3250Test1([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);

			var query = from s in db.Person
						where s.LastName != "ERROR"
						group s by 1 into g
						where g.Count() > 0
						select new
						{
							Message = $"{g.Count()} items have not been processed, e.g. #{g.Min(x => x.ID)}."
						};

			query.ToList();

			db.LastQuery!.ShouldContain("SELECT", Exactly.Once());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3250")]
		public void Issue3250Test2([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);

			var query = db.Person
				.Where(s => s.LastName != "ERROR")
				.Having(_ => Sql.Ext.Count().ToValue() > 0)
				.Select(s => new
				{
					Message = $"{ Sql.Ext.Count().ToValue() } items have not been processed, e.g. #{ Sql.Ext.Min(s.ID).ToValue() }.",
				});

			query.ToList();

			if (context.IsAnyOf(TestProvName.AllAccess))
				db.LastQuery!.ShouldContain("SELECT", Exactly.Twice());
			else
				db.LastQuery!.ShouldContain("SELECT", Exactly.Once());
		}

		[Test]
		public void Issue_HavingConditionTranslation([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var results = db
				.GetTable<Person>()
				.GroupBy(_ => _.MiddleName)
				.Having(_ => _.Key == null || _.Key != "Unknown")
				.Select(_ => new { _.Key, Count = Sql.Ext.Count().ToValue() })
				.ToList();

			var nullValue = results.SingleOrDefault(r => r.Key == null);
			var koValue   = results.SingleOrDefault(r => r.Key == "Ko");
			using (Assert.EnterMultipleScope())
			{
				Assert.That(results, Has.Count.EqualTo(2));
				Assert.That(nullValue, Is.Not.Null);
				Assert.That(nullValue!.Count, Is.EqualTo(3));
				Assert.That(koValue, Is.Not.Null);
			}

			Assert.That(koValue.Count, Is.EqualTo(1));
		}

		[Test]
		public void Issue_PlaceholderDuplicate([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.GetTable<Person>()
				.GroupBy(r => new
				{
					key = r.ID,
					sort = r.ID,
				})
				.Select(r => new
				{
					Key = r.Key.key,
					Sort = r.Key.sort,
					label = "label"
				})
				.OrderBy(r => r.Sort)
				.Take(100)
				.ToList();
		}

		[Test]
		public void Issue_UnusedColumnsElimination([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.GetTable<Person>()
				.GroupBy(r => new
				{
					key = r.ID,
					sort = r.ID,
				})
				.Select(r => new
				{
					Key = r.Key.key,
					Sort = r.Key.sort,
					label = "label"
				})
				.LongCount();
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_OrderBy_in_Derived)]
		[Test]
		public void Issue_FilterByOrderedGroupBy([DataSources] string context)
		{
			using var db  = GetDataContext(context);

			var grp =
			(
				from c in db.Child
				group c by c.ParentID into g
				select new
				{
					ParentID = g.Key,
					Max      = g.Max(x => x.ChildID)
				}
				into g
				orderby g.Max descending
				select g
			)
			.Take(2);

			var query =
				from t in db.Child
				where t.ParentID.In(grp.Select(x => x.ParentID))
				select t;

			AssertQuery(query);
		}

		[Test]
		public void InsertFirstFromGroup([DataSources(false, TestProvName.AllFirebird, TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			using var t1 = db.CreateTempTable("temp_table_1", [new { ID = 1, Value = "Value"}], ed => ed.Property(r => r.ID).IsPrimaryKey(), tableOptions: TableOptions.None);
			using var t2 = db.CreateTempTable("temp_table_2",
				from c in t1
				group c by c.ID into gr
				select new
				{
					gr.First().Value,
				}, ed => ed.Property(r => r.Value).IsPrimaryKey().IsNotNull().HasLength(50), tableOptions: TableOptions.None);
		}

		static class Issue5070
		{
			public sealed class CustomerPrice
			{
				[PrimaryKey] public int CustomerId { get; set; }
				[PrimaryKey] public int FinalCustomerId { get; set; }
				public bool IsActive { get; set; }
				public decimal Price { get; set; }
			}

			public sealed class Inventory
			{
				[PrimaryKey] public int CustomerId { get; set; }
				public decimal Volume { get; set; }
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5070")]
		public void Issue5070Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Issue5070.CustomerPrice>();
			using var t2 = db.CreateLocalTable<Issue5070.Inventory>();

			var forceInactive = true;

			var query = t2
				.InnerJoin(
					t1,
					(v, p) => v.CustomerId == p.CustomerId,
					(v, p) => new
					{
						FinalCustomerId = Sql.NullIf(p.FinalCustomerId, 0) ?? p.CustomerId,
						IsActive = forceInactive ? false : p.IsActive,
						Amount = v.Volume * p.Price
					})
				.GroupBy(t => new { t.FinalCustomerId, t.IsActive })
				.Select(t => new
				{
					t.Key.FinalCustomerId,
					t.Key.IsActive,
					Amount = t.Sum(x => x.Amount)
				});

			query.ToArray();
		}

		static class Issue5317
		{
			[Table]
			public sealed class TestTable
			{
				[PrimaryKey]
				public int Id { get; set; }

				[Column, NotNull]
				public string Name { get; set; } = string.Empty;

				[Column]
				public int ReferenceId { get; set; }

				[Association(ThisKey = nameof(ReferenceId), OtherKey = nameof(Reference.Id), CanBeNull = false)]
				public Reference Reference { get; set; } = null!;
			}

			[Table]
			public sealed class Reference
			{
				[PrimaryKey]
				public int Id { get; set; }

				[Column, NotNull]
				public string Name { get; set; } = string.Empty;
			}
		}

		[ThrowsRequiredOuterJoins(TestProvName.AllAccess, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3, TestProvName.AllSybase)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5317")]
		public void Issue5317Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue5317.TestTable>();
			using var tr = db.CreateLocalTable<Issue5317.Reference>();

			var query = from t1 in tb.LoadWith(x => x.Reference) // for AssertQuery
						join t2 in tb on t1.Id equals t2.Id into g
						from t2 in g.DefaultIfEmpty()
						group new { t1 } by t1.Id into g
						select new
						{
							ReferenceName = g.First().t1.Reference.Name
						};

			AssertQuery(query);
		}

		sealed class Issue5327Table
		{
			[PrimaryKey]
			public int Id    { get; set; }
			public int Key   { get; set; }
			public int Value { get; set; }

			public static readonly Issue5327Table[] Data =
			[
				new() { Id = 1, Key = 2, Value = 1 },
				new() { Id = 2, Key = 2, Value = 2 },
				new() { Id = 3, Key = 2, Value = 3 },
				new() { Id = 4, Key = 2, Value = 4 },
				new() { Id = 5, Key = 1, Value = 5 },
				new() { Id = 6, Key = 1, Value = 6 },
				new() { Id = 7, Key = 1, Value = 7 },
				new() { Id = 8, Key = 1, Value = 8 },
				new() { Id = 9, Key = 3, Value = 9 },
				new() { Id = 10, Key = 3, Value = 10 },
				new() { Id = 11, Key = 3, Value = 11 },
				new() { Id = 12, Key = 3, Value = 12 },
			];
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5327")]
		public void Issue5327Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue5327Table.Data);

			var query = tb
				.Select(c => new { c.Key, c.Value })
				.GroupBy(c => c.Key)
				.Select(c => new { c.Key, Sum = c.Sum(d => d.Value)})
				.OrderByDescending(c => c.Sum)
				.Select(c => c.Key);

			AssertQuery(query);
			Assert.That(query.GetSelectQuery().Select.OrderBy.IsEmpty, Is.Not.True);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5327")]
		public void Issue5327Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue5327Table.Data);

			var query = tb
				.Select(c => new { c.Key, c.Value})
				.GroupBy(c => c.Key)
				.Select(c => new { c.Key, Sum = c.Sum(d => d.Value)})
				.OrderByDescending(c => c.Sum);

			AssertQuery(query);
			Assert.That(query.GetSelectQuery().Select.OrderBy.IsEmpty, Is.Not.True);
		}
	}
}
