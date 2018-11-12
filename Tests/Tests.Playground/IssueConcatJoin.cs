using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class IssueConcatJoin : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void ConcatJoinTest([DataSources] string context)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = from m in table
					from id in table.Where(t => t.Id == 1).Select(t => t.Id)
						.Concat(table.Where(t => t.Id == 2).Select(t => t.Id))
						.InnerJoin(id => id == m.Id)
					select m;

				var expected = from m in table
					from id in table.Where(t => t.Id == 1).Select(t => t.Id)
						.Concat(table.Where(t => t.Id == 2).Select(t => t.Id))
						.Where(id => id == m.Id)
					select m;

				AreEqual(expected, query);
			}
		}

		[Test]
		public void ConcatJoinTestChain([DataSources] string context)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				var query = table.InnerJoin(
					table.Where(t => t.Id == 1).Select(t => t.Id)
						.Concat(table.Where(t => t.Id == 2).Select(t => t.Id)),
					(m, id) => m.Id == id, (m, id) => m);

				var expected = from m in table
					from id in table.Where(t => t.Id == 1).Select(t => t.Id)
						.Concat(table.Where(t => t.Id == 2).Select(t => t.Id))
						.Where(id => id == m.Id)
					select m;

				AreEqual(expected, query);
			}
		}

		private static SampleClass[] GenerateTestData()
		{
			var testData = new[]
			{
				new SampleClass { Id = 1, Value = 10 },
				new SampleClass { Id = 2, Value = 20 },
				new SampleClass { Id = 3, Value = 30 },
				new SampleClass { Id = 4, Value = 40 },
			};
			return testData;
		}
	}
}
