using System;
using System.Collections.Generic;
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
		sealed class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void SelectQueryFromList([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
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

		[Test]
		public void FieldProjection([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var collection = db.Person.ToList();
				var query = db.Person
					.Select(x => collection.First(r => r.ID == x.ID).ID);

				AssertQuery(query);
			}
		}

		[Test]
		public void AnonymousProjection([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var collection = db.Parent.Select(_ => new { _.ParentID }).ToList();

				var query = db.Parent
					.Select(_ => new
					{
						Children = collection.Where(c1 => c1.ParentID == _.ParentID).ToArray()
					});

				AssertQuery(query);
			}
		}

		[Test]
		public void EnumerableAsQueryable([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var resultQuery = Array.Empty<Model.Person>().AsQueryable();

			var query = db.GetTable<Model.Person>()
				.Where(_ => !resultQuery.Select(m => m.ID).Contains(_.ID));

			AssertQuery(query);
		}
	}
}
