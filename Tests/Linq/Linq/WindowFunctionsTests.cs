using System;
using System.Linq;

using FluentAssertions;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class WindowFunctionsTests : TestBase
	{
		public class WindowFunctionTestEntity
		{
			public int Id { get; set; }
			public string? Name { get; set; }
			public int CategoryId { get; set; }
			public double? Value { get; set; }
			public DateTime? Timestamp { get; set; }

			public override string ToString()
			{
				return $"Id: {Id}, Name: {Name}, CategoryId: {CategoryId}, Value: {Value}, Timestamp: {Timestamp}";
			}

			public static WindowFunctionTestEntity[] Seed()
			{
				return
				[
					new WindowFunctionTestEntity { Id = 1, Name = "Alice", CategoryId   = 1, Value = 10.5, Timestamp = new DateTime(2024, 1, 1, 9, 0, 0) },
					new WindowFunctionTestEntity { Id = 2, Name = "Bob", CategoryId     = 1, Value = 15.0, Timestamp = new DateTime(2024, 1, 1, 10, 0, 0) },
					new WindowFunctionTestEntity { Id = 3, Name = "Charlie", CategoryId = 2, Value = 8.0,  Timestamp = new DateTime(2024, 1, 2, 11, 0, 0) },
					new WindowFunctionTestEntity { Id = 4, Name = "Diana", CategoryId   = 2, Value = 12.5, Timestamp = new DateTime(2024, 1, 2, 12, 0, 0) },
					new WindowFunctionTestEntity { Id = 5, Name = "Eve", CategoryId     = 1, Value = 18.5, Timestamp = new DateTime(2024, 1, 3, 13, 0, 0) },
					new WindowFunctionTestEntity { Id = 6, Name = "Frank", CategoryId   = 3, Value = 20.0, Timestamp = new DateTime(2024, 1, 3, 14, 0, 0) },
					new WindowFunctionTestEntity { Id = 7, Name = "Grace", CategoryId   = 3, Value = 25.0, Timestamp = new DateTime(2024, 1, 4, 15, 0, 0) },
					new WindowFunctionTestEntity { Id = 8, Name = "Hank", CategoryId    = 1, Value = 30.0, Timestamp = new DateTime(2024, 1, 4, 16, 0, 0) },
					new WindowFunctionTestEntity { Id = 9, Name = null, CategoryId      = 1, Value = null, Timestamp = null }
				];
			}
		}

		[Test]
		public void RowNumberWithMultiplePartitions([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp)),
					rn2    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Value)),
					rn3    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp)),
					rn4    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Value)),
					rn5    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp).ThenBy(x.Value)),
					rn6    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

			var expected = new[]
			{
				new { Id = 1, rn1 = 1, rn2 = 1, rn3 = 1, rn4 = 1, rn5 = 1, rn6 = 1 },
				new { Id = 2, rn1 = 1, rn2 = 1, rn3 = 1, rn4 = 1, rn5 = 1, rn6 = 1 },
				new { Id = 3, rn1 = 1, rn2 = 1, rn3 = 1, rn4 = 1, rn5 = 1, rn6 = 1 },
				new { Id = 4, rn1 = 1, rn2 = 1, rn3 = 1, rn4 = 1, rn5 = 1, rn6 = 1 },
				new { Id = 5, rn1 = 1, rn2 = 1, rn3 = 1, rn4 = 1, rn5 = 1, rn6 = 1 },
				new { Id = 6, rn1 = 1, rn2 = 1, rn3 = 1, rn4 = 1, rn5 = 1, rn6 = 1 },
				new { Id = 7, rn1 = 1, rn2 = 1, rn3 = 1, rn4 = 1, rn5 = 1, rn6 = 1 },
				new { Id = 8, rn1 = 1, rn2 = 1, rn3 = 1, rn4 = 1, rn5 = 1, rn6 = 1 },
				new { Id = 9, rn1 = 1, rn2 = 1, rn3 = 1, rn4 = 1, rn5 = 1, rn6 = 1 }
			};

			var result = query
				.Select(x => new { x.Entity.Id, x.rn1, x.rn2, x.rn3, x.rn4, x.rn5, x.rn6 })
				.ToList();

			result.Should().BeEquivalentTo(expected);
		}

		[Test]
		//TODO: we can emulate it for other providers by suing additional order by with CASE:
		//ROW_NUMBER() OVER(ODER BY WHEN x.Value IS NULL THEN 1 ELSE 0 END, x.Value)
		public void RowNumberWithNulls([IncludeDataSources(
			true,
			TestProvName.AllOracle12Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn7    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId).OrderBy(x.Timestamp, Sql.NullsPosition.First)),
					rn8    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId).OrderByDesc(x.Timestamp, Sql.NullsPosition.Last))
				})
				.OrderBy(x => x.Entity.Id);

			var expected = new[]
			{
				new { Id = 1, rn7 = 1, rn8 = 5 },
				new { Id = 2, rn7 = 2, rn8 = 4 },
				new { Id = 5, rn7 = 3, rn8 = 3 },
				new { Id = 8, rn7 = 4, rn8 = 2 },
				new { Id = 9, rn7 = 5, rn8 = 1 },
				new { Id = 3, rn7 = 1, rn8 = 2 },
				new { Id = 4, rn7 = 2, rn8 = 1 },
				new { Id = 6, rn7 = 1, rn8 = 2 },
				new { Id = 7, rn7 = 2, rn8 = 1 }
			};

			var result = query
				.Select(x => new { x.Entity.Id, x.rn7, x.rn8 })
				.ToList();

			result.Should().BeEquivalentTo(expected);
		}

		[Test]
		public void RowNumberWithoutPartition([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.RowNumber(f => f.OrderBy(x.Timestamp)),
					rn2    = Sql.Window.RowNumber(f => f.OrderBy(x.Value)),
					rn3    = Sql.Window.RowNumber(f => f.OrderByDesc(x.Timestamp)),
					rn4    = Sql.Window.RowNumber(f => f.OrderByDesc(x.Value)),
					rn5    = Sql.Window.RowNumber(f => f.OrderBy(x.Timestamp).ThenBy(x.Value)),
					rn6    = Sql.Window.RowNumber(f => f.OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

			var expected = new[]{ 
				new { Id = 1, rn1 = 2, rn2 = 3, rn3 = 8, rn4 = 7, rn5 = 2, rn6 = 8 },
				new { Id = 2, rn1 = 3, rn2 = 5, rn3 = 7, rn4 = 5, rn5 = 3, rn6 = 7 },
				new { Id = 3, rn1 = 4, rn2 = 2, rn3 = 6, rn4 = 8, rn5 = 4, rn6 = 6 },
				new { Id = 4, rn1 = 5, rn2 = 4, rn3 = 5, rn4 = 6, rn5 = 5, rn6 = 5 },
				new { Id = 5, rn1 = 6, rn2 = 6, rn3 = 4, rn4 = 4, rn5 = 6, rn6 = 4 },
				new { Id = 6, rn1 = 7, rn2 = 7, rn3 = 3, rn4 = 3, rn5 = 7, rn6 = 3 },
				new { Id = 7, rn1 = 8, rn2 = 8, rn3 = 2, rn4 = 2, rn5 = 8, rn6 = 2 },
				new { Id = 8, rn1 = 9, rn2 = 9, rn3 = 1, rn4 = 1, rn5 = 9, rn6 = 1 },
				new { Id = 9, rn1 = 1, rn2 = 1, rn3 = 9, rn4 = 9, rn5 = 1, rn6 = 9 },
			};

			var result = query
				.Select(x => new { x.Entity.Id, x.rn1, x.rn2, x.rn3, x.rn4, x.rn5, x.rn6 })
				.ToList();

			result.Should().BeEquivalentTo(expected);
		}

	}
}

