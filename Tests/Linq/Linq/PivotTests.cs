using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class PivotTests : TestBase
	{
		[Table]
		sealed class QuarterlySales
		{
			[Column] public int      Id     { get; set; }
			[Column] public string   Region { get; set; } = null!;
			[Column] public decimal? Q1     { get; set; }
			[Column] public decimal? Q2     { get; set; }
			[Column] public decimal? Q3     { get; set; }
			[Column] public decimal? Q4     { get; set; }

			public static readonly QuarterlySales[] Data =
			{
				new() { Id = 1, Region = "EU", Q1 = 10m, Q2 = 20m,   Q3 = 30m, Q4 = null },
				new() { Id = 2, Region = "US", Q1 = 5m,  Q2 = null,   Q3 = 15m, Q4 = 25m  },
			};
		}

		[Test]
		public void UnpivotExcludeNulls([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterlySales.Data);

			var result = t
				.Unpivot(
					(row, quarter, amount) => new { row.Id, Quarter = quarter, Amount = amount },
					x => x.Q1, x => x.Q2, x => x.Q3, x => x.Q4)
				.OrderBy(r => r.Id)
				.ThenBy(r => r.Quarter)
				.ToArray();

			// 2 rows x 4 quarters - 2 NULL cells excluded = 6
			result.Length.ShouldBe(6);
			result.ShouldAllBe(r => r.Amount != null);
			result.Count(r => r.Id == 1).ShouldBe(3);
			result.Count(r => r.Id == 2).ShouldBe(3);
		}

		[Test]
		public void UnpivotIncludeNulls([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterlySales.Data);

			var result = t
				.Unpivot(
					UnpivotNulls.IncludeNulls,
					(row, quarter, amount) => new { row.Id, Quarter = quarter, Amount = amount },
					x => x.Q1, x => x.Q2, x => x.Q3, x => x.Q4)
				.ToArray();

			// 2 rows x 4 quarters, NULLs kept = 8
			result.Length.ShouldBe(8);
			result.Count(r => r.Amount == null).ShouldBe(2);
		}

		[Test]
		public void UnpivotEmitsNativeKeyword([IncludeDataSources(ProviderName.DuckDB)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterlySales.Data);

			_ = t
				.Unpivot((row, quarter, amount) => new { row.Id, Quarter = quarter, Amount = amount }, x => x.Q1, x => x.Q2, x => x.Q3, x => x.Q4)
				.ToArray();

			LastQuery!.ToUpperInvariant().ShouldContain("UNPIVOT");
		}

		[Test]
		public void UnpivotLowersToUnionAll([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterlySales.Data);

			_ = t
				.Unpivot((row, quarter, amount) => new { row.Id, Quarter = quarter, Amount = amount }, x => x.Q1, x => x.Q2, x => x.Q3, x => x.Q4)
				.ToArray();

			var sql = LastQuery!.ToUpperInvariant();
			sql.ShouldNotContain("UNPIVOT");
			sql.ShouldContain("UNION ALL");
		}

		[Table]
		sealed class CategorySales
		{
			[Column] public string   Category { get; set; } = null!;
			[Column] public int      Year     { get; set; }
			[Column] public decimal? Amount   { get; set; }

			public static readonly CategorySales[] Data =
			{
				new() { Category = "A", Year = 2000, Amount = 10m },
				new() { Category = "A", Year = 2010, Amount = 20m },
				new() { Category = "B", Year = 2000, Amount = 5m  },
				new() { Category = "B", Year = 2010, Amount = 15m },
			};
		}

		[Test]
		public void Pivot([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var result = t
				.Pivot(p => new
				{
					p.Key.Category,
					Y2000 = p.Sum(x => x.Amount, x => x.Year, 2000),
					Y2010 = p.Sum(x => x.Amount, x => x.Year, 2010),
				})
				.OrderBy(r => r.Category)
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Category.ShouldBe("A");
			result[0].Y2000.ShouldBe(10m);
			result[0].Y2010.ShouldBe(20m);
			result[1].Category.ShouldBe("B");
			result[1].Y2000.ShouldBe(5m);
			result[1].Y2010.ShouldBe(15m);

			// SQL-text assertion only for the direct (non-remote) context: DuckDB emits native PIVOT, SQLite lowers.
			if (!context.Contains("LinqService", System.StringComparison.Ordinal))
			{
				var sql = LastQuery!.ToUpperInvariant();
				if (context.Contains("DuckDB", System.StringComparison.Ordinal))
					sql.ShouldContain("PIVOT");
				else
					sql.ShouldNotContain("PIVOT");
			}
		}
	}
}
