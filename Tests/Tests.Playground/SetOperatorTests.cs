using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.Playground
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
		public void TestExcept([DataSources(ProviderName.Access, ProviderName.SqlCe)] string context)
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
		public void TestIntersect([DataSources(ProviderName.Access, ProviderName.SqlCe)] string context)
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
