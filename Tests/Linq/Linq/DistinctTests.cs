using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class DistinctTests : TestBase
	{
		[Test]
		public void Distinct1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in    Child select ch.ParentID).Distinct(),
					(from ch in db.Child select ch.ParentID).Distinct());
		}

		[Test]
		public void Distinct2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p in    Parent select p.Value1 ?? p.ParentID % 2).Distinct(),
					(from p in db.Parent select p.Value1 ?? p.ParentID % 2).Distinct());
		}

		[Test]
		public void Distinct3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p in    Parent select new { Value = p.Value1 ?? p.ParentID % 2, p.Value1 }).Distinct(),
					(from p in db.Parent select new { Value = p.Value1 ?? p.ParentID % 2, p.Value1 }).Distinct());
		}

		[Test]
		public void Distinct4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from p in    Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = p.Value1 }).Distinct(),
					(from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = p.Value1 }).Distinct());
		}

		[Test]
		public void Distinct5([DataSources] string context, [Values(2, 3)] int id, [Values(0, 1)] int iteration)
		{
			using var db = GetDataContext(context);

			var query = (from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = id + 1 }).Distinct();

			AssertQuery(query);

			var cacheMissCount = query.GetCacheMissCount();

			var query2 = (from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = id + 1 }).Distinct();

			AssertQuery(query);

			query2.GetCacheMissCount().ShouldBe(cacheMissCount);
		}

		[Test]
		public void Distinct6([DataSources] string context, [Values(2, 3)] int id, [Values(0, 1)] int iteration)
		{
			using var db = GetDataContext(context);

			var query = (from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID + id % 2, Value1 = id + 1 }).Distinct();

			var cacheMissCount = query.GetCacheMissCount();

			AssertQuery(query);

			if (iteration > 0)
				query.GetCacheMissCount().ShouldBe(cacheMissCount);
		}

		[Test]
		public void DistinctCount([DataSources] string context)
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

				Assert.That(result.Distinct().Count(), Is.EqualTo(expected.Distinct().Count()));
			}
		}

		[Test]
		public void DistinctMax([DataSources] string context)
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

				Assert.That(result.Distinct().Max(p => p.ParentID), Is.EqualTo(expected.Distinct().Max(p => p.ParentID)));
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_OrderBy_in_Derived)]
		[Test]
		public void TakeDistinct([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in    Child orderby ch.ParentID select ch.ParentID).Take(4).Distinct(),
					(from ch in db.Child orderby ch.ParentID select ch.ParentID).Take(4).Distinct());
		}

		[Test]
		public void DistinctOrderBy([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Child.Select(ch => ch.ParentID).Distinct().OrderBy(ch => ch),
					db.Child.Select(ch => ch.ParentID).Distinct().OrderBy(ch => ch));
		}

		[Test]
		public void DistinctJoin([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q1 = GetTypes(context);
				var q2 = db.Types.Select(_ => new LinqDataTypes {ID = _.ID, SmallIntValue = _.SmallIntValue }).Distinct();

				AreEqual(
					from e in q1
					from p in q1.Where(_ => _.ID == e.ID).DefaultIfEmpty()
					select new { e.ID, p.SmallIntValue },
					from e in q2
					from p in q2.Where(_ => _.ID == e.ID).DefaultIfEmpty()
					select new { e.ID, p.SmallIntValue }
					);
			}
		}

		[ThrowsRequiredOuterJoins(TestProvName.AllAccess, TestProvName.AllDB2, TestProvName.AllSybase, TestProvName.AllInformix)]
		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void AssociationAfterDistinct1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child
				.Distinct()
				.OrderBy(ch => ch.Parent!.Children.Count);

			AssertQuery(query);
		}

		[ThrowsRequiredOuterJoins(TestProvName.AllAccess, TestProvName.AllDB2, TestProvName.AllSybase, TestProvName.AllInformix)]
		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void AssociationAfterDistinct2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child
				.LoadWith(c => c.Parent!.Children) // for AssertQuery
				.Select(c => new { Child = c, Parent = c.Parent })
				.Distinct()
				.OrderBy(r => r.Parent!.Children.Count)
				.Select(r => new { r.Child, r.Parent, Count = r.Parent!.Children.Count });

			AssertQuery(query);
		}

		[Test]
		public void AssociationAfterDistinct3([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child
				.LoadWith(c => c.Parent) // for AssertQuery
				.Distinct()
				.Select(ch => new { ch.ChildID, ch.ParentID, ParentValue = ch.Parent!.Value1 });

			AssertQuery(query);
		}

		[Test]
		public void AssociationAfterDistinctWithWhere([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child
				.LoadWith(c => c.Parent) // for AssertQuery
				.Where(c => c.ChildID > 10)
				.Distinct()
				.OrderBy(ch => ch.Parent!.ParentID)
				.ThenBy(ch => ch.ChildID);

			AssertQuery(query);
		}

		[Test]
		public void AssociationAfterDistinctWithMultipleNavigations([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GrandChild
				.LoadWith(gc => gc.Child!.Parent) // for AssertQuery
				.Distinct()
				.Select(gc => new { gc.GrandChildID, ParentID = gc.Child!.ParentID, GrandParentValue = gc.Child!.Parent!.Value1 })
				.OrderBy(x => x.GrandChildID);

			AssertQuery(query);
		}

		[Test]
		public void AssociationAfterDistinctWithFilter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child
				.LoadWith(c => c.Parent)
				.Distinct()
				.Where(ch => ch.Parent!.ParentID > 1)
				.OrderBy(ch => ch.ChildID);

			AssertQuery(query);
		}

		[Test]
		public void DistinctWithManyToOneAssociation([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child
				.LoadWith(c => c.Parent) // for AssertQuery
				.Select(c => new { c.ChildID, c.Parent })
				.Distinct()
				.OrderBy(r => r.ChildID);

			AssertQuery(query);
		}

		[Test]
		public void DistinctBeforeLoadWith([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child
				.Where(c => c.ParentID < 4)
				.Distinct()
				.LoadWith(c => c.Parent)
				.OrderBy(c => c.ChildID);

			AssertQuery(query);
		}

		[Test]
		public void DistinctWithOneToManyAssociation([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent
				.LoadWith(p => p.Children) // for AssertQuery
				.Select(p => new { p.ParentID, ChildCount = p.Children.Count })
				.Distinct()
				.OrderBy(r => r.ParentID);

			AssertQuery(query);
		}

		[Test]
		public void AssociationAfterDistinctWithGroupBy([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child
				.LoadWith(c => c.Parent) // for AssertQuery
				.Distinct()
				.GroupBy(ch => ch.Parent!.ParentID)
				.Select(g => new { ParentID = g.Key, Count = g.Count() })
				.OrderBy(r => r.ParentID);

			AssertQuery(query);
		}

		[Test]
		public void DistinctAfterAssociationProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child
				.LoadWith(c => c.Parent) // for AssertQuery
				.Select(c => new { c.ChildID, ParentValue = c.Parent!.Value1 })
				.Distinct()
				.OrderBy(r => r.ChildID);

			AssertQuery(query);
		}

		[Test]
		public void MultipleDistinctWithAssociations([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Child
				.LoadWith(c => c.Parent) // for AssertQuery
				.Select(c => c.Parent!.ParentID)
				.Distinct();

			var query2 = db.Parent
				.Where(p => query1.Contains(p.ParentID))
				.Select(p => new { p.ParentID, p.Value1 })
				.Distinct()
				.OrderBy(r => r.ParentID);

			AssertQuery(query2);
		}

		[ThrowsRequiredOuterJoins(TestProvName.AllAccess)]
		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void DistinctWithAssociationInSubquery([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent
				.LoadWith(p => p.Children) // for AssertQuery
				.Where(p => p.Children.Select(c => c.ChildID).Distinct().Count() > 0)
				.Select(p => new { p.ParentID, p.Value1 })
				.OrderBy(r => r.ParentID);

			AssertQuery(query);
		}

		[Test]
		public void DistinctWithNestedAssociationNavigation([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GrandChild
				.LoadWith(gc => gc.Child!.Parent) // for AssertQuery
				.Select(gc => new { gc.GrandChildID, gc.Child!.Parent!.Value1 })
				.Distinct()
				.OrderBy(r => r.GrandChildID);

			AssertQuery(query);
		}

		[Test]
		public void DistinctWithAssociationAndJoin([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = from c in db.Child.LoadWith(c => c.Parent).Distinct()
				join p in db.Parent on c.ParentID equals p.ParentID
				select new { c.ChildID, ParentFromAssociation = c.Parent!.ParentID, ParentFromJoin = p.ParentID };

			AssertQuery(query.OrderBy(r => r.ChildID));
		}

		[Test]
		public void AssociationAfterDistinctWithThenLoad([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Child
				.LoadWith(c => c.Parent)
				.ThenLoad(p => p!.Children)
				.Distinct()
				.OrderBy(c => c.ChildID);

			AssertQuery(query);
		}

		[Test]
		public void DistinctWithMultipleAssociationLevels([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GrandChild
				.LoadWith(gc => gc.Child!.Parent) // for AssertQuery
				.Distinct()
				.Where(gc => gc.Child!.Parent!.ParentID > 0)
				.Select(gc => new { gc.GrandChildID, gc.Child!.ChildID, gc.Child.Parent!.ParentID })
				.OrderBy(r => r.GrandChildID);

			AssertQuery(query);
		}

		[Table]
		public class DistinctOrderByTable
		{
			[PrimaryKey] public int    Id { get; set; }
			[Column]     public int    F1 { get; set; }
			[Column]     public string F2 { get; set; } = null!;
			[Column]     public int    F3 { get; set; }

			public static readonly DistinctOrderByTable[] Data = new[]
			{
				new DistinctOrderByTable() { Id = 8, F1 = 8, F2 = "8", F3 = 5 },
				new DistinctOrderByTable() { Id = 3, F1 = 3, F2 = "3", F3 = 3 },
				new DistinctOrderByTable() { Id = 2, F1 = 2, F2 = "2", F3 = 1 },
				new DistinctOrderByTable() { Id = 6, F1 = 3, F2 = "3", F3 = 4 },
				new DistinctOrderByTable() { Id = 1, F1 = 3, F2 = "3", F3 = 7 },
				new DistinctOrderByTable() { Id = 5, F1 = 5, F2 = "5", F3 = 2 },
				new DistinctOrderByTable() { Id = 7, F1 = 2, F2 = "2", F3 = 8 },
				new DistinctOrderByTable() { Id = 4, F1 = 4, F2 = "4", F3 = 6 },
			};
		}

		[Test]
		public void DistinctOrderBy2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.Select(_ => new { _.F1, _.F2 }).Distinct().OrderByDescending(_ => _.F1).Select(_ => _.F2).ToArray();

				Assert.That(res, Has.Length.EqualTo(5));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo("8"));
					Assert.That(res[1], Is.EqualTo("5"));
					Assert.That(res[2], Is.EqualTo("4"));
					Assert.That(res[3], Is.EqualTo("3"));
					Assert.That(res[4], Is.EqualTo("2"));
				}
			}
		}

		[Test]
		public void DistinctOrderBySkipTake([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.Select(_ => new { _.F1, _.F2 }).Distinct().OrderByDescending(_ => _.F1).Select(_ => _.F2).Skip(1).Take(2).ToArray();

				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo("5"));
					Assert.That(res[1], Is.EqualTo("4"));
				}
			}
		}

		[Test]
		public void DistinctOrderByTake([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.Select(_ => new { _.F1, _.F2 }).Distinct().OrderByDescending(_ => _.F1).Select(_ => _.F2).Take(2).ToArray();

				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo("8"));
					Assert.That(res[1], Is.EqualTo("5"));
				}
			}
		}

		[Test]
		public void DistinctOrderBySkip([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.Select(_ => new { _.F1, _.F2 }).Distinct().OrderByDescending(_ => _.F1).Select(_ => _.F2).Skip(2).ToArray();

				Assert.That(res, Has.Length.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo("4"));
					Assert.That(res[1], Is.EqualTo("3"));
					Assert.That(res[2], Is.EqualTo("2"));
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinct([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).ToArray();

				Assert.That(res, Has.Length.EqualTo(5));
				using (Assert.EnterMultipleScope())
				{
					// ordering optimized out for non-selected column and not preserved
					Assert.That(res, Contains.Item("2"));
					Assert.That(res, Contains.Item("3"));
					Assert.That(res, Contains.Item("4"));
					Assert.That(res, Contains.Item("8"));
					Assert.That(res, Contains.Item("5"));
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctSkipTake([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Select(_ => new { _.F1, _.F2 }).Distinct().OrderBy(_ => _.F1).Select(_ => _.F2).Skip(1).Take(2).ToArray();

				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo("3"));
					Assert.That(res[1], Is.EqualTo("4"));
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctTake([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Select(_ => new { _.F1, _.F2 }).Distinct().OrderBy(_ => _.F1).Select(_ => _.F2).Take(2).ToArray();

				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo("2"));
					Assert.That(res[1], Is.EqualTo("3"));
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctSkip([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Select(_ => new { _.F1, _.F2 }).Distinct().OrderBy(r => r.F1).Select(_ => _.F2).Skip(2).ToArray();

				Assert.That(res, Has.Length.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo("4"));
					Assert.That(res[1], Is.EqualTo("5"));
					Assert.That(res[2], Is.EqualTo("8"));
				}
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_OrderBy_in_Derived)]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllAccess], ErrorMessage = ErrorHelper.Error_Skip_in_Subquery)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctSkipTakeFirst([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Skip(1).Take(4).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).ToArray();

				Assert.That(res, Has.Length.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					// order not preserved
					Assert.That(res, Contains.Item("3"));
					Assert.That(res, Contains.Item("4"));
					Assert.That(res, Contains.Item("8"));
				}
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_OrderBy_in_Derived)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctTakeFirst([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Take(5).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).ToArray();

				Assert.That(res, Has.Length.EqualTo(4));
				using (Assert.EnterMultipleScope())
				{
					// ordering not guaranteed
					Assert.That(res, Contains.Item("2"));
					Assert.That(res, Contains.Item("3"));
					Assert.That(res, Contains.Item("4"));
					Assert.That(res, Contains.Item("8"));
				}
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_OrderBy_in_Derived)]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [ProviderName.Ydb, TestProvName.AllAccess, TestProvName.AllSQLite], ErrorMessage = ErrorHelper.Error_Skip_in_Subquery)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctSkipFirst([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Skip(2).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).ToArray();

				Assert.That(res, Has.Length.EqualTo(5));
				using (Assert.EnterMultipleScope())
				{
					// ordering not preserved
					Assert.That(res, Contains.Item("4"));
					Assert.That(res, Contains.Item("8"));
					Assert.That(res, Contains.Item("3"));
					Assert.That(res, Contains.Item("5"));
					Assert.That(res, Contains.Item("2"));
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void Test2943Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t = db.CreateLocalTable(DistinctOrderByTable.Data);

			var res = t.OrderByDescending(r => r.F1).ThenBy(r => r.F3).ThenBy(r => r.F2).Select(r => new { r.F1, r.F2 }).Distinct().ToArray();

			Assert.That(res, Has.Length.EqualTo(5));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].F2, Is.EqualTo("8"));
				Assert.That(res[1].F2, Is.EqualTo("5"));
				Assert.That(res[2].F2, Is.EqualTo("4"));
				Assert.That(res[3].F2, Is.EqualTo("3"));
				Assert.That(res[4].F2, Is.EqualTo("2"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void Test2943Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t = db.CreateLocalTable(DistinctOrderByTable.Data);

			var res = t.OrderByDescending(r => r.F1).Select(r => new { r.F1, r.F2 }).Distinct().ToArray();

			Assert.That(res, Has.Length.EqualTo(5));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].F2, Is.EqualTo("8"));
				Assert.That(res[1].F2, Is.EqualTo("5"));
				Assert.That(res[2].F2, Is.EqualTo("4"));
				Assert.That(res[3].F2, Is.EqualTo("3"));
				Assert.That(res[4].F2, Is.EqualTo("2"));
			}
		}

		sealed class Level1
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Level2.FK2))]
			public Level2 Lvl2 { get; set; } = null!;

			public static readonly Level1[] Data =
			[
				new Level1() { Id = 1 }
			];
		}

		sealed class Level2
		{
			[PrimaryKey] public int Id { get; set; }
			public int FK2 { get; set; }
			public int FK3 { get; set; }

			[Association(ThisKey = nameof(FK3), OtherKey = nameof(Level3.Id))]
			public Level3 Lvl3 { get; set; } = null!;

			[Association(ThisKey = nameof(FK3), OtherKey = nameof(Level3AllNull.Id))]
			public Level3AllNull Lvl3AllNull { get; set; } = null!;

			public static readonly Level2[] Data =
			[
				new Level2() { Id = 11, FK2 = 1, FK3 = 21 },
				new Level2() { Id = 12, FK2 = 1, FK3 = 21 }
			];
		}

		sealed class Level3
		{
			[PrimaryKey] public int Id { get; set; }
			public int Value { get; set; }

			public static readonly Level3[] Data =
			[
				new Level3() { Id = 21 },
				new Level3() { Id = 22 }
			];
		}

		sealed class Level3AllNull
		{
			public int? Id { get; set; }
			public int? Value { get; set; }

			public static readonly Level3AllNull[] Data =
			[
				new Level3AllNull() { Id = 21 },
				new Level3AllNull() { Id = 22 }
			];
		}

		[Test]
		public void DistinctSelectsUnusedColumn([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(Level1.Data);
			using var t2 = db.CreateLocalTable(Level2.Data);
			using var t3 = db.CreateLocalTable(Level3.Data);

			t1.Select(c => c.Lvl2.Lvl3).Where(p => p.Id == 21).Distinct().Select(p => p.Value).Single();
		}

		[Test]
		public void DistinctSelectsUnusedColumn_Nullable([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(Level1.Data);
			using var t2 = db.CreateLocalTable(Level2.Data);
			using var t3 = db.CreateLocalTable(Level3AllNull.Data);

			t1.Select(c => c.Lvl2.Lvl3AllNull).Where(p => p.Id == 21).Distinct().Select(p => p.Value).Single();
		}
	}
}
