using System.Dynamic;
using System.Linq;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class DynamicResultTests : TestBase
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
		public void DynamicQueryViaDynamic([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(1, 10)] int param)
		{
			var data = RawDynamicData.Seed();

			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable(data))
			{
				var result = db.Query<dynamic>("select * from RawDynamicData where AId >= @param", new {param = param}).ToList();

				var casted = result.Select(x =>
						new RawDynamicData
						{
							AId = (int)x.AId,
							AValue = (int)x.AValue,
							BId = (int)x.BId,
							BValue = (int)x.BValue
						})
					.ToArray();

				AreEqualWithComparer(data.Where(x => x.AId >= param), casted);
			}
		}

		[Test]
		public void DynamicQueryViaObject([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(1, 10)] int param)
		{
			var data = RawDynamicData.Seed();

			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable(data))
			{
				var result = db.Query<object>("select * from RawDynamicData where AId >= @param", new {param = param}).ToList();

				var casted = result.OfType<dynamic>().Select(x =>
						new RawDynamicData
						{
							AId = (int)x.AId,
							AValue = (int)x.AValue,
							BId = (int)x.BId,
							BValue = (int)x.BValue
						})
					.ToArray();

				AreEqualWithComparer(data.Where(x => x.AId >= param), casted);
			}
		}

	}
}
