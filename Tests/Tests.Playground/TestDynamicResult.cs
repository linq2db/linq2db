using System.Linq;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class TestDynamicResult : TestBase
	{
		[Table]
		class RawDynamicData
		{
			[Column] public int AId { get; set; }
			[Column] public int AValue { get; set; }

			[Column] public int BId { get; set; }
			[Column] public int BValue { get; set; }

			public static RawDynamicData[] Seed()
			{
				return Enumerable.Range(1, 20)
					.Select(i => new RawDynamicData { AId = i, BId = i * 100, AValue = i * 2, BValue = i * 100 * 2 })
					.ToArray();
			}
		}

		[Test]
		public void DynamicQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var data = RawDynamicData.Seed();

			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var result = db.Query<dynamic>("select * from RawDynamicData").ToList();
			}
		}
	}
}
