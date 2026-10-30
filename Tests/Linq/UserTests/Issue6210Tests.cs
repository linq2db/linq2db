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
	public class Issue6210Tests : TestBase
	{
		[Table]
		sealed class ParentRow
		{
			[Column, PrimaryKey] public int Id { get; set; }
		}

		[Table]
		sealed class Row
		{
			[Column] public int     ParentId { get; set; }
			[Column] public decimal Value    { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6210")]
		public async Task SumClampedValues(
			[DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context,
			[Values] bool useSelector)
		{
			using var db      = GetDataContext(context);
			using var parents = db.CreateLocalTable(new[]
			{
				new ParentRow { Id = 1 },
				new ParentRow { Id = 2 },
				new ParentRow { Id = 3 },
			});
			using var rows = db.CreateLocalTable(new[]
			{
				new Row { ParentId = 1, Value =    5.5m  },
				new Row { ParentId = 1, Value = -100m    },
				new Row { ParentId = 1, Value =    2.25m },
				new Row { ParentId = 1, Value =    0m    },
				new Row { ParentId = 2, Value =   -4m    },
			});

			var query = useSelector
				? parents.OrderBy(parent => parent.Id)
					.Select(parent => rows
						.Where(row => row.ParentId == parent.Id)
						.Sum(row => Math.Max(0m, row.Value)))
				: parents.OrderBy(parent => parent.Id)
					.Select(parent => rows
						.Where(row => row.ParentId == parent.Id)
						.Select(row => Math.Max(0m, row.Value))
						.Sum());

			var totals = await query.ToListAsync();

			// Mixed values, only negative values, and no rows must all use LINQ Sum semantics.
			totals.ShouldBe([7.75m, 0m, 0m]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6210")]
		public async Task SumClampedValuesPreservesOuterOperators(
			[DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context,
			[Values] bool applyFilter)
		{
			using var db      = GetDataContext(context);
			using var parents = db.CreateLocalTable(new[]
			{
				new ParentRow { Id = 1 },
				new ParentRow { Id = 2 },
				new ParentRow { Id = 3 },
			});
			using var rows = db.CreateLocalTable(new[]
			{
				new Row { ParentId = 1, Value =    5.5m  },
				new Row { ParentId = 1, Value = -100m    },
				new Row { ParentId = 1, Value =    2.25m },
				new Row { ParentId = 1, Value =    0m    },
				new Row { ParentId = 2, Value =   -4m    },
			});

			var query = applyFilter
				? parents.OrderBy(parent => parent.Id)
					.Select(parent => rows
						.Where(row => row.ParentId == parent.Id)
						.Select(row => Math.Max(0m, row.Value))
						.Where(value => value > 3m)
						.Sum())
				: parents.OrderBy(parent => parent.Id)
					.Select(parent => rows
						.Where(row => row.ParentId == parent.Id)
						.Select(row => Math.Max(0m, row.Value))
						.Select(value => value + 1m)
						.Sum());

			var totals   = await query.ToListAsync();
			var expected = applyFilter ? new[] { 5.5m, 0m, 0m } : new[] { 11.75m, 1m, 0m };

			totals.ShouldBe(expected);
		}
	}
}
