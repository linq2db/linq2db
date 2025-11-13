using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class SetOperatorTests : TestBase
	{
		[Table]
		sealed class SampleData
		{
			[PrimaryKey]
			[Column] public int Id     { get; set; }
			[Column] public int Value1 { get; set; }
			[Column] public int Value2 { get; set; }
			[Column] public int Value3 { get; set; }
		}

		[Test]
		[YdbMemberNotFound]
		public void TestExcept([DataSources] string context)
		{
			var isDistinct = !context.IsAnyOf(TestProvName.AllClickHouse);

			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var rs1 = table.Where(t => t.Id % 2 == 0);
				rs1 = rs1.Concat(rs1);
				var rs2 = table.Where(t => t.Id % 4 == 0);
				var query = rs1.Except(rs2);

				var e1 = testData.Where(t => t.Id % 2 == 0);
				e1 = e1.Concat(e1);
				var e2 = testData.Where(t => t.Id % 4 == 0);
				var expected = e1.Except(e2).ToArray();

				if (!isDistinct)
					query = query.Distinct();

				var actual = query.ToArray();

				AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		[Test]
		[YdbMemberNotFound]
		public void TestExceptAll([DataSources(TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var rs1 = table.Where(t => t.Id % 2 == 0);
				rs1 = rs1.Concat(rs1);
				var rs2 = table.Where(t => t.Id % 4 == 0);
				var query = rs1.ExceptAll(rs2);

				var e1 = testData.Where(t => t.Id % 2 == 0);
				e1 = e1.Concat(e1);
				var e2 = testData.Where(t => t.Id % 4 == 0);
				var expected = e1.Where(e => !e2.Contains(e, ComparerBuilder.GetEqualityComparer<SampleData>())).ToArray();
				var actual = query.ToArray();

				// TODO: emulation is not correct, but pgsql and mysql native implementation working properly
				if (!context.IsAnyOf(TestProvName.AllPostgreSQL, TestProvName.AllMySql8Plus))
					AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		[Test]
		[YdbMemberNotFound]
		public void TestIntersectAll([DataSources(TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var rs1 = table.Where(t => t.Id % 2 == 0);
				rs1 = rs1.Concat(rs1);
				var rs2 = table.Where(t => t.Id % 4 == 0);
				var query = rs1.IntersectAll(rs2);

				var e1 = testData.Where(t => t.Id % 2 == 0);
				e1 = e1.Concat(e1);
				var e2 = testData.Where(t => t.Id % 4 == 0);
				var expected = e1.Where(e => e2.Contains(e, ComparerBuilder.GetEqualityComparer<SampleData>())).ToArray();
				var actual = query.ToArray();

				// TODO: emulation is not correct, but pgsql and mysql native implementation working properly
				if (!context.IsAnyOf(TestProvName.AllPostgreSQL, TestProvName.AllMySql8Plus))
					AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		[Test]
		[YdbMemberNotFound]
		public void TestExceptProjection([DataSources] string context)
		{
			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var rs1 = table.Select(r => new { r.Id, Value = r.Value1 }).Where(t => t.Id % 2 == 0);
				var rs2 = table.Select(r => new { r.Id, Value = r.Value2 / 10 }).Where(t => t.Id % 4 == 0);
				var rs3 = table.Select(r => new { r.Id, Value = r.Value1 }).Where(t => t.Id % 6 == 0);
				var query = rs1.Except(rs2).Except(rs3);

				var e1 = testData.Select(r => new { r.Id, Value = r.Value1 }).Where(t => t.Id % 2 == 0);
				var e2 = testData.Select(r => new { r.Id, Value = r.Value2 / 10 }).Where(t => t.Id % 4 == 0);
				var e3 = testData.Select(r => new { r.Id, Value = r.Value1 }).Where(t => t.Id % 6 == 0);
				var expectedQuery = e1.Except(e2).Except(e3);

				var actual   = query.Select(r => new { r.Value }).ToArray();
				var expected = expectedQuery.Select(r => new { r.Value }).ToArray();

				AreEqual(expected, actual);
			}
		}

		[Test]
		[YdbMemberNotFound]
		public void TestIntersect([DataSources] string context)
		{
			var isDistinct = !context.IsAnyOf(TestProvName.AllClickHouse);

			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var rs1 = table.Where(t => t.Id % 2 == 0);
				rs1 = rs1.Concat(rs1);
				var rs2 = table.Where(t => t.Id % 4 == 0);
				var query = rs1.Intersect(rs2);

				var e1 = testData.Where(t => t.Id % 2 == 0);
				e1 = e1.Concat(e1);
				var e2 = testData.Where(t => t.Id % 4 == 0);
				var expected = e1.Intersect(e2).ToArray();

				if (!isDistinct)
					query = query.Distinct();

				var actual = query.ToArray();

				AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		[Test]
		public void TestUnionAll([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var rs1 = table.Where(t => t.Id % 2 == 0);
				var rs2 = table.Where(t => t.Id % 4 == 0);
				var query = rs1.UnionAll(rs2);

				var e1 = testData.AsQueryable().Where(t => t.Id % 2 == 0);
				var e2 = testData.AsQueryable().Where(t => t.Id % 4 == 0);
				var expected = e1.UnionAll(e2).ToArray();
				var actual = query.ToArray();

				AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		[Test]
		public void TestUnionAllExpr([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from s in table
					from u in table.Where(t => t.Id % 2 == 0).UnionAll(table.Where(t => t.Id % 4 == 0))
						.InnerJoin(u => u.Id == s.Id)
					select u;

				var e1 = testData.AsQueryable().Where(t => t.Id % 2 == 0);
				var e2 = testData.AsQueryable().Where(t => t.Id % 4 == 0);
				var expected = e1.UnionAll(e2).ToArray();
				var actual = query.ToArray();
				
				AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		private SampleData[] GenerateTestData()
		{
			return Enumerable.Range(1, 10)
				.Select(i => new SampleData
				{
					Id = i,
					Value1 = i * 10,
					Value2 = i * 100,
					Value3 = i * 1000
				})
				.ToArray();
		}

		[Test]
		public async Task Issue3132Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Person
					.Where(x => x.MiddleName != null)
					.GroupBy(x => x.MiddleName)
					.Select(grp => new
					{
						grp.Key,
						Count = grp.Count()
					});

				var unionResult = await query1
					.UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1)
					.UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1)
					.UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1)
					.UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1).UnionAll(query1)
					.ToArrayAsync();
			}
		}
	}
}
