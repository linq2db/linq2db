using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class PaginationTests : TestBase
	{
		[Table]
		sealed class PaginationData
		{
			[Column] public int Id { get; set; }
			[Column] public int Value { get; set; }

			public static PaginationData[] Seed()
			{
				return Enumerable.Range(0, 300)
					.Select(x => new PaginationData { Id = x, Value = x * 33 })
					.ToArray();
			}
		}

		[Test]
		public void Tests([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var data = PaginationData.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query = table.Where(x => x.Id %2 == 0).OrderBy(x => x.Id).ThenByDescending(x => x.Value);
				var pageSize = 20;
				var pagination1 = query.Paginate(1, pageSize);
				var pagination2 = query.Paginate(2, pageSize, true);

				var byKey1 = query.GetPageByCondition(pageSize, x => x.Id == pagination1.Items[1].Id);
				var byKey2 = query.GetPageByCondition(pageSize, x => x.Id == pagination2.Items[pageSize - 1].Id, true);

				Assert.Multiple(() =>
				{
					Assert.That(byKey1.Page, Is.EqualTo(pagination1.Page));
					Assert.That(byKey1.TotalCount, Is.EqualTo(pagination1.TotalCount));

					Assert.That(byKey2.Page, Is.EqualTo(pagination2.Page));
					Assert.That(byKey2.TotalCount, Is.EqualTo(pagination2.TotalCount));
				});

				var pageNumber1 = query.GetPageNumberByCondition(pageSize, x => x.Id == pagination1.Items[1].Id);
				var pageNumber2 = query.GetPageNumberByCondition(pageSize, x => x.Id == pagination2.Items[pageSize - 1].Id, true);
				Assert.Multiple(() =>
				{
					Assert.That(pageNumber1, Is.EqualTo(pagination1.Page));
					Assert.That(pageNumber2, Is.EqualTo(pagination2.Page));
				});

				AreEqualWithComparer(pagination1.Items, byKey1.Items);
				AreEqualWithComparer(pagination2.Items, byKey2.Items);
			}
		}

		[Test]
		public async Task TestsAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var data = PaginationData.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query = table.Where(x => x.Id %2 == 0).OrderBy(x => x.Id).ThenByDescending(x => x.Value);
				var pageSize = 20;
				var pagination1 = await query.PaginateAsync(1, pageSize);
				var pagination2 = await query.PaginateAsync(2, pageSize, true);

				var byKey1 = await query.GetPageByConditionAsync(pageSize, x => x.Id == pagination1.Items[1].Id);
				var byKey2 = await query.GetPageByConditionAsync(pageSize, x => x.Id == pagination2.Items[pageSize - 1].Id, true);

				Assert.Multiple(() =>
				{
					Assert.That(byKey1.Page, Is.EqualTo(pagination1.Page));
					Assert.That(byKey1.TotalCount, Is.EqualTo(pagination1.TotalCount));

					Assert.That(byKey2.Page, Is.EqualTo(pagination2.Page));
					Assert.That(byKey2.TotalCount, Is.EqualTo(pagination2.TotalCount));
				});

				var pageNumber1 = await query.GetPageNumberByConditionAsync(pageSize, x => x.Id == pagination1.Items[1].Id);
				var pageNumber2 = await query.GetPageNumberByConditionAsync(pageSize, x => x.Id == pagination2.Items[pageSize - 1].Id, true);

				Assert.Multiple(() =>
				{
					Assert.That(pageNumber1, Is.EqualTo(pagination1.Page));
					Assert.That(pageNumber2, Is.EqualTo(pagination2.Page));
				});

				AreEqualWithComparer(pagination1.Items, byKey1.Items);
				AreEqualWithComparer(pagination2.Items, byKey2.Items);
			}
		}

		[Test]
		public void ApplyOrderBy([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var data = PaginationData.Seed();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query = table.Where(x => x.Id %2 == 0).ApplyOrderBy(new []{Tuple.Create("Id", false), Tuple.Create("Value", true)});
				var pageSize = 20;
				var pagination1 = query.Paginate(1, pageSize);
				var pagination2 = query.Paginate(2, pageSize, true);

				var byKey1 = query.GetPageByCondition(pageSize, x => x.Id == pagination1.Items[1].Id);
				var byKey2 = query.GetPageByCondition(pageSize, x => x.Id == pagination2.Items[pageSize - 1].Id, true);

				Assert.Multiple(() =>
				{
					Assert.That(byKey1.Page, Is.EqualTo(pagination1.Page));
					Assert.That(byKey1.TotalCount, Is.EqualTo(pagination1.TotalCount));

					Assert.That(byKey2.Page, Is.EqualTo(pagination2.Page));
					Assert.That(byKey2.TotalCount, Is.EqualTo(pagination2.TotalCount));
				});

				var pageNumber1 = query.GetPageNumberByCondition(pageSize, x => x.Id == pagination1.Items[1].Id);
				var pageNumber2 = query.GetPageNumberByCondition(pageSize, x => x.Id == pagination2.Items[pageSize - 1].Id, true);

				Assert.Multiple(() =>
				{
					Assert.That(pageNumber1, Is.EqualTo(pagination1.Page));
					Assert.That(pageNumber2, Is.EqualTo(pagination2.Page));
				});

				AreEqualWithComparer(pagination1.Items, byKey1.Items);
				AreEqualWithComparer(pagination2.Items, byKey2.Items);
			}
		}
	}
}
