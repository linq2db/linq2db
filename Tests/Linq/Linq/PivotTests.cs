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

		/// <summary>
		/// Whether the unpivoted columns can be named by value rather than by a compile-time member reference.
		/// Sql.Property is rewritten to member access before UnpivotBuilder sees the selectors, so the
		/// ergonomic overload may be the only thing missing. Run on the full provider set, so any discrepancy
		/// between the name the native keyword reports and the one the lowering emits surfaces here.
		/// </summary>
		[Test]
		public void UnpivotWithRuntimeColumnNames([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterlySales.Data);

			string q1 = "Q1", q2 = "Q2", q3 = "Q3", q4 = "Q4";

			var result = t
				.Unpivot(
					(row, quarter, amount) => new { row.Id, Quarter = quarter, Amount = amount },
					row => Sql.Property<decimal?>(row, q1),
					row => Sql.Property<decimal?>(row, q2),
					row => Sql.Property<decimal?>(row, q3),
					row => Sql.Property<decimal?>(row, q4))
				.OrderBy(r => r.Id)
				.ThenBy(r => r.Quarter)
				.ToArray();

			result.Length.ShouldBe(6);
			result.ShouldAllBe(r => r.Amount != null);
			result.Select(r => r.Quarter).Distinct().OrderBy(q => q).ShouldBe(new[] { "Q1", "Q2", "Q3", "Q4" });
		}

		// The same table, mapped without its quarter columns - the shape a per-tenant or per-period table has.
		[Table("QuarterlySales")]
		sealed class QuarterlyKeys
		{
			[Column] public int    Id     { get; set; }
			[Column] public string Region { get; set; } = null!;
		}

		[Table]
		sealed class AliasedSales
		{
			[Column]          public int      Id { get; set; }
			[Column("Q_ONE")] public decimal? Q1 { get; set; }
			[Column("Q_TWO")] public decimal? Q2 { get; set; }

			public static readonly AliasedSales[] Data = { new() { Id = 1, Q1 = 10m, Q2 = 20m } };
		}

		/// <summary>
		/// A column whose physical name differs from its member name reaches the native keyword and reports the
		/// physical name: the lookup matches on the member name, which is what the source field carries. The
		/// control for the unmapped case below, which is the one the lookup cannot see.
		/// </summary>
		[Test]
		public void UnpivotAliasedColumnEmitsNativeKeyword([IncludeDataSources(ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(AliasedSales.Data);

			var result = t
				.Unpivot((row, name, amount) => new { row.Id, Name = name, Amount = amount }, x => x.Q1, x => x.Q2)
				.OrderBy(r => r.Name)
				.ToArray();

			result.Select(r => r.Name).ShouldBe(new[] { "Q_ONE", "Q_TWO" });
			LastQuery!.ToUpperInvariant().ShouldContain("UNPIVOT");
		}

		/// <summary>
		/// The column set arrives as a <see cref="string"/> array, which the selector overload cannot take at
		/// all - there is no way to spread N runtime names into N lambda arguments.
		/// </summary>
		[Test]
		public void UnpivotByColumnNames([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterlySales.Data);

			var columns = new[] { "Q1", "Q2", "Q3", "Q4" };

			var result = t
				.Unpivot((QuarterlySales row, string quarter, decimal? amount) => new { row.Id, Quarter = quarter, Amount = amount }, columns)
				.OrderBy(r => r.Id)
				.ThenBy(r => r.Quarter)
				.ToArray();

			result.Length.ShouldBe(6);
			result.ShouldAllBe(r => r.Amount != null);
			result.Select(r => r.Quarter).Distinct().OrderBy(q => q).ShouldBe(new[] { "Q1", "Q2", "Q3", "Q4" });

			var kept = t
				.Unpivot(UnpivotNulls.IncludeNulls, (QuarterlySales row, string quarter, decimal? amount) => new { row.Id, Quarter = quarter, Amount = amount }, columns)
				.ToArray();

			kept.Length.ShouldBe(8);
		}

		/// <summary>A narrower name set is a different query, not a cache hit on the wider one.</summary>
		[Test, QueryCacheTest]
		public void UnpivotByColumnNamesDiscriminatesQueryCache([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterlySales.Data);

			IQueryable<int> Build(string[] columns) => t
				.Unpivot((QuarterlySales row, string quarter, decimal? amount) => new { row.Id, Quarter = quarter, Amount = amount }, columns)
				.Select(r => r.Id);

			var probe = Build(new[] { "Q1" });

			probe.ClearCache();

			var start = probe.GetCacheMissCount();

			_ = Build(new[] { "Q1" }).ToArray();
			_ = Build(new[] { "Q1" }).ToArray();
			(probe.GetCacheMissCount() - start).ShouldBe(1, "the same column set must reuse the compiled query");

			_ = Build(new[] { "Q2" }).ToArray();
			(probe.GetCacheMissCount() - start).ShouldBe(2, "a different column must not reuse it");

			_ = Build(new[] { "Q1", "Q2" }).ToArray();
			(probe.GetCacheMissCount() - start).ShouldBe(3, "a wider column set must not reuse it");
		}

		/// <summary>The same gap for a column the mapping does not carry at all: its field is created lazily.</summary>
		[Test]
		public void UnpivotUnmappedColumnEmitsNativeKeyword([IncludeDataSources(ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterlySales.Data);

			string q1 = "Q1", q2 = "Q2";

			_ = db.GetTable<QuarterlyKeys>()
				.Unpivot(
					(row, quarter, amount) => new { row.Id, Quarter = quarter, Amount = amount },
					row => Sql.Property<decimal?>(row, q1),
					row => Sql.Property<decimal?>(row, q2))
				.ToArray();

			LastQuery!.ToUpperInvariant().ShouldContain("UNPIVOT");
		}

		/// <summary>
		/// The unpivoted columns are not members of the mapped type at all, so the fields do not exist until
		/// the table context creates them lazily - which is where the native path looks them up by name.
		/// </summary>
		[Test]
		public void UnpivotWithColumnsAbsentFromTheMapping([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterlySales.Data);

			string q1 = "Q1", q2 = "Q2", q3 = "Q3", q4 = "Q4";

			var result = db.GetTable<QuarterlyKeys>()
				.Unpivot(
					(row, quarter, amount) => new { row.Id, Quarter = quarter, Amount = amount },
					row => Sql.Property<decimal?>(row, q1),
					row => Sql.Property<decimal?>(row, q2),
					row => Sql.Property<decimal?>(row, q3),
					row => Sql.Property<decimal?>(row, q4))
				.OrderBy(r => r.Id)
				.ThenBy(r => r.Quarter)
				.ToArray();

			result.Length.ShouldBe(6);
			result.ShouldAllBe(r => r.Amount != null);
			result.Select(r => r.Quarter).Distinct().OrderBy(q => q).ShouldBe(new[] { "Q1", "Q2", "Q3", "Q4" });
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

			// Two rows per cell, so AVG / MIN / MAX are distinguishable from SUM.
			public static readonly CategorySales[] MultiRowData =
			{
				new() { Category = "A", Year = 2000, Amount = 10m },
				new() { Category = "A", Year = 2000, Amount = 30m },
				new() { Category = "B", Year = 2000, Amount = 5m  },
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

		[Test]
		public void PivotAvgMinMax([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.MultiRowData);

			// Three aggregates - always the conditional-aggregation lowering.
			var lowered = t
				.Pivot(p => new
				{
					p.Key.Category,
					Avg2000 = p.Avg(x => x.Amount, x => x.Year, 2000),
					Min2000 = p.Min(x => x.Amount, x => x.Year, 2000),
					Max2000 = p.Max(x => x.Amount, x => x.Year, 2000),
				})
				.OrderBy(r => r.Category)
				.ToArray();

			lowered.Length.ShouldBe(2);
			lowered[0].Category.ShouldBe("A");
			lowered[0].Avg2000.ShouldBe(20d);
			lowered[0].Min2000.ShouldBe(10m);
			lowered[0].Max2000.ShouldBe(30m);
			lowered[1].Category.ShouldBe("B");
			lowered[1].Avg2000.ShouldBe(5d);

			// Single aggregate over a plain table - native PIVOT where the provider supports it.
			var single = t
				.Pivot(p => new
				{
					p.Key.Category,
					Avg2000 = p.Avg(x => x.Amount, x => x.Year, 2000),
				})
				.OrderBy(r => r.Category)
				.ToArray();

			single[0].Avg2000.ShouldBe(20d);
			single[1].Avg2000.ShouldBe(5d);
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

			// Oracle / DuckDB support multi-value UNPIVOT natively; SQL Server / SQLite lower to UNION ALL.
			if (!context.Contains("LinqService", System.StringComparison.Ordinal))
			{
				var sql = LastQuery!.ToUpperInvariant();
				if (context.Contains("DuckDB", System.StringComparison.Ordinal) || context.Contains("Oracle", System.StringComparison.Ordinal))
					sql.ShouldContain("UNPIVOT");
				else
					sql.ShouldNotContain("UNPIVOT");
			}
		}

		[Table]
		sealed class AliasedQuarterly
		{
			[Column]            public int      Id { get; set; }
			[Column("Q_ONE")]   public decimal? Q1 { get; set; }
			[Column("Q_TWO")]   public decimal? Q2 { get; set; }

			public static readonly AliasedQuarterly[] Data =
			{
				new() { Id = 1, Q1 = 10m, Q2 = 20m },
			};
		}

		/// <summary>
		/// The name column carries the physical column name on every provider. Native UNPIVOT takes it from the
		/// database, so the portable lowering has to emit the same string - emitting the CLR member name made
		/// the identical query return Q1/Q2 where it lowers and Q_ONE/Q_TWO where it does not.
		/// </summary>
		[Test]
		public void UnpivotNameColumnUsesPhysicalName([IncludeDataSources(true, TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(AliasedQuarterly.Data);

			var result = t
				.Unpivot((row, name, value) => new { row.Id, Name = name, Value = value }, x => x.Q1, x => x.Q2)
				.OrderBy(r => r.Name)
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Name.ShouldBe("Q_ONE");
			result[1].Name.ShouldBe("Q_TWO");
		}

		/// <summary>
		/// The multi-value overload used to pass its groups as a single array constant, which the query cache
		/// compares by reference - so every execution rebuilt the query. The groups now travel as a
		/// query-dependent name array plus quoted column lambdas, both of which compare by value.
		/// </summary>
		[Test, QueryCacheTest]
		public void UnpivotMultiValueQueryCache([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(MonthlySales.Data);

			var probe = t.Unpivot(
				(row, quarter, m1, m2, m3) => new { row.Id, Quarter = quarter, M1 = m1, M2 = m2, M3 = m3 },
				("Q1", x => x.Jan, x => x.Feb, x => x.Mar));

			probe.ClearCache();
			var start = probe.GetCacheMissCount();

			for (var i = 0; i < 3; i++)
			{
				_ = t.Unpivot(
						(row, quarter, m1, m2, m3) => new { row.Id, Quarter = quarter, M1 = m1, M2 = m2, M3 = m3 },
						("Q1", x => x.Jan, x => x.Feb, x => x.Mar),
						("Q2", x => x.Apr, x => x.May, x => x.Jun))
					.ToSqlQuery();
			}

			(probe.GetCacheMissCount() - start).ShouldBe(1, "the same groups must reuse the compiled query");

			// A different group name is a different query - the names must discriminate.
			_ = t.Unpivot(
					(row, quarter, m1, m2, m3) => new { row.Id, Quarter = quarter, M1 = m1, M2 = m2, M3 = m3 },
					("Q1", x => x.Jan, x => x.Feb, x => x.Mar),
					("Q3", x => x.Apr, x => x.May, x => x.Jun))
				.ToSqlQuery();

			(probe.GetCacheMissCount() - start).ShouldBe(2);
		}
	}
}
