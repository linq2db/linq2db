using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class SetOperatorTests : TestBase
	{
		[Table]
		class SampleData
		{
			[PrimaryKey]
			[Column] public int Id     { get; set; }
			[Column] public int Value1 { get; set; }
			[Column] public int Value2 { get; set; }
			[Column] public int Value3 { get; set; }
		}

		[Test]
		public void TestExcept([DataSources] string context)
		{
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
				var actual = query.ToArray();

				AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		[Test]
		public void TestExceptAll([DataSources()] string context)
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

				if (!context.Contains(ProviderName.PostgreSQL)) // postgres has a bug?
					AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		[Test]
		public void TestIntersectAll([DataSources] string context)
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

				if (!context.Contains(ProviderName.PostgreSQL)) // postgres has a bug?
					AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		[Test]
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
		public void TestIntersect([DataSources] string context)
		{
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
				var actual = query.ToArray();

				AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		[Test]
		public void TestUnionAll([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void TestUnionAllExpr([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
	}
}
