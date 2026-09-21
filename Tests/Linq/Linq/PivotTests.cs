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
			// YDB requires every table to have a primary key
			[PrimaryKey(Configuration = ProviderName.Ydb)]
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
		public void UnpivotExcludeNulls([DataSources] string context)
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
		public void UnpivotIncludeNulls([DataSources] string context)
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

		/// <summary>
		/// The unpivoted columns can be named by value rather than by a compile-time member reference:
		/// Sql.Property is rewritten to member access before UnpivotBuilder sees the selectors.
		/// </summary>
		[Test]
		public void UnpivotWithRuntimeColumnNames([DataSources] string context)
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

		/// <summary>
		/// The column set arrives as a <see cref="string"/> array, which the selector overload cannot take at
		/// all - there is no way to spread N runtime names into N lambda arguments.
		/// </summary>
		[Test]
		public void UnpivotByColumnNames([DataSources] string context)
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

		/// <summary>
		/// The unpivoted columns are not members of the mapped type at all, so the fields do not exist until the
		/// table context creates them lazily.
		/// </summary>
		[Test]
		public void UnpivotWithColumnsAbsentFromTheMapping([DataSources] string context)
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
		public void UnpivotLowersToUnionAll([IncludeDataSources(TestProvName.AllSQLite, ProviderName.DuckDB, TestProvName.AllSqlServer, TestProvName.AllOracle)] string context)
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
		sealed class MonthlySales
		{
			[PrimaryKey(Configuration = ProviderName.Ydb)]
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

			// A row whose second group is entirely NULL: the documented contract keeps it.
			public static readonly MonthlySales[] WithEmptyGroup =
			{
				new() { Id = 1, Jan = 10m, Feb = 20m, Mar = 30m, Apr = null, May = null, Jun = null },
			};
		}

		[Test]
		public void UnpivotMultiValue([DataSources] string context)
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

		/// <summary>
		/// A group whose measures are all NULL is still a row - the overload filters nothing.
		/// </summary>
		[Test]
		public void UnpivotMultiValueKeepsAllNullGroup([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(MonthlySales.WithEmptyGroup);

			var result = t
				.Unpivot(
					(row, quarter, m1, m2, m3) => new { row.Id, Quarter = quarter, M1 = m1, M2 = m2, M3 = m3 },
					("Q1", x => x.Jan, x => x.Feb, x => x.Mar),
					("Q2", x => x.Apr, x => x.May, x => x.Jun))
				.OrderBy(r => r.Quarter)
				.ToArray();

			result.Length.ShouldBe(2);
			result[1].Quarter.ShouldBe("Q2");
			result[1].M1.ShouldBeNull();
			result[1].M2.ShouldBeNull();
			result[1].M3.ShouldBeNull();
		}

		[Table]
		sealed class AliasedQuarterly
		{
			[PrimaryKey(Configuration = ProviderName.Ydb)]
			[Column]            public int      Id { get; set; }
			[Column("Q_ONE")]   public decimal? Q1 { get; set; }
			[Column("Q_TWO")]   public decimal? Q2 { get; set; }

			public static readonly AliasedQuarterly[] Data =
			{
				new() { Id = 1, Q1 = 10m, Q2 = 20m },
			};
		}

		/// <summary>
		/// The name column carries the physical column name, so a renamed member reads as its column name rather
		/// than as the member it was written with.
		/// </summary>
		[Test]
		public void UnpivotNameColumnUsesPhysicalName([DataSources] string context)
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
