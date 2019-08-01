using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class SetOperatorTests : TestBase
	{
		class SupportedSourcesAttribute : DataSourcesAttribute
		{
			public SupportedSourcesAttribute() : base(ProviderName.Access, ProviderName.SqlCe, TestProvName.AllFirebird, TestProvName.AllMySql)
			{
			}
		}

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
		public void TestExcept([SupportedSources] string context)
		{
			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var rs1 = table.Where(t => t.Id % 2 == 0);
				var rs2 = table.Where(t => t.Id % 4 == 0);
				var query = rs1.Except(rs2);

				var expected = testData.Where(t => t.Id % 2 == 0).Except(testData.Where(t => t.Id % 4 == 0)).ToArray();
				var actual = query.ToArray();

				AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<SampleData>());
			}
		}

		[Test]
		public void TestExceptProjection([SupportedSources] string context)
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
		public void TestIntersect([SupportedSources] string context)
		{
			var testData = GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var rs1 = table.Where(t => t.Id % 2 == 0);
				var rs2 = table.Where(t => t.Id % 4 == 0);
				var query = rs1.Intersect(rs2);

				var expected = testData.Where(t => t.Id % 2 == 0).Intersect(testData.Where(t => t.Id % 4 == 0)).ToArray();
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
