using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5382Tests : TestBase
	{
		[Table]
		public sealed class SampleTable
		{
			[Column] public int IntColumn { get; set; }
			[Column] public DateTime DateColumn { get; set; }
			[Column] public decimal Value { get; set; }
		}

		[Test]
		public async Task DeleteFromCteWithWindowFunction([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			await using var db = GetDataContext(context);

			// Insert test data - multiple rows with same IntColumn to test ROW_NUMBER partitioning
			await using var tmp = db.CreateLocalTable(
			[
				new SampleTable { IntColumn = 1, DateColumn = new DateTime(2024, 1, 1), Value = 100 },
				new SampleTable { IntColumn = 1, DateColumn = new DateTime(2024, 1, 2), Value = 100 }, // Duplicate value - should be deleted
				new SampleTable { IntColumn = 1, DateColumn = new DateTime(2024, 1, 3), Value = 150 },
				new SampleTable { IntColumn = 2, DateColumn = new DateTime(2024, 1, 1), Value = 200 },
				new SampleTable { IntColumn = 2, DateColumn = new DateTime(2024, 1, 2), Value = 200 }, // Duplicate value - should be deleted
				new SampleTable { IntColumn = 2, DateColumn = new DateTime(2024, 1, 3), Value = 250 }
			]);

			// Create CTE with ROW_NUMBER window function
			var cte = tmp
				.Select(sample => new
				{
					Sample = sample,
					RowNum = Sql.Ext.RowNumber().Over().PartitionBy(sample.IntColumn).OrderBy(sample.DateColumn).ToValue()
				})
				.AsCte();

			// Delete rows where consecutive values are the same
			var deleted = await cte
				.InnerJoin(cte,
					(next, prev) => prev.Sample.IntColumn == next.Sample.IntColumn &&
					                prev.RowNum == next.RowNum - 1,
					(next, prev) => new { Previous = prev, Next = next })
				.Where(query => query.Previous.Sample.Value == query.Next.Sample.Value)
				.Select(query => query.Next)
				.DeleteAsync();

			deleted.ShouldBe(2);

			// Verify remaining rows
			var remaining = await tmp.OrderBy(x => x.IntColumn).ThenBy(x => x.DateColumn).ToListAsync();
			remaining.Count.ShouldBe(4);
			
			// Should have kept first occurrence and rows with different values
			remaining[0].IntColumn.ShouldBe(1);
			remaining[0].DateColumn.ShouldBe(new DateTime(2024, 1, 1));
			remaining[0].Value.ShouldBe(100);
			
			remaining[1].IntColumn.ShouldBe(1);
			remaining[1].DateColumn.ShouldBe(new DateTime(2024, 1, 3));
			remaining[1].Value.ShouldBe(150);
			
			remaining[2].IntColumn.ShouldBe(2);
			remaining[2].DateColumn.ShouldBe(new DateTime(2024, 1, 1));
			remaining[2].Value.ShouldBe(200);
			
			remaining[3].IntColumn.ShouldBe(2);
			remaining[3].DateColumn.ShouldBe(new DateTime(2024, 1, 3));
			remaining[3].Value.ShouldBe(250);
		}

		[Test]
		public async Task UpdateViaCteWithWindowFunction([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			await using var db = GetDataContext(context);

			// Test data with values to update
			await using var tmp = db.CreateLocalTable(
			[
				new SampleTable { IntColumn = 1, DateColumn = new DateTime(2024, 1, 1), Value = 100 },
				new SampleTable { IntColumn = 1, DateColumn = new DateTime(2024, 1, 2), Value = 200 },
				new SampleTable { IntColumn = 2, DateColumn = new DateTime(2024, 1, 1), Value = 300 },
				new SampleTable { IntColumn = 2, DateColumn = new DateTime(2024, 1, 2), Value = 400 }
			]);

			// Create CTE with ROW_NUMBER window function - get first row in each partition
			var cte = tmp
				.Select(sample => new
				{
					Sample = sample,
					RowNum = Sql.Ext.RowNumber().Over().PartitionBy(sample.IntColumn).OrderBy(sample.DateColumn).ToValue()
				})
				.Where(x => x.RowNum == 1)
				.AsCte();

			// Update rows by joining with CTE - multiply value by 10 for first rows in each partition
			var updated = await tmp
				.InnerJoin(cte,
					(target, source) => target.IntColumn == source.Sample.IntColumn && 
					                    target.DateColumn == source.Sample.DateColumn,
					(target, source) => target)
				.UpdateAsync(t => new SampleTable { Value = t.Value * 10 });

			updated.ShouldBe(2);

			// Verify updated rows
			var result = await tmp.OrderBy(x => x.IntColumn).ThenBy(x => x.DateColumn).ToListAsync();
			result.Count.ShouldBe(4);

			// First rows should be updated (multiplied by 10)
			result[0].IntColumn.ShouldBe(1);
			result[0].DateColumn.ShouldBe(new DateTime(2024, 1, 1));
			result[0].Value.ShouldBe(1000);

			result[1].IntColumn.ShouldBe(1);
			result[1].DateColumn.ShouldBe(new DateTime(2024, 1, 2));
			result[1].Value.ShouldBe(200);

			result[2].IntColumn.ShouldBe(2);
			result[2].DateColumn.ShouldBe(new DateTime(2024, 1, 1));
			result[2].Value.ShouldBe(3000);

			result[3].IntColumn.ShouldBe(2);
			result[3].DateColumn.ShouldBe(new DateTime(2024, 1, 2));
			result[3].Value.ShouldBe(400);
		}
	}
}
