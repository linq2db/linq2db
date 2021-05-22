using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class EnumerableInQuery : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void SelectQueryFromList([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var items = new[]
			{
				new SampleClass { Id = 1, Value = 11 }, new SampleClass { Id = 2, Value = 22 },
				new SampleClass { Id = 3, Value = 33 }, new SampleClass { Id = 4, Value = 44 }
			};
			using (var db = GetDataContext(context))
			{

				IQueryable<SampleClass>? itemsQuery = null;

				for (int i = 0; i < items.Length; i++)
				{
					var item = items[i];
					var current = i % 2 == 0
						? db.SelectQuery(() => new SampleClass
						{
							Id = item.Id,
							Value = item.Value,
						})
						: db.SelectQuery(() => new SampleClass
						{
							Value = item.Value,
							Id = item.Id,
						});

					itemsQuery = itemsQuery == null ? current : itemsQuery.Concat(current);
				}

				var result = itemsQuery!.AsCte().ToArray();

				AreEqual(items, result, ComparerBuilder.GetEqualityComparer<SampleClass>());
			}
		}
	}
}
