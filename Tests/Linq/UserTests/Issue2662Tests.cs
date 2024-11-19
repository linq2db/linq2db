using System.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2662Tests : TestBase
	{
		[Table(Name = "CountDistinctTest")]
		public class TestRow
		{
			[Column, PrimaryKey]
			public int ID { get; set; }

			[Column]
			public int GroupCol { get; set; }

			[Column]
			public int LinkCol { get; set; }

			[Column]
			public int? NotUsed { get; set; }

			public static TestRow[] MakeTestData()
			{
				return Enumerable.Range(0, 100)
					.Select(i => new TestRow { ID = i, GroupCol = i / 20, LinkCol = i % 5 })
					.ToArray();
			}
		}
		[Test]
		public void GroupPropertyValueAndDistinct([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var testData = TestRow.MakeTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query =
					from row in table
					group row.LinkCol by row.GroupCol
					into grp
					select new {grp.Key, c1 = grp.Count(), c2 = grp.Distinct().Count()};

				var expected =
					from row in testData
					group row.LinkCol by row.GroupCol
					into grp
					select new {grp.Key, c1 = grp.Count(), c2 = grp.Distinct().Count()};

				Assert.That(query, Is.EqualTo(expected));
			}
		}
	}
}
