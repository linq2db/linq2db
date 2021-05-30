using System.Linq;

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

		[ActiveIssue("CI: SQL0418N  The statement was not processed because the statement contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword, or a null", Configuration = ProviderName.DB2)]
		[Test]
		public void Distinct5([DataSources] string context)
		{
			var id = 2;

			using (var db = GetDataContext(context))
				AreEqual(
					(from p in    Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = id + 1 }).Distinct(),
					(from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID % 2, Value1 = id + 1 }).Distinct());
		}

		[ActiveIssue("CI: SQL0418N  The statement was not processed because the statement contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword, or a null", Configuration = ProviderName.DB2)]
		[Test]
		public void Distinct6([DataSources(TestProvName.AllInformix)] string context)
		{
			var id = 2;

			using (var db = GetDataContext(context))
				AreEqual(
					(from p in    Parent select new Parent { ParentID = p.Value1 ?? p.ParentID + id % 2, Value1 = id + 1 }).Distinct(),
					(from p in db.Parent select new Parent { ParentID = p.Value1 ?? p.ParentID + id % 2, Value1 = id + 1 }).Distinct());
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

				Assert.AreEqual(expected.Distinct().Count(), result.Distinct().Count());
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

				Assert.AreEqual(expected.Distinct().Max(p => p.ParentID), result.Distinct().Max(p => p.ParentID));
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

			public static readonly DistinctOrderByTable[] Data = new[]
			{
				new DistinctOrderByTable() { Id = 8, F1 = 8, F2 = "8" },
				new DistinctOrderByTable() { Id = 3, F1 = 3, F2 = "3" },
				new DistinctOrderByTable() { Id = 2, F1 = 2, F2 = "2" },
				new DistinctOrderByTable() { Id = 6, F1 = 3, F2 = "3" },
				new DistinctOrderByTable() { Id = 1, F1 = 3, F2 = "3" },
				new DistinctOrderByTable() { Id = 5, F1 = 5, F2 = "5" },
				new DistinctOrderByTable() { Id = 7, F1 = 2, F2 = "2" },
				new DistinctOrderByTable() { Id = 4, F1 = 4, F2 = "4" },
			};
		}

		[Test]
		public void DistinctOrderBy2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.Select(_ => new { _.F1, _.F2 }).Distinct().OrderByDescending(_ => _.F1).Select(_ => _.F2).ToArray();

				Assert.AreEqual(5, res.Length);
				Assert.AreEqual("8", res[0]);
				Assert.AreEqual("5", res[1]);
				Assert.AreEqual("4", res[2]);
				Assert.AreEqual("3", res[3]);
				Assert.AreEqual("2", res[4]);
			}
		}

		[Test]
		public void DistinctOrderBySkipTake([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.Select(_ => new { _.F1, _.F2 }).Distinct().OrderByDescending(_ => _.F1).Select(_ => _.F2).Skip(1).Take(2).ToArray();

				Assert.AreEqual(2, res.Length);
				Assert.AreEqual("5", res[0]);
				Assert.AreEqual("4", res[1]);
			}
		}

		[Test]
		public void DistinctOrderByTake([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.Select(_ => new { _.F1, _.F2 }).Distinct().OrderByDescending(_ => _.F1).Select(_ => _.F2).Take(2).ToArray();

				Assert.AreEqual(2, res.Length);
				Assert.AreEqual("8", res[0]);
				Assert.AreEqual("5", res[1]);
			}
		}

		[Test]
		public void DistinctOrderBySkip([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var t = db.CreateLocalTable(DistinctOrderByTable.Data))
			{
				var res = t.Select(_ => new { _.F1, _.F2 }).Distinct().OrderByDescending(_ => _.F1).Select(_ => _.F2).Skip(2).ToArray();

				Assert.AreEqual(3, res.Length);
				Assert.AreEqual("4", res[0]);
				Assert.AreEqual("3", res[1]);
				Assert.AreEqual("2", res[2]);
			}
		}
	}
}
