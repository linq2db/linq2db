using System.Linq;

using FluentAssertions;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Mapping;

	using Model;

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
		public void Distinct5([DataSources(TestProvName.AllInformix)] string context, [Values(2, 3)] int id, [Values(0, 1)] int iteration)
		{
			using var db = GetDataContext(context);

			var query = (from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = id + 1 }).Distinct();

			AssertQuery(query);

			var cacheMissCount = query.GetCacheMissCount();

			var query2 = (from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = id + 1 }).Distinct();

			AssertQuery(query);

			query2.GetCacheMissCount().Should().Be(cacheMissCount);
		}

		[Test]
		public void Distinct6([DataSources(TestProvName.AllInformix)] string context, [Values(2, 3)] int id, [Values(0, 1)] int iteration)
		{
			using var db = GetDataContext(context);

			var query = (from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID + id % 2, Value1 = id + 1 }).Distinct();

			var cacheMissCount = query.GetCacheMissCount();

			AssertQuery(query);

			if (iteration > 0)
				query.GetCacheMissCount().Should().Be(cacheMissCount);
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

		[Test]
		public void TakeDistinct([DataSources(TestProvName.AllSybase, TestProvName.AllSQLite)] string context)
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
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("8"));
					Assert.That(res[1], Is.EqualTo("5"));
					Assert.That(res[2], Is.EqualTo("4"));
					Assert.That(res[3], Is.EqualTo("3"));
					Assert.That(res[4], Is.EqualTo("2"));
				});
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
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("5"));
					Assert.That(res[1], Is.EqualTo("4"));
				});
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
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("8"));
					Assert.That(res[1], Is.EqualTo("5"));
				});
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
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("4"));
					Assert.That(res[1], Is.EqualTo("3"));
					Assert.That(res[2], Is.EqualTo("2"));
				});
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinct([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).ToArray();

				Assert.That(res, Has.Length.EqualTo(5));
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("2"));
					Assert.That(res[1], Is.EqualTo("3"));
					Assert.That(res[2], Is.EqualTo("4"));
					Assert.That(res[3], Is.EqualTo("8"));
					Assert.That(res[4], Is.EqualTo("5"));
				});
			}
		}

		[ActiveIssue(Configurations = [TestProvName.AllClickHouse, TestProvName.AllMySql, TestProvName.AllPostgreSQL, TestProvName.AllSapHana, TestProvName.AllSQLite, TestProvName.AllSybase])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctSkipTake([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).Skip(1).Take(2).ToArray();

				Assert.That(res, Has.Length.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("3"));
					Assert.That(res[1], Is.EqualTo("4"));
				});
			}
		}

		[ActiveIssue(Configurations = [TestProvName.AllClickHouse, TestProvName.AllMySql, TestProvName.AllPostgreSQL, TestProvName.AllSQLite, TestProvName.AllSybase, TestProvName.AllSapHana])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctTake([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).Take(2).ToArray();

				Assert.That(res, Has.Length.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("2"));
					Assert.That(res[1], Is.EqualTo("3"));
				});
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctSkip([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).Skip(2).ToArray();

				Assert.That(res, Has.Length.EqualTo(3));
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("4"));
					Assert.That(res[1], Is.EqualTo("8"));
					Assert.That(res[2], Is.EqualTo("5"));
				});
			}
		}

		[ActiveIssue(Configurations = [TestProvName.AllAccess, TestProvName.AllOracle, TestProvName.AllSapHana, TestProvName.AllSybase])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctSkipTakeFirst([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Skip(1).Take(4).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).ToArray();

				Assert.That(res, Has.Length.EqualTo(3));
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("3"));
					Assert.That(res[1], Is.EqualTo("4"));
					Assert.That(res[2], Is.EqualTo("8"));
				});
			}
		}

		[ActiveIssue(Configurations = [TestProvName.AllOracle, TestProvName.AllSapHana, TestProvName.AllSybase])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctTakeFirst([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Take(5).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).ToArray();

				Assert.That(res, Has.Length.EqualTo(4));
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("2"));
					Assert.That(res[1], Is.EqualTo("3"));
					Assert.That(res[2], Is.EqualTo("4"));
					Assert.That(res[3], Is.EqualTo("8"));
				});
			}
		}

		[ActiveIssue(Configurations = [TestProvName.AllAccess, TestProvName.AllDB2, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllSapHana, ProviderName.SqlCe, TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllSybase])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2943")]
		public void OrderByDistinctSkipFirst([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.OrderByDescending(_ => _.F3).Skip(2).Select(_ => new { _.F1, _.F2 }).Distinct().Select(_ => _.F2).ToArray();

				Assert.That(res, Has.Length.EqualTo(5));
				Assert.Multiple(() =>
				{
					Assert.That(res[0], Is.EqualTo("4"));
					Assert.That(res[1], Is.EqualTo("8"));
					Assert.That(res[2], Is.EqualTo("3"));
					Assert.That(res[3], Is.EqualTo("5"));
					Assert.That(res[4], Is.EqualTo("2"));
				});
			}
		}
	}
}
