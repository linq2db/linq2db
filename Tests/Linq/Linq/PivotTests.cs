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
		public void UnpivotExcludeNulls([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
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
		public void UnpivotIncludeNulls([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
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
		public void UnpivotEmitsNativeKeyword([IncludeDataSources(ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
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
		public void Pivot([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
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
				if (context.Contains("SQLite", System.StringComparison.Ordinal))
					sql.ShouldNotContain("PIVOT");
				else
					sql.ShouldContain("PIVOT");
			}
		}

		[Test]
		public void PivotMultiAggregate([IncludeDataSources(TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var result = t
				.Pivot(p => new
				{
					p.Key.Category,
					Sum2000 = p.Sum  (x => x.Amount, x => x.Year, 2000),
					Cnt2000 = p.Count(x => x.Amount, x => x.Year, 2000),
				})
				.OrderBy(r => r.Category)
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Category.ShouldBe("A");
			result[0].Sum2000.ShouldBe(10m);
			result[0].Cnt2000.ShouldBe(1);
		}

		[Table]
		sealed class RegionSales
		{
			[Column] public string   Category { get; set; } = null!;
			[Column] public string   Region   { get; set; } = null!;
			[Column] public int      Year     { get; set; }
			[Column] public decimal? Amount   { get; set; }

			public static readonly RegionSales[] Data =
			{
				new() { Category = "A", Region = "EU", Year = 2000, Amount = 10m },
				new() { Category = "A", Region = "EU", Year = 2010, Amount = 20m },
				new() { Category = "A", Region = "US", Year = 2000, Amount = 3m  },
				new() { Category = "B", Region = "EU", Year = 2000, Amount = 5m  },
			};
		}

		[Test]
		public void PivotCompositeKey([IncludeDataSources(TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(RegionSales.Data);

			var result = t
				.Pivot(p => new
				{
					p.Key.Category,
					p.Key.Region,
					Y2000 = p.Sum(x => x.Amount, x => x.Year, 2000),
					Y2010 = p.Sum(x => x.Amount, x => x.Year, 2010),
				})
				.OrderBy(r => r.Category)
				.ThenBy(r => r.Region)
				.ToArray();

			// groups: (A,EU) Y2000=10 Y2010=20; (A,US) Y2000=3; (B,EU) Y2000=5
			result.Length.ShouldBe(3);
			result[0].Category.ShouldBe("A");
			result[0].Region.ShouldBe("EU");
			result[0].Y2000.ShouldBe(10m);
			result[0].Y2010.ShouldBe(20m);
			result[1].Region.ShouldBe("US");
			result[1].Y2000.ShouldBe(3m);
			result[2].Category.ShouldBe("B");
			result[2].Y2000.ShouldBe(5m);
		}

		[Table]
		sealed class QuarterAmounts
		{
			[Column] public string   Category { get; set; } = null!;
			[Column] public int      Year     { get; set; }
			[Column] public int      Quarter  { get; set; }
			[Column] public decimal? Amount   { get; set; }

			public static readonly QuarterAmounts[] Data =
			{
				new() { Category = "A", Year = 2000, Quarter = 1, Amount = 10m },
				new() { Category = "A", Year = 2000, Quarter = 2, Amount = 20m },
				new() { Category = "A", Year = 2010, Quarter = 1, Amount = 30m },
				new() { Category = "B", Year = 2000, Quarter = 1, Amount = 5m  },
			};
		}

		[Test]
		public void PivotCompositeFor([IncludeDataSources(TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterAmounts.Data);

			// Composite (multi-column) FOR: pivot on (Year, Quarter).
			var result = t
				.Pivot(p => new
				{
					p.Key.Category,
					Y2000Q1 = p.Sum(x => x.Amount, x => new { x.Year, x.Quarter }, new { Year = 2000, Quarter = 1 }),
					Y2000Q2 = p.Sum(x => x.Amount, x => new { x.Year, x.Quarter }, new { Year = 2000, Quarter = 2 }),
				})
				.OrderBy(r => r.Category)
				.ToArray();

			// A: Q1=10, Q2=20; B: Q1=5, Q2=null
			result.Length.ShouldBe(2);
			result[0].Category.ShouldBe("A");
			result[0].Y2000Q1.ShouldBe(10m);
			result[0].Y2000Q2.ShouldBe(20m);
			result[1].Category.ShouldBe("B");
			result[1].Y2000Q1.ShouldBe(5m);

			// Oracle / DuckDB support a composite (multi-column) FOR natively; SQL Server / SQLite lower to CASE.
			var sql = LastQuery!.ToUpperInvariant();
			if (context.Contains("DuckDB", System.StringComparison.Ordinal) || context.Contains("Oracle", System.StringComparison.Ordinal))
				sql.ShouldContain("PIVOT");
			else
				sql.ShouldNotContain("PIVOT");
		}

		[Test]
		public void PivotThenWhereAndProject([IncludeDataSources(ProviderName.DuckDB, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			// Where on a pivoted column + a projection that drops another pivoted column — stresses column pruning.
			var result = t
				.Pivot(p => new
				{
					p.Key.Category,
					Y2000 = p.Sum(x => x.Amount, x => x.Year, 2000),
					Y2010 = p.Sum(x => x.Amount, x => x.Year, 2010),
				})
				.Where(r => r.Y2010 >= 15)
				.Select(r => new { r.Category, r.Y2010 })
				.OrderBy(r => r.Category)
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Category.ShouldBe("A");
			result[0].Y2010.ShouldBe(20m);
			result[1].Category.ShouldBe("B");
			result[1].Y2010.ShouldBe(15m);
		}

		[Table]
		sealed class MonthlySales
		{
			[Column] public int      Id  { get; set; }
			[Column] public decimal? Jan { get; set; }
			[Column] public decimal? Feb { get; set; }
			[Column] public decimal? Mar { get; set; }
			[Column] public decimal? Apr { get; set; }
			[Column] public decimal? May { get; set; }
			[Column] public decimal? Jun { get; set; }

			public static readonly MonthlySales[] Data =
			{
				new() { Id = 1, Jan = 10m, Feb = 20m, Mar = 30m, Apr = 40m, May = 50m, Jun = 60m },
			};
		}

		[Test]
		public void UnpivotMultiValue([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(MonthlySales.Data);

			// Multi-value UNPIVOT: two column-groups of three columns each -> two rows with three value columns.
			var result = t
				.Unpivot(
					(row, quarter, m1, m2, m3) => new { row.Id, Quarter = quarter, M1 = m1, M2 = m2, M3 = m3 },
					("Q1", x => x.Jan, x => x.Feb, x => x.Mar),
					("Q2", x => x.Apr, x => x.May, x => x.Jun))
				.OrderBy(r => r.Quarter)
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Quarter.ShouldBe("Q1");
			result[0].M1.ShouldBe(10m);
			result[0].M2.ShouldBe(20m);
			result[0].M3.ShouldBe(30m);
			result[1].Quarter.ShouldBe("Q2");
			result[1].M1.ShouldBe(40m);
			result[1].M3.ShouldBe(60m);
		}
	}
}
