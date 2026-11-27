using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

using Microsoft.CSharp.RuntimeBinder;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class PivotDynamicTests : TestBase
	{
		[Table]
		sealed class Sales
		{
			// YDB requires every table to have a primary key
			[PrimaryKey(ProviderName.Ydb, 1)]
			[Column] public string   Category { get; set; } = null!;
			[PrimaryKey(ProviderName.Ydb, 2)]
			[Column] public int      Year     { get; set; }
			[Column] public decimal? Amount   { get; set; }
			[Column] public string?  Note     { get; set; }

			[ExpressionMethod(nameof(DoubleAmountExpr))]
			public decimal? DoubleAmount => Amount * 2;

			static Expression<Func<Sales, decimal?>> DoubleAmountExpr() => x => x.Amount * 2;

			public static readonly Sales[] Data =
			{
				new() { Category = "A", Year = 2000, Amount = 10m, Note = "a0" },
				new() { Category = "A", Year = 2010, Amount = 20m, Note = "a1" },
				new() { Category = "B", Year = 2000, Amount = 5m,  Note = "b0" },
				new() { Category = "B", Year = 2010, Amount = 15m, Note = "b1" },
			};
		}

		sealed class SalesDto
		{
			public string     Category { get; set; } = null!;
			public decimal?   Total    { get; set; }
			public PivotCells Cells    { get; set; } = null!;
		}

		static string Year(int y) => "Y" + y.ToString(CultureInfo.InvariantCulture);

		static string Message(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

		[Test]
		public void PivotsRuntimeValues([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			// Runtime, and built by a query so it cannot fold to a constant. Ordered because the value set decides
			// the generated cell order, and an unordered DISTINCT makes the emitted SQL differ run to run.
			var years = t
				.Select(x => x.Year)
				.Distinct()
				.OrderBy(y => y)
				.ToList();

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, years)
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0].Cells["Y2000"].ShouldBe(10m);
			result[0].Cells["Y2010"].ShouldBe(20m);

			result[1].Key.ShouldBe("B");
			result[1].Cells["Y2000"].ShouldBe(5m);
			result[1].Cells["Y2010"].ShouldBe(15m);
		}

		/// <summary>
		/// Several cells crossed with the runtime values, alongside static aggregated members, in a declared result type -
		/// the shape a real report needs.
		/// </summary>
		[Test]
		public void PivotsMultipleCellsIntoUserType([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var years = new[] { 2000, 2010 };

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new SalesDto
				{
					Category = g.Key,
					Total    = g.Sum(x => x.Amount),
					Cells    = g
						.Pivot(x => x.Year, years)
						.Cell(rows => rows.Sum(x => x.Amount), y => "AMT_"  + Year(y))
						.Cell(rows => rows.Max(x => x.Note),   y => "NOTE_" + Year(y))
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Category, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Category.ShouldBe("A");
			result[0].Total.ShouldBe(30m);
			result[0].Cells["AMT_Y2000"].ShouldBe(10m);
			result[0].Cells["NOTE_Y2010"].ShouldBe("a1");

			result[1].Category.ShouldBe("B");
			result[1].Total.ShouldBe(20m);
			result[1].Cells["AMT_Y2010"].ShouldBe(15m);
			result[1].Cells["NOTE_Y2000"].ShouldBe("b0");
		}

		// Call-site guard: throws while the query is built, before any SQL.
		[Test]
		public void MultipleCellsWithoutNameFactoryThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			// Two cells would otherwise generate two columns called "2000".
			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount))
						.Cell(rows => rows.Max(x => x.Note))
						.AsDictionary(),
				});

			Action act = () => query.ToList();

			Assert.That(() => act(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(ErrorHelper.Pivot.Error_PivotNameFactoryRequired));
		}

		[Table]
		sealed class CategorySales
		{
			// Surrogate key: the rows are deliberately non-unique, so nothing else can serve as YDB's primary key.
			[PrimaryKey(Configuration = ProviderName.Ydb)]
			[Column] public int      Id       { get; set; }
			[Column] public string   Category { get; set; } = null!;
			[Column] public int      Year     { get; set; }
			[Column] public decimal? Amount   { get; set; }

			public static readonly CategorySales[] Data =
			{
				new() { Id = 1, Category = "A", Year = 2000, Amount = 10m },
				new() { Id = 2, Category = "A", Year = 2010, Amount = 20m },
				new() { Id = 3, Category = "B", Year = 2000, Amount = 5m  },
				new() { Id = 4, Category = "B", Year = 2010, Amount = 15m },
			};

			// A fractional cell, so a cell read as int differs from the cell.
			public static readonly CategorySales[] FractionalData =
			{
				new() { Id = 1, Category = "A", Year = 2000, Amount = 10m   },
				new() { Id = 2, Category = "A", Year = 2010, Amount = 20.6m },
				new() { Id = 3, Category = "B", Year = 2000, Amount = 5m    },
				new() { Id = 4, Category = "B", Year = 2010, Amount = 15m   },
			};

			// Two rows per cell, so AVG / MIN / MAX are distinguishable from SUM.
			public static readonly CategorySales[] MultiRowData =
			{
				new() { Id = 1, Category = "A", Year = 2000, Amount = 10m },
				new() { Id = 2, Category = "A", Year = 2000, Amount = 30m },
				new() { Id = 3, Category = "B", Year = 2000, Amount = 5m  },
			};

			// A group whose distinct count differs from its row count, so a plain COUNT cannot pass by accident.
			public static readonly CategorySales[] DuplicateData =
			{
				new() { Id = 1, Category = "A", Year = 2000, Amount = 10m },
				new() { Id = 2, Category = "A", Year = 2000, Amount = 10m },
				new() { Id = 3, Category = "A", Year = 2000, Amount = 30m },
				new() { Id = 4, Category = "B", Year = 2000, Amount = 5m  },
			};
		}

		[Test]
		public void PivotsConstantValueSet([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0].Cells["Y2000"].ShouldBe(10m);
			result[0].Cells["Y2010"].ShouldBe(20m);

			result[1].Key.ShouldBe("B");
			result[1].Cells["Y2000"].ShouldBe(5m);
			result[1].Cells["Y2010"].ShouldBe(15m);
		}

		/// <summary>A pivot written in a sub-query, over a source that is itself a query rather than a captured local.</summary>
		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void PivotInlineInASubQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var categories =
				from c in t
				where t
					.Where(x => x.Amount > 0)
					.GroupBy(x => x.Category)
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, new[] { 2000, 2010 })
							.Cell(rows => rows.Sum(x => x.Amount), Year)
							.AsDictionary(),
					})
					.Any(r => r.Key == c.Category && Sql.Property<decimal?>(r.Cells, "Y2010") > 16)
				select c.Category;

			categories
				.Distinct()
				.ToArray()
				.ShouldBe(new[] { "A" });
		}

		[Test]
		public void PivotOverACorrelatedSource([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var correlated =
				from c in t
				from r in t
					.Where(x => x.Category == c.Category)
					.GroupBy(x => x.Category)
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, new[] { 2000, 2010 })
							.Cell(rows => rows.Sum(x => x.Amount), Year)
							.AsDictionary(),
					})
				select new
				{
					c.Id,
					r.Key,
					Y2010 = Sql.Property<decimal?>(r.Cells, "Y2010"),
				};

			correlated
				.ToArray()
				.OrderBy(r => r.Id)
				.Select(r => (r.Id, r.Key, r.Y2010))
				.ShouldBe(new (int, string, decimal?)[] { (1, "A", 20m), (2, "A", 20m), (3, "B", 15m), (4, "B", 15m) });
		}

		/// <summary>A cell aggregate may read the whole group through the selector's own grouping parameter.</summary>
		[Test]
		public void PivotsAgainstTheGroupTotal([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => g.Sum(x => x.Amount) - rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Cells["Y2000"].ShouldBe(20m);
			result[0].Cells["Y2010"].ShouldBe(10m);
			result[1].Cells["Y2000"].ShouldBe(15m);
			result[1].Cells["Y2010"].ShouldBe(5m);
		}

		[Test]
		public void PivotsMultipleAggregatesOfOneValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount), y => "Sum" + Year(y))
						.Cell(rows => rows.Count(),           y => "Cnt" + Year(y))
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0].Cells["SumY2000"].ShouldBe(10m);
			result[0].Cells["CntY2000"].ShouldBe(1);

			result[1].Cells["SumY2000"].ShouldBe(5m);
			result[1].Cells["CntY2000"].ShouldBe(1);
		}

		[Test]
		public void PivotsMinMaxCells([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.MultiRowData);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Min(x => x.Amount), y => "Min" + Year(y))
						.Cell(rows => rows.Max(x => x.Amount), y => "Max" + Year(y))
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0].Cells["MinY2000"].ShouldBe(10m);
			result[0].Cells["MaxY2000"].ShouldBe(30m);

			result[1].Key.ShouldBe("B");
			result[1].Cells["MinY2000"].ShouldBe(5m);
			result[1].Cells["MaxY2000"].ShouldBe(5m);
		}

		[Test]
		// Excluded on Jet OLE DB: an AVG cell over a decimal column returns a computed DECIMAL as a NUMERIC buffer
		// System.Data.OleDb cannot decode, and the error varies run to run, so there is nothing stable to pin —
		// linq2db#5954. MIN and MAX read the stored column and bind fine there, and Jet ODBC reads this query too.
		public void PivotsAvgCell([DataSources(ProviderName.AccessJetOleDb)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.MultiRowData);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Average(x => x.Amount), y => "Avg" + Year(y))
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0].Cells["AvgY2000"].ShouldBe(20m);

			result[1].Key.ShouldBe("B");
			result[1].Cells["AvgY2000"].ShouldBe(5m);
		}

		[Table]
		sealed class RegionSales
		{
			[PrimaryKey(ProviderName.Ydb, 1)]
			[Column] public string   Category { get; set; } = null!;
			[PrimaryKey(ProviderName.Ydb, 2)]
			[Column] public string   Region   { get; set; } = null!;
			[PrimaryKey(ProviderName.Ydb, 3)]
			[Column] public int      Year     { get; set; }
			[Column] public decimal? Amount   { get; set; }

			public static readonly RegionSales[] Data =
			{
				new() { Category = "A", Region = "EU", Year = 2000, Amount = 10m },
				new() { Category = "A", Region = "EU", Year = 2010, Amount = 20m },
				new() { Category = "A", Region = "US", Year = 2000, Amount = 3m  },
				new() { Category = "B", Region = "EU", Year = 2000, Amount = 5m  },
			};

			// A region named like a year, so a cell-name collision between two pivots would surface.
			public static readonly RegionSales[] YearNamedRegionData =
			{
				new() { Category = "A", Region = "EU",   Year = 2000, Amount = 10m },
				new() { Category = "A", Region = "2000", Year = 2010, Amount = 20m },
				new() { Category = "B", Region = "EU",   Year = 2010, Amount = 5m  },
			};
		}

		[Test]
		public void PivotsOnACompositeKey([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(RegionSales.Data);

			var query = t
				.GroupBy(x => new { x.Category, x.Region })
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key.Category, StringComparer.Ordinal)
				.ThenBy(r => r.Key.Region, StringComparer.Ordinal)
				.ToList();

			// groups: (A,EU) Y2000=10 Y2010=20; (A,US) Y2000=3; (B,EU) Y2000=5
			result.Count.ShouldBe(3);

			result[0].Key.Category  .ShouldBe("A");
			result[0].Key.Region    .ShouldBe("EU");
			result[0].Cells["Y2000"].ShouldBe(10m);
			result[0].Cells["Y2010"].ShouldBe(20m);

			result[1].Key.Region    .ShouldBe("US");
			result[1].Cells["Y2000"].ShouldBe(3m);

			result[2].Key.Category  .ShouldBe("B");
			result[2].Cells["Y2000"].ShouldBe(5m);
		}

		/// <summary>Two pivots of one group over different columns, each in its own member; equal names do not collide.</summary>
		[Test]
		public void TwoPivotsInOneProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(RegionSales.YearNamedRegionData);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					ByYear = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount))
						.AsDictionary(),
					ByRegion = g
						.Pivot(x => x.Region, new[] { "EU", "2000" })
						.Cell(rows => rows.Sum(x => x.Amount))
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].ByYear["2000"]  .ShouldBe(10m);
			result[0].ByYear["2010"]  .ShouldBe(20m);
			result[0].ByRegion["EU"]  .ShouldBe(10m);
			result[0].ByRegion["2000"].ShouldBe(20m);

			result[1].ByYear["2000"]  .ShouldBeNull();
			result[1].ByYear["2010"]  .ShouldBe(5m);
			result[1].ByRegion["EU"]  .ShouldBe(5m);
			result[1].ByRegion["2000"].ShouldBeNull();
		}

		[Table]
		sealed class QuarterAmounts
		{
			[PrimaryKey(ProviderName.Ydb, 1)]
			[Column] public string   Category { get; set; } = null!;
			[PrimaryKey(ProviderName.Ydb, 2)]
			[Column] public int      Year     { get; set; }
			[PrimaryKey(ProviderName.Ydb, 3)]
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
		public void PivotsOnACompositeValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterAmounts.Data);

			// The pivoted column is a pair, so the cell predicate is an AND of the two member comparisons.
			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => new { x.Year, x.Quarter }, new[] { new { Year = 2000, Quarter = 1 }, new { Year = 2000, Quarter = 2 } })
						.Cell(rows => rows.Sum(x => x.Amount), v => Year(v.Year) + "Q" + v.Quarter.ToString(CultureInfo.InvariantCulture))
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			// A: Q1=10, Q2=20; B: Q1=5, Q2=null
			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0].Cells["Y2000Q1"].ShouldBe(10m);
			result[0].Cells["Y2000Q2"].ShouldBe(20m);

			result[1].Key.ShouldBe("B");
			result[1].Cells["Y2000Q1"].ShouldBe(5m);
			result[1].Cells["Y2000Q2"].ShouldBeNull();
		}

		/// <summary>
		/// A cell is a server-side column: it can be filtered on and re-projected after the pivot, though it lives in a
		/// nested member the result type never declares.
		/// </summary>
		[Test]
		public void ComposesAfterAConstantPivot([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			// Where on a cell plus a projection that drops another one - stresses column pruning.
			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Where(r => Sql.Property<decimal?>(r.Cells, "Y2010") >= 15)
				.OrderByDescending(r => Sql.Property<decimal?>(r.Cells, "Y2010"))
				.Select(r => new { r.Key, Y2010 = Sql.Property<decimal?>(r.Cells, "Y2010") });

			var result = query.ToList();

			result.Count.ShouldBe(2);
			result[0].Key.ShouldBe("A");
			result[0].Y2010.ShouldBe(20m);
			result[1].Key.ShouldBe("B");
			result[1].Y2010.ShouldBe(15m);
		}

		// Asserts SQL text: a filter on a cell is a HAVING over its aggregate, and a cell nothing reads is not selected.
		[Test]
		public void CompositionKeepsTheCellServerSide([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			_ = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Where(r => Sql.Property<decimal?>(r.Cells, "Y2010") >= 15)
				.Select(r => new { r.Key, Y2010 = Sql.Property<decimal?>(r.Cells, "Y2010") })
				.ToList();

			var sql = db.LastQuery!;

			sql.ShouldContain("HAVING");

			// Y2010 in the select list and in HAVING; Y2000 is never read.
			Occurrences(sql, "WHEN")
				.ShouldBe(2);
		}

		/// <summary>
		/// <see cref="PivotCells.Get{T}"/> and the indexer filter and order on a cell's column, when the member is typed
		/// <see cref="PivotCells"/>. The filter keeps one group only by the named cell, and the ordering differs from the
		/// grouping order, so a wrong cell or a dropped clause reads differently.
		/// </summary>
		[Test]
		public void ComposesThroughTheCellsMembers([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var pivoted = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			var filtered = pivoted
				.Where(r => r.Cells.Get<decimal?>("Y2010") > 16)
				.Select(r => r.Key)
				.ToList();

			filtered.ShouldBe(new[] { "A" });

			var ordered = pivoted
				.OrderBy(r => (decimal?)r.Cells["Y2000"])
				.Select(r => new { r.Key, Y2000 = r.Cells.Get<decimal?>("Y2000") })
				.ToList();

			ordered
				.Select(r => (r.Key, r.Y2000))
				.ShouldBe(new (string, decimal?)[] { ("B", 5m), ("A", 10m) });
		}

		/// <summary>A changed cell name is a different query, not a cache hit on the column the previous name read.</summary>
		[Test]
		public void ACapturedCellNameKeysTheQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var name = "Y2000";

			List<string> Keys() => t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Where(r => r.Cells.Get<decimal?>(name) > 12)
				.Select(r => r.Key)
				.ToList();

			var keys = Keys();

			keys.ShouldBeEmpty();

			name = "Y2010";

			Keys()
				.OrderBy(k => k, StringComparer.Ordinal)
				.ShouldBe(new[] { "A", "B" });
		}

		/// <summary>
		/// Cells read back from a pivot and captured by a later query are a value: they are not mistaken for the pivot's
		/// columns.
		/// </summary>
		[Test]
		public void CapturedCellsAreAValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var row = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.ToList()
				.Single(r => r.Key == "A");

			t.Where(x => x.Amount == row.Cells.Get<decimal?>("Y2010"))
				.Select(x => x.Id)
				.ToList()
				.ShouldBe(new[] { 2 });

			t.Where(x => x.Amount == (decimal?)row.Cells["Y2010"])
				.Select(x => x.Id)
				.ToList()
				.ShouldBe(new[] { 2 });
		}

		/// <summary>
		/// In a projection, <see cref="PivotCells.Get{T}"/> and the indexer read the materialized cells: a name no cell
		/// carries reads <see langword="null"/>, and a cell of another type reads <see langword="default"/>.
		/// </summary>
		[Test]
		public void AProjectedCellReadKeepsItsInMemoryContract([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var result = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.OrderBy(r => r.Key)
				.Select(r => new
				{
					Absent      = r.Cells.Get<decimal?>("Y2099"),
					AbsentIndex = (decimal?)r.Cells["Y2099"],
					AsInt       = r.Cells.Get<int>("Y2010"),
					AsString    = r.Cells.Get<string>("Y2010"),
					Present     = r.Cells.Get<decimal?>("Y2010"),
				})
				.ToList();

			result.Count.ShouldBe(2);
			result.ShouldAllBe(r => r.Absent == null && r.AbsentIndex == null && r.AsInt == 0 && r.AsString == null);
			result
				.Select(r => r.Present)
				.ShouldBe(new decimal?[] { 20m, 15m });
		}

		/// <summary>
		/// A projected cell name read from the row has no value until the query runs, so the cached query compares it as part
		/// of the tree: the second execution reads as the first.
		/// </summary>
		[Test]
		public void ARowDependentCellNameReadsOnEveryExecution([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var pivoted = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.OrderBy(r => r.Key);

			for (var execution = 0; execution < 2; execution++)
			{
				pivoted
					.Select(r => r.Cells.Get<decimal?>(r.Key == "A" ? "Y2000" : "Y2010"))
					.ToList()
					.ShouldBe(new decimal?[] { 10m, 15m });

				pivoted
					.Select(r => (decimal?)r.Cells[r.Key == "A" ? "Y2000" : "Y2010"])
					.ToList()
					.ShouldBe(new decimal?[] { 10m, 15m });
			}
		}

		/// <summary>A changed name read through the indexer is a different query, as through <see cref="PivotCells.Get{T}"/>.</summary>
		[Test]
		public void ACapturedCellNameKeysAnIndexerRead([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var name = "Y2000";

			List<string> Keys() => t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Where(r => (decimal?)r.Cells[name] > 12)
				.Select(r => r.Key)
				.ToList();

			var keys = Keys();

			keys.ShouldBeEmpty();

			name = "Y2010";

			Keys()
				.OrderBy(k => k, StringComparer.Ordinal)
				.ShouldBe(new[] { "A", "B" });
		}

		/// <summary>A compiled query's cell name, passed as an argument, keys the build it reuses.</summary>
		// Compiled-query build identity is provider-agnostic.
		[Test]
		public void ACompiledCellNameKeysTheBuild([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var keys = CompiledQuery.Compile((IDataContext dc, string name) => dc
				.GetTable<CategorySales>()
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Where(r => r.Cells.Get<decimal?>(name) > 12)
				.Select(r => r.Key));

			keys(db, "Y2000")
				.ToList()
				.ShouldBeEmpty();

			keys(db, "Y2010")
				.OrderBy(k => k)
				.ToList()
				.ShouldBe(new[] { "A", "B" });
		}

		/// <summary>
		/// Where SQL is required, <see cref="PivotCells.Get{T}"/> reads the cell's column converted to <c>T</c>, not the
		/// in-memory default a projection reads for a cell of another type.
		/// </summary>
		[Test]
		public void AGetInASqlPositionReadsTheCellAsItsType([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.FractionalData);

			// The provider's own cast of the same amounts - Distinct puts it in SQL - which truncates or rounds as the cell's does.
			var cast = t
				.Where(x => x.Year == 2010)
				.Select(x => (int)x.Amount!.Value)
				.Distinct()
				.ToList();

			var castOfA = t
				.Where(x => x.Year == 2010 && x.Category == "A")
				.Select(x => (int)x.Amount!.Value)
				.Distinct()
				.Single();

			var pivoted = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			pivoted
				.Where(r => r.Cells.Get<int>("Y2010") == castOfA)
				.Select(r => r.Key)
				.ToList()
				.ShouldBe(new[] { "A" });

			pivoted
				.Select(r => r.Cells.Get<int>("Y2010"))
				.Distinct()
				.ToList()
				.OrderBy(v => v)
				.ShouldBe(cast.OrderBy(v => v));

			// A projection keeps the in-memory read: a decimal cell is not an int.
			pivoted
				.OrderBy(r => r.Key)
				.Select(r => r.Cells.Get<int>("Y2010"))
				.ToList()
				.ShouldBe(new[] { 0, 0 });
		}

		/// <summary>Where SQL is required, a name no cell carries is a named error rather than a getter that throws later.</summary>
		[Test]
		public void AnAbsentCellNameInASqlPositionIsNamed([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var pivoted = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			var expected = Message(ErrorHelper.Pivot.Error_PivotCellNotFound, "Y2099");

			Assert.That(
				() => pivoted
					.Where(r => r.Cells.Get<decimal?>("Y2099") > 1)
					.ToList(),
				Throws.TypeOf<LinqToDBException>().With.Message.Contains(expected));

			Assert.That(
				() => pivoted
					.Select(r => r.Cells.Get<decimal?>("Y2099"))
					.Distinct()
					.ToList(),
				Throws.TypeOf<LinqToDBException>().With.Message.Contains(expected));
		}

		/// <summary>
		/// Where SQL is required, a read SQL cannot translate is a named error: a type the cell has no conversion to, and an
		/// absent name in a grouping key or an unpivot column.
		/// </summary>
		[Test]
		public void ACellReadSqlCannotTranslateIsNamed([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var pivoted = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			Assert.That(
				() => pivoted
					.Where(r => r.Cells.Get<Uri>("Y2010") != null)
					.ToList(),
				Throws.TypeOf<LinqToDBException>().With.Message.Contains(Message(ErrorHelper.Pivot.Error_PivotCellType, "Y2010", typeof(Uri))));

			Assert.That(
				() => pivoted
					.Unpivot(
						(row, name, value) => new { row.Key, Value = value },
						x => x.Cells.Get<Uri>("Y2010"))
					.ToList(),
				Throws.TypeOf<LinqToDBException>().With.Message.Contains(Message(ErrorHelper.Pivot.Error_PivotCellType, "Y2010", typeof(Uri))));

			// Kept NULLs leave no filter, so the column alone reports it.
			Assert.That(
				() => pivoted
					.Unpivot(
						UnpivotNulls.IncludeNulls,
						(row, name, value) => new { row.Key, Value = value },
						x => x.Cells.Get<Uri>("Y2010"))
					.ToList(),
				Throws.TypeOf<LinqToDBException>().With.Message.Contains(Message(ErrorHelper.Pivot.Error_PivotCellType, "Y2010", typeof(Uri))));

			var absent = Message(ErrorHelper.Pivot.Error_PivotCellNotFound, "Y2099");

			Assert.That(
				() => pivoted
					.Unpivot(
						UnpivotNulls.IncludeNulls,
						(row, name, value) => new { row.Key, Value = value },
						x => x.Cells.Get<int>("Y2099"))
					.ToList(),
				Throws.TypeOf<LinqToDBException>().With.Message.Contains(absent));

			Assert.That(
				() => pivoted
					.GroupBy(r => r.Cells.Get<decimal?>("Y2099"))
					.Select(g => g.Count())
					.ToList(),
				Throws.TypeOf<LinqToDBException>().With.Message.Contains(absent));
		}

		/// <summary>The conversions an unpivot column writes over a cell read are kept.</summary>
		[Test]
		public void UnpivotKeepsTheConversionsOverACellRead([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var result = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Unpivot(
					(row, name, value) => new { row.Key, Value = value },
					x => (int?)(decimal?)x.Cells["Y2000"])
				.Where(u => u.Value == 10)
				.Select(u => u.Key)
				.ToList();

			result.ShouldBe(new[] { "A" });
		}

		/// <summary>An unpivot column reads a cell as the type the read names, not as the cell's own type.</summary>
		[Test]
		public void UnpivotConvertsACellReadToItsType([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var pivoted = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			pivoted
				.Unpivot(
					(row, name, value) => new { row.Key, Value = value },
					x => x.Cells.Get<int>("Y2000"))
				.ToList()
				.OrderBy(u => u.Key, StringComparer.Ordinal)
				.Select(u => (u.Key, u.Value))
				.ShouldBe(new[] { ("A", 10), ("B", 5) });

			pivoted
				.Unpivot(
					(row, name, value) => new { row.Key, Value = value },
					x => (int?)x.Cells["Y2000"])
				.ToList()
				.OrderBy(u => u.Key, StringComparer.Ordinal)
				.Select(u => (u.Key, u.Value))
				.ShouldBe(new (string, int?)[] { ("A", 10), ("B", 5) });
		}

		/// <summary>
		/// An unpivot column reads a cell as a filter does: cast in SQL, as the provider casts, over every column, and NULL for an
		/// empty cell kept by <see cref="UnpivotNulls.IncludeNulls"/>.
		/// </summary>
		[Test]
		public void UnpivotCastsACellReadAsTheProviderDoes([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.FractionalData);

			// The provider's own cast of the fractional amount - Distinct puts it in SQL.
			var castOfA = t
				.Where(x => x.Year == 2010 && x.Category == "A")
				.Select(x => (int)x.Amount!.Value)
				.Distinct()
				.Single();

			var pivoted = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010, 2020 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			pivoted
				.Unpivot(
					(row, name, value) => new { row.Key, Name = name, Value = value },
					x => x.Cells.Get<int>("Y2000"),
					x => x.Cells.Get<int>("Y2010"))
				.ToList()
				.OrderBy(u => u.Key, StringComparer.Ordinal)
				.ThenBy(u => u.Name, StringComparer.Ordinal)
				.Select(u => (u.Key, u.Name, u.Value))
				.ShouldBe(new[] { ("A", "Y2000", 10), ("A", "Y2010", castOfA), ("B", "Y2000", 5), ("B", "Y2010", 15) });

			pivoted
				.Unpivot(
					UnpivotNulls.IncludeNulls,
					(row, name, value) => new { row.Key, Value = value },
					x => (int?)x.Cells["Y2020"])
				.ToList()
				.OrderBy(u => u.Key, StringComparer.Ordinal)
				.Select(u => (u.Key, u.Value))
				.ShouldBe(new (string, int?)[] { ("A", null), ("B", null) });

			pivoted
				.Unpivot(
					UnpivotNulls.IncludeNulls,
					(row, name, value) => new { row.Key, Value = value },
					x => x.Cells.Get<int>("Y2020"))
				.ToList()
				.OrderBy(u => u.Key, StringComparer.Ordinal)
				.Select(u => (u.Key, u.Value))
				.ShouldBe(new[] { ("A", 0), ("B", 0) });
		}

		/// <summary>An unpivot column's value can feed a subquery in the result selector.</summary>
		[Test]
		public void UnpivotValueFeedsASubquery([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Unpivot(
					(row, name, value) => new { row.Key, Rows = t.Count(o => o.Amount == value) },
					x => x.Cells.Get<decimal?>("Y2000"))
				.ToList()
				.OrderBy(u => u.Key, StringComparer.Ordinal)
				.Select(u => (u.Key, u.Rows))
				.ShouldBe(new[] { ("A", 1), ("B", 1) });
		}

		/// <summary>A pivot's cells unpivot under the names the pivot gave them, not the path that reaches them.</summary>
		[Test]
		public void UnpivotsTheCellsUnderTheirOwnNames([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var result = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Unpivot(
					(row, name, value) => new { row.Key, Name = name, Value = value },
					x => Sql.Property<decimal?>(x.Cells, "Y2000"),
					x => Sql.Property<decimal?>(x.Cells, "Y2010"))
				.ToArray()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ThenBy(r => r.Name, StringComparer.Ordinal)
				.Select(r => (r.Key, r.Name, r.Value));

			result.ShouldBe(new (string, string, decimal?)[] { ("A", "Y2000", 10m), ("A", "Y2010", 20m), ("B", "Y2000", 5m), ("B", "Y2010", 15m) });
		}

		/// <summary>A pivot's cells unpivot through the indexer as through <c>Sql.Property</c>.</summary>
		[Test]
		public void UnpivotsTheCellsThroughTheIndexer([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var result = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Unpivot(
					(row, name, value) => new { row.Key, Name = name, Value = value },
					x => (decimal?)x.Cells["Y2000"],
					x => (decimal?)x.Cells["Y2010"])
				.ToArray()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ThenBy(r => r.Name, StringComparer.Ordinal)
				.Select(r => (r.Key, r.Name, r.Value));

			result.ShouldBe(new (string, string, decimal?)[] { ("A", "Y2000", 10m), ("A", "Y2010", 20m), ("B", "Y2000", 5m), ("B", "Y2010", 15m) });
		}

		/// <summary>Two pivots' cells of one name unpivot under the paths that tell them apart.</summary>
		[Test]
		public void UnpivotsSameNamedCellsOfTwoPivotsUnderTheirPaths([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.MultiRowData);

			var result = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Total = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
					Top = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Max(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Unpivot(
					(row, name, value) => new { row.Key, Name = name, Value = value },
					x => x.Total.Get<decimal?>("Y2000"),
					x => x.Top.Get<decimal?>("Y2000"))
				.ToArray()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ThenBy(r => r.Name, StringComparer.Ordinal)
				.Select(r => (r.Key, r.Name, r.Value))
				.ToArray();

			result.ShouldBe(new (string, string, decimal?)[] { ("A", "Top.Y2000", 30m), ("A", "Total.Y2000", 40m), ("B", "Top.Y2000", 5m), ("B", "Total.Y2000", 5m) });
		}

		sealed class InterfaceCellsPair
		{
			public string                               Category { get; set; } = null!;
			public IReadOnlyDictionary<string, object?> Total    { get; set; } = null!;
			public IReadOnlyDictionary<string, object?> Top      { get; set; } = null!;
		}

		/// <summary>Same-named cells read through a cast of interface-typed members unpivot under the members' paths.</summary>
		[Test]
		public void UnpivotsCastInterfaceMembersUnderTheirPaths([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.MultiRowData);

			var result = t
				.GroupBy(x => x.Category)
				.Select(g => new InterfaceCellsPair
				{
					Category = g.Key,
					Total    = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
					Top      = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Max(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Unpivot(
					(row, name, value) => new { row.Category, Name = name, Value = value },
					x => ((PivotCells)x.Total).Get<decimal?>("Y2000"),
					x => ((PivotCells)x.Top).Get<decimal?>("Y2000"))
				.ToArray()
				.OrderBy(r => r.Category, StringComparer.Ordinal)
				.ThenBy(r => r.Name, StringComparer.Ordinal)
				.Select(r => (r.Category, r.Name, r.Value))
				.ToArray();

			result.ShouldBe(new (string, string, decimal?)[] { ("A", "Top.Y2000", 30m), ("A", "Total.Y2000", 40m), ("B", "Top.Y2000", 5m), ("B", "Total.Y2000", 5m) });
		}

		/// <summary>The cells go into a member of an anonymous type, which cannot carry a dynamic-columns store.</summary>
		[Test]
		public void PivotsIntoAnAnonymousType([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var years = new[] { 2000, 2010 };

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					Category = g.Key,
					Total    = g.Sum(x => x.Amount),
					Cells    = g
						.Pivot(x => x.Year, years)
						.Cell(rows => rows.Sum(x => x.Amount), y => "AMT_"  + Year(y))
						.Cell(rows => rows.Max(x => x.Note),   y => "NOTE_" + Year(y))
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Category, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Category.ShouldBe("A");
			result[0].Total.ShouldBe(30m);
			result[0].Cells["AMT_Y2000"].ShouldBe(10m);
			result[0].Cells["NOTE_Y2010"].ShouldBe("a1");

			result[1].Category.ShouldBe("B");
			result[1].Total.ShouldBe(20m);
			result[1].Cells["AMT_Y2010"].ShouldBe(15m);
			result[1].Cells["NOTE_Y2000"].ShouldBe("b0");
		}

		static readonly int[] _pivotedParents = { 2, 3 };

		// The children of the pivoted parents. Each child belongs to one parent, so a cell read from the wrong group shows.
		int[] ChildIdsOfPivotedParents()
			=> Child
				.Where(c => _pivotedParents.Contains(c.ParentID))
				.Select(c => c.ChildID)
				.Distinct()
				.OrderBy(id => id)
				.ToArray();

		// What a SUM cell reads: null when the parent has no such child.
		int? ExpectedChildSum(int parentId, int childId)
		{
			var rows = Child
				.Where(c => c.ParentID == parentId && c.ChildID == childId)
				.ToList();

			return rows.Count == 0 ? null : rows.Sum(c => c.ChildID);
		}

		/// <summary>The pivot in query syntax, over a join whose grouped row is anonymous.</summary>
		[Test]
		public void PivotsInQuerySyntaxOverAJoin([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var childIds = ChildIdsOfPivotedParents();

			var query =
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				where p.ParentID == 2 || p.ParentID == 3
				group new { p, c } by p.ParentID into g
				select new
				{
					ParentID = g.Key,
					Cells    = g
						.Pivot(x => x.c.ChildID, childIds)
						.Cell(rows => rows.Sum(x => x.c.ChildID))
						.AsDictionary(),
				};

			var result = query
				.ToList()
				.OrderBy(r => r.ParentID)
				.ToList();

			result
				.Select(r => r.ParentID)
				.ShouldBe(_pivotedParents);

			foreach (var row in result)
			{
				foreach (var childId in childIds)
				{
					row.Cells[childId.ToString(CultureInfo.InvariantCulture)]
						.ShouldBe(ExpectedChildSum(row.ParentID, childId));
				}
			}
		}

		// Call-site guard: throws while the query is built, before any SQL.
		[Test]
		public void CellNamedAfterACellsMemberThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount), _ => nameof(PivotCells.Store))
						.AsDictionary(),
				});

			Action act = () => query.ToList();

			Assert.That(() => act(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.DynamicColumns.Error_DynamicColumnShadowsMember, nameof(PivotCells.Store), nameof(PivotCells))));
		}

		// Call-site guards: each throws while the query is built, before any SQL.
		[Test]
		public void ArgumentGuardsThrow([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			// A runtime value set read from a nullable column can carry null, whose default name is empty.
			var emptyName = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Note, new[] { "a0", null })
						.Cell(rows => rows.Sum(x => x.Amount))
						.AsDictionary(),
				});

			var duplicateName = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), _ => "Same")
						.AsDictionary(),
				});

			var noCell = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.AsDictionary(),
				});

			var noTemplate = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cells(Array.Empty<PivotCell<Sales, int>>())
						.AsDictionary(),
				});

			int[] noValues = null!;

			var nullValues = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, noValues)
						.Cell(rows => rows.Sum(x => x.Amount))
						.AsDictionary(),
				});

			var selectAfterCell = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Items = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount))
						.Select((y, rows) => rows.Sum(x => x.Amount)),
				});

			Action emptyNameAct       = () => emptyName.ToList();
			Action duplicateNameAct   = () => duplicateName.ToList();
			Action noCellAct          = () => noCell.ToList();
			Action noTemplateAct      = () => noTemplate.ToList();
			Action nullValuesAct      = () => nullValues.ToList();
			Action selectAfterCellAct = () => selectAfterCell.ToList();

			Assert.That(() => emptyNameAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Pivot.Error_PivotValueEmptyName, "")));

			Assert.That(() => duplicateNameAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Pivot.Error_PivotDuplicateName, "Same")));

			Assert.That(() => noCellAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(ErrorHelper.Pivot.Error_PivotNeedsCellTemplate));

			Assert.That(() => noTemplateAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(ErrorHelper.Pivot.Error_PivotNeedsCellTemplate));

			Assert.That(() => nullValuesAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Pivot.Error_PivotArgumentNull, "value set")));

			Assert.That(() => selectAfterCellAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(ErrorHelper.Pivot.Error_PivotSelectAfterCells));
		}

		/// <summary>
		/// Only a field or a property shadows a cell; a cell named after a method or the indexer stays reachable, through
		/// the indexer and through <c>Sql.Property</c> alike.
		/// </summary>
		[Test]
		public void CellsNamedAfterMethodsAndTheIndexer([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), y => y == 2000 ? "Item" : "ToString")
						.AsDictionary(),
				})
				.Where(r => Sql.Property<decimal?>(r.Cells, "Item") >= 10)
				.Select(r => new
				{
					r.Key,
					Item = Sql.Property<decimal?>(r.Cells, "Item"),
					Text = Sql.Property<decimal?>(r.Cells, "ToString"),
					r.Cells,
				});

			var result = query.ToList();

			result.Count.ShouldBe(1);

			result[0].Key              .ShouldBe("A");
			result[0].Item             .ShouldBe(10m);
			result[0].Text             .ShouldBe(20m);
			result[0].Cells["Item"]    .ShouldBe(10m);
			result[0].Cells["ToString"].ShouldBe(20m);
		}

		[Test]
		public void PivotsIntoANestedObject([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					Category = g.Key,
					Data     = new
					{
						Total = g.Sum(x => x.Amount),
						Cells = g
							.Pivot(x => x.Year, new[] { 2000, 2010 })
							.Cell(rows => rows.Sum(x => x.Amount), Year)
							.AsDictionary(),
					},
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Category, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Category.ShouldBe("A");
			result[0].Data.Total.ShouldBe(30m);
			result[0].Data.Cells["Y2000"].ShouldBe(10m);
			result[0].Data.Cells["Y2010"].ShouldBe(20m);

			result[1].Category.ShouldBe("B");
			result[1].Data.Total.ShouldBe(20m);
			result[1].Data.Cells["Y2000"].ShouldBe(5m);
			result[1].Data.Cells["Y2010"].ShouldBe(15m);
		}

		sealed class RootStoreRow
		{
			public string      Category { get; set; } = null!;
			public NestedCells Inner    { get; set; } = null!;

			[DynamicColumnsStore]
			public IDictionary<string, object>? Extra { get; set; }
		}

		sealed class NestedCells
		{
			public PivotCells Cells { get; set; } = null!;
		}

		/// <summary>The cells belong to the member they are assigned to, not to a dynamic-columns store the result type also has.</summary>
		[Test]
		public void PivotsIntoANestedMemberBesideARootStore([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new RootStoreRow
				{
					Category = g.Key,
					Inner    = new NestedCells
					{
						Cells = g
							.Pivot(x => x.Year, new[] { 2000, 2010 })
							.Cell(rows => rows.Sum(x => x.Amount), Year)
							.AsDictionary(),
					},
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Category, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Inner.Cells["Y2000"].ShouldBe(10m);
			result[0].Inner.Cells["Y2010"].ShouldBe(20m);
			result[1].Inner.Cells["Y2000"].ShouldBe(5m);
			result[1].Inner.Cells["Y2010"].ShouldBe(15m);

			result.ShouldAllBe(r => r.Extra == null || r.Extra.Count == 0);
		}

		sealed class InterfaceCellsRow
		{
			public string                               Category { get; set; } = null!;
			public IReadOnlyDictionary<string, object?> Cells    { get; set; } = null!;
		}

		/// <summary>
		/// A cell of a member declared through the dictionary interface is filtered on through <c>Sql.Property</c>, or
		/// through <see cref="PivotCells.Get{T}"/> once the member is cast back.
		/// </summary>
		[Test]
		public void FiltersAnInterfaceTypedMember([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.Select(x => new { x.Category, x.Year, x.Amount })
				.GroupBy(x => x.Category)
				.Select(g => new InterfaceCellsRow
				{
					Category = g.Key,
					Cells    = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			query
				.Where(r => Sql.Property<decimal?>(r.Cells, "Y2010") > 16)
				.Select(r => r.Category)
				.ToList()
				.ShouldBe(new[] { "A" });

			query
				.Where(r => ((PivotCells)r.Cells).Get<decimal?>("Y2010") > 16)
				.Select(r => r.Category)
				.ToList()
				.ShouldBe(new[] { "A" });
		}

		/// <summary>
		/// <c>Sql.Property&lt;object&gt;</c> under a cast reads the cell's column cast in SQL, through a member typed
		/// <see cref="PivotCells"/> and through one typed as the dictionary interface.
		/// </summary>
		[Test]
		public void ReadsACellThroughAnObjectProperty([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var source = t.Select(x => new { x.Category, x.Year, x.Amount });

			source
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					Category = g.Key,
					Cells    = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.OrderBy(r => r.Category)
				.Select(r => (decimal?)Sql.Property<object>(r.Cells, "Y2010"))
				.ToList()
				.ShouldBe(new decimal?[] { 20m, 15m });

			source
				.GroupBy(x => x.Category)
				.Select(g => new InterfaceCellsRow
				{
					Category = g.Key,
					Cells    = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.OrderBy(r => r.Category)
				.Select(r => (decimal?)Sql.Property<object>(r.Cells, "Y2010"))
				.ToList()
				.ShouldBe(new decimal?[] { 20m, 15m });
		}

		/// <summary>A set operation over rows with an interface-typed member still filters on its cells.</summary>
		[Test]
		public void FiltersAnInterfaceTypedMemberUnderConcat([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.Select(x => new { x.Category, x.Year, x.Amount })
				.GroupBy(x => x.Category)
				.Select(g => new InterfaceCellsRow
				{
					Category = g.Key,
					Cells    = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			query
				.Concat(query)
				.Where(r => Sql.Property<decimal?>(r.Cells, "Y2010") > 16)
				.Select(r => r.Category)
				.ToList()
				.ShouldBe(new[] { "A", "A" });
		}

		/// <summary>A result type declares the member through the dictionary interface.</summary>
		[Test]
		public void PivotsIntoAnInterfaceTypedMember([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.Select(x => new { x.Category, x.Year, x.Amount })
				.GroupBy(x => x.Category)
				.Select(g => new InterfaceCellsRow
				{
					Category = g.Key,
					Cells    = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Category, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Cells.Count.ShouldBe(2);
			result[0].Cells["Y2000"].ShouldBe(10m);
			result[0].Cells["Y2010"].ShouldBe(20m);
			result[1].Cells["Y2000"].ShouldBe(5m);
			result[1].Cells["Y2010"].ShouldBe(15m);

			result[0].Cells
				.ContainsKey("Y2020")
				.ShouldBeFalse();

			Action absentCell = () => _ = result[0].Cells["Y2020"];

			Assert.That(() => absentCell(), Throws.TypeOf<KeyNotFoundException>());
		}

		static Expression<Func<IEnumerable<Sales>, string?>> MaxNote() => rows => rows.Max(x => x.Note);

		/// <summary>A cell aggregate built outside the query - a captured local, or a helper's result.</summary>
		[Test]
		public void PivotsWithAPrebuiltAggregate([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			Expression<Func<IEnumerable<Sales>, decimal?>> sum = rows => rows.Sum(x => x.Amount);

			var cells = new[]
			{
				PivotCell<Sales, int>.Create(sum,       y => "AMT_"  + Year(y)),
				PivotCell<Sales, int>.Create(MaxNote(), y => "NOTE_" + Year(y)),
			};

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cells(cells)
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Cells["AMT_Y2000"].ShouldBe(10m);
			result[0].Cells["NOTE_Y2010"].ShouldBe("a1");
			result[1].Cells["AMT_Y2010"].ShouldBe(15m);
			result[1].Cells["NOTE_Y2000"].ShouldBe("b0");
		}

		/// <summary>A cell set decided at run time: which cells the pivot carries depends on a flag.</summary>
		[Test]
		public void PivotsARuntimeCellSet([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			List<PivotCell<Sales, int>> Templates(bool includeNotes)
			{
				var templates = new List<PivotCell<Sales, int>>
				{
					PivotCell<Sales, int>.Create(rows => rows.Sum(x => x.Amount), y => "AMT_" + Year(y)),
				};

				if (includeNotes)
					templates.Add(PivotCell<Sales, int>.Create(MaxNote(), y => "NOTE_" + Year(y)));

				return templates;
			}

			IQueryable<KeyValuePair<string, PivotCells>> Build(bool includeNotes)
			{
				var templates = Templates(includeNotes);

				return t
					.GroupBy(x => x.Category)
					.Select(g => new KeyValuePair<string, PivotCells>(
						g.Key,
						g
							.Pivot(x => x.Year, new[] { 2000, 2010 })
							.Cells(templates)
							.AsDictionary()));
			}

			var narrow = Build(false)
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			var wide = Build(true)
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			narrow[0].Value.Store.Keys
				.OrderBy(k => k, StringComparer.Ordinal)
				.ShouldBe(new[] { "AMT_Y2000", "AMT_Y2010" });

			wide[0].Value.Store.Keys
				.OrderBy(k => k, StringComparer.Ordinal)
				.ShouldBe(new[] { "AMT_Y2000", "AMT_Y2010", "NOTE_Y2000", "NOTE_Y2010" });

			wide[1].Value["NOTE_Y2000"].ShouldBe("b0");
			wide[1].Value["AMT_Y2010"] .ShouldBe(15m);
		}

		/// <summary>A cell name is a key, not SQL text: quotes and spaces survive, and names differing only in them stay apart.</summary>
		[Test]
		public void CellNamesNeedNoIdentifierSyntax([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), y => y == 2000 ? "Men's \"[2000]\"" : "Mens 2000")
						.AsDictionary(),
				})
				.Where(r => Sql.Property<decimal?>(r.Cells, "Men's \"[2000]\"") >= 5);

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Cells["Men's \"[2000]\""].ShouldBe(10m);
			result[0].Cells["Mens 2000"].ShouldBe(20m);
			result[1].Cells["Men's \"[2000]\""].ShouldBe(5m);
			result[1].Cells["Mens 2000"].ShouldBe(15m);
		}

		/// <summary>The cells as the whole projection: a lambda body is a place a terminal may stand.</summary>
		[Test]
		public void PivotsAsTheWholeProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => g
					.Pivot(x => x.Year, new[] { 2000, 2010 })
					.Cell(rows => rows.Sum(x => x.Amount), Year)
					.AsDictionary());

			var result = query
				.ToList()
				.OrderBy(r => r["Y2000"] as decimal?)
				.ToList();

			result.Count.ShouldBe(2);

			result[0]["Y2000"].ShouldBe(5m);
			result[0]["Y2010"].ShouldBe(15m);
			result[1]["Y2000"].ShouldBe(10m);
			result[1]["Y2010"].ShouldBe(20m);
		}

		/// <summary>A cell can carry any aggregate the provider can translate over a grouping - here a distinct count.</summary>
		[Test]
		// An aggregate over a distinct set lowers to a lateral subquery, which Access cannot join.
		[ThrowsRequiredOuterJoins(TestProvName.AllAccess)]
		public void PivotsWithADistinctCountCell([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.DuplicateData);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Select(x => x.Amount).Distinct().Count(), y => "Distinct" + Year(y))
						.Cell(rows => rows.Count(),                                  y => "Rows"     + Year(y))
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0].Cells["DistinctY2000"].ShouldBe(2);
			result[0].Cells["RowsY2000"]    .ShouldBe(3);

			result[1].Key.ShouldBe("B");
			result[1].Cells["DistinctY2000"].ShouldBe(1);
			result[1].Cells["RowsY2000"]    .ShouldBe(1);
		}

		[Table]
		sealed class StrictSales
		{
			[PrimaryKey(ProviderName.Ydb, 1)]
			[Column] public string   Category { get; set; } = null!;
			[PrimaryKey(ProviderName.Ydb, 2)]
			[Column] public int      Year     { get; set; }
			[Column] public int      Amount   { get; set; }
			[Column] public DateTime At       { get; set; }

			// No row for (A, 2010) or (B, 2000): those cells have nothing to aggregate.
			public static readonly StrictSales[] Data =
			{
				new() { Category = "A", Year = 2000, Amount = 10, At = new DateTime(2000, 1, 1) },
				new() { Category = "B", Year = 2010, Amount = 20, At = new DateTime(2010, 1, 1) },
			};
		}

		static void AssertEmptyCellsReadNull(IReadOnlyList<(string Key, Func<string, object?> Cell)> result)
		{
			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0]
				.Cell("SumY2000")
				.ShouldBe(10);
			result[0]
				.Cell("AtY2000") 
				.ShouldBe(new DateTime(2000, 1, 1));
			result[0]
				.Cell("SumY2010")
				.ShouldBeNull();
			result[0]
				.Cell("AtY2010") 
				.ShouldBeNull();

			result[1].Key.ShouldBe("B");
			result[1]
				.Cell("SumY2000")
				.ShouldBeNull();
			result[1]
				.Cell("AtY2000") 
				.ShouldBeNull();
			result[1]
				.Cell("SumY2010")
				.ShouldBe(20);
		}

		/// <summary>
		/// A cell no row matches reads null, not <c>default(TCell)</c>, through every cell-fed terminal. Pinned for MAX and
		/// for SUM separately: SUM over a non-nullable column takes its own null-handling path.
		/// </summary>
		public enum CellFeed
		{
			Cell,
			Dynamic,
			Templates,
		}

		[Test]
		public void AnEmptyCellOverANonNullableColumnReadsNull([DataSources] string context, [Values] CellFeed feed)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(StrictSales.Data);

			var cells = new[]
			{
				PivotCell<StrictSales, int>.Create(rows => rows.Sum(x => x.Amount), y => "Sum" + Year(y)),
				PivotCell<StrictSales, int>.Create(rows => rows.Max(x => x.At),     y => "At"  + Year(y)),
			};

			var groups = t.GroupBy(x => x.Category);

			// The Cell and Templates arms expand to one tree, so the second would hit the first's cached query, whose
			// SQLite.MS materializer #6028 can leave faulted.
			Query.ClearCaches();

			var result = feed switch
			{
				CellFeed.Cell => groups
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, new[] { 2000, 2010 })
							.Cell(rows => rows.Sum(x => x.Amount), y => "Sum" + Year(y))
							.Cell(rows => rows.Max(x => x.At),     y => "At"  + Year(y))
							.AsDictionary(),
					})
					.ToList()
					.Select(r => (r.Key, (Func<string, object?>)(n => r.Cells[n]))),

				CellFeed.Dynamic => groups
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, new[] { 2000, 2010 })
							.Cell(rows => rows.Sum(x => x.Amount), y => "Sum" + Year(y))
							.Cell(rows => rows.Max(x => x.At),     y => "At"  + Year(y))
							.AsDynamic(),
					})
					.ToList()
					.Select(r => (r.Key, (Func<string, object?>)(n => ((PivotCells)r.Cells)[n]))),

				_ => groups
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, new[] { 2000, 2010 })
							.Cells(cells)
							.AsDictionary(),
					})
					.ToList()
					.Select(r => (r.Key, (Func<string, object?>)(n => r.Cells[n]))),
			};

			var ordered = result
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			AssertEmptyCellsReadNull(ordered);
		}

		/// <summary>A runtime value set can be empty; the row then carries an empty cell set, not a null one.</summary>
		[Test]
		public void AnEmptyValueSetYieldsNoCells([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, Array.Empty<int>())
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result
				.Select(r => r.Key)
				.ShouldBe(new[] { "A", "B" });

			result.ShouldAllBe(r => r.Cells.Store.Count == 0 && r.Cells["Y2000"] == null);

			// One store per row, not one shared.
			result[0].Cells.ShouldNotBeSameAs(result[1].Cells);
		}

		/// <summary>An empty cell reads what its aggregate yields over no rows - for a count that is 0, not null.</summary>
		[Test]
		public void AnEmptyCountCellReadsZero([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(StrictSales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Count(), Year)
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Cells["Y2000"].ShouldBe(1);
			result[0].Cells["Y2010"].ShouldBe(0);
			result[1].Cells["Y2000"].ShouldBe(0);
			result[1].Cells["Y2010"].ShouldBe(1);
		}

		/// <summary>
		/// One statically typed item per pivoted value, in value order. The item's members are the selector's own, so an
		/// empty cell reads null only where the selector lifts the aggregate.
		/// </summary>
		[Test]
		public void SelectsAnItemPerValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(StrictSales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Years = g
						.Pivot(x => x.Year, new[] { 2010, 2000 })
						.Select((year, rows) => new
						{
							Year   = year,
							Lifted = rows.Sum(x => (int?)x.Amount),
							Total  = rows.Sum(x => x.Amount),
							Rows   = rows.Count(),
						}),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0].Years
				.Select(y => (y.Year, y.Lifted, y.Total, y.Rows))
				.ShouldBe(new (int, int?, int, int)[] { (2010, null, 0, 0), (2000, 10, 10, 1) });

			result[1].Key.ShouldBe("B");
			result[1].Years
				.Select(y => (y.Year, y.Lifted, y.Total, y.Rows))
				.ShouldBe(new (int, int?, int, int)[] { (2010, 20, 20, 1), (2000, null, 0, 0) });
		}

		[Test]
		public void ReadsCellsAsDynamicMembers([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDynamic(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			((object)result[0].Cells.Y2000)
				.ShouldBe(10m);
			((object)result[0].Cells.Y2010)
				.ShouldBe(20m);
			((object)result[1].Cells.Y2000)
				.ShouldBe(5m);
			((object)result[1].Cells.Y2010)
				.ShouldBe(15m);

			((PivotCells)result[0].Cells)
				.GetDynamicMemberNames()
				.OrderBy(n => n, StringComparer.Ordinal)
				.ShouldBe(new[] { "Y2000", "Y2010" });

			Action absentCell = () => _ = result[0].Cells.Y2020;

			Assert.That(() => absentCell(), Throws.TypeOf<RuntimeBinderException>());
		}

		static T PassThrough<T>(T value) => value;

		static readonly Func<int, string> _yearName = Year;

		static List<PivotCell<Sales, int>> SumTemplates() => new() { PivotCell<Sales, int>.Create(rows => rows.Sum(x => x.Amount), Year) };

		static int[] PivotYearsArray() => new[] { 2000, 2010 };

		/// <summary>
		/// Each place an argument can come from: name factories as a method group and a captured delegate, cell templates as
		/// an inline array and a method result, values from a method, a composite column, an expression-method member in
		/// the aggregate and <c>Sql.Property</c> as the pivoted column.
		/// </summary>
		[Test]
		public void ReadsEveryArgumentShape([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var factory = _yearName;

			var query = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					MethodGroup = g
						.Pivot(x => x.Year, PivotYearsArray())
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
					CapturedFactory = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), factory)
						.AsDictionary(),
					InlineTemplates = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cells(new[] { PivotCell<Sales, int>.Create(rows => rows.Sum(x => x.Amount), Year) })
						.AsDictionary(),
					MethodTemplates = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cells(SumTemplates())
						.AsDictionary(),
					ExpressionMethod = g
						.Pivot(x => Sql.Property<int>(x, nameof(Sales.Year)), new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.DoubleAmount), Year)
						.AsDictionary(),
				});

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			foreach (var cells in new[] { result[0].MethodGroup, result[0].CapturedFactory, result[0].InlineTemplates, result[0].MethodTemplates })
			{
				cells["Y2000"].ShouldBe(10m);
				cells["Y2010"].ShouldBe(20m);
			}

			result[0].ExpressionMethod["Y2000"].ShouldBe(20m);
			result[0].ExpressionMethod["Y2010"].ShouldBe(40m);
			result[1].ExpressionMethod["Y2010"].ShouldBe(30m);
		}

		// Call-site guards: each throws while the query is built, before any SQL.
		[Test]
		public void RejectsShapesItCannotRead([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			Func<IEnumerable<Sales>, decimal?> aggregate = rows => rows.Sum(x => x.Amount);
			Func<Sales, int>                   column    = x => x.Year;

			var capturedAggregate = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(aggregate)
						.AsDictionary(),
				});

			var capturedColumn = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(column, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount))
						.AsDictionary(),
				});

			var correlatedValues = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, g.Select(x => x.Year))
						.Cell(rows => rows.Sum(x => x.Amount))
						.AsDictionary(),
				});

			var correlatedFactory = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount), y => g.Key + y.ToString(CultureInfo.InvariantCulture))
						.AsDictionary(),
				});

			var noTerminal = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount)),
				});

			var asArgument = t
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = PassThrough(g
						.Pivot(x => x.Year, new[] { 2000 })
						.Cell(rows => rows.Sum(x => x.Amount))
						.AsDictionary()),
				});

			var inWhere = t
				.GroupBy(x => x.Category)
				.Where(g => g
					.Pivot(x => x.Year, new[] { 2000 })
					.Cell(rows => rows.Sum(x => x.Amount))
					.AsDictionary() != null)
				.Select(g => g.Key);

			Action capturedAggregateAct = () => capturedAggregate.ToList();
			Action capturedColumnAct    = () => capturedColumn.ToList();
			Action correlatedValuesAct  = () => correlatedValues.ToList();
			Action correlatedFactoryAct = () => correlatedFactory.ToList();
			Action noTerminalAct        = () => noTerminal.ToList();
			Action asArgumentAct        = () => asArgument.ToList();
			Action inWhereAct           = () => inWhere.ToList();

			Assert.That(() => capturedAggregateAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(ErrorHelper.Pivot.Error_PivotAggregateNotInline));

			Assert.That(() => capturedColumnAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Pivot.Error_PivotLambdaNotInline, "column")));

			Assert.That(() => correlatedValuesAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Pivot.Error_PivotReadsQueryParameter, "value set")));

			Assert.That(() => correlatedFactoryAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Pivot.Error_PivotReadsQueryParameter, "name factory")));

			Assert.That(() => noTerminalAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(ErrorHelper.Pivot.Error_PivotNoTerminal));

			Assert.That(() => asArgumentAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Pivot.Error_PivotTerminalNotAMember, nameof(PivotBuilder<,>.AsDictionary))));

			Assert.That(() => inWhereAct(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Pivot.Error_PivotTerminalNotAMember, nameof(PivotBuilder<,>.AsDictionary))));
		}

		/// <summary>The pivot is translated to SQL as part of a query; in memory every member throws, and a template does not.</summary>
		[Test]
		public void ThrowsWhenInvokedInMemory()
		{
			var group = Sales.Data
				.GroupBy(x => x.Category)
				.First();

			Action pivot = () => group.Pivot(x => x.Year, new[] { 2000 });

			Assert.That(() => pivot(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Error_QueryMarkerInvoked, nameof(LinqExtensions.Pivot))));

			// No instance can be had in memory, since Pivot throws; one is made here only to reach the members.
			var builder = (PivotBuilder<Sales, int>)Activator.CreateInstance(typeof(PivotBuilder<Sales, int>), nonPublic: true)!;

			var members = new (string Name, Action Invoke)[]
			{
				(nameof(builder.Cell),         () => builder.Cell(rows => rows.Count())),
				(nameof(builder.Cells),        () => builder.Cells(Array.Empty<PivotCell<Sales, int>>())),
				(nameof(builder.AsDictionary), () => builder.AsDictionary()),
				(nameof(builder.AsDynamic),    () => builder.AsDynamic()),
				(nameof(builder.Select),       () => builder.Select((y, rows) => y)),
			};

			foreach (var (name, invoke) in members)
			{
				Assert.That(() => invoke(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Error_QueryMarkerInvoked, name)));
			}

			PivotCell<Sales, int>
				.Create(rows => rows.Sum(x => x.Amount))
				.ShouldNotBeNull();
		}

		[Test]
		public void PivotsUnderConcat([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var query = t
				.Where(x => x.Category == "A")
				.GroupBy(x => x.Category)
				.Select(g => new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.Year, new[] { 2000, 2010 })
						.Cell(rows => rows.Sum(x => x.Amount), Year)
						.AsDictionary(),
				})
				.Concat(t
					.Where(x => x.Category == "B")
					.GroupBy(x => x.Category)
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, new[] { 2000, 2010 })
							.Cell(rows => rows.Max(x => x.Amount), Year)
							.AsDictionary(),
					}));

			var result = query
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Cells["Y2000"].ShouldBe(10m);
			result[0].Cells["Y2010"].ShouldBe(20m);
			result[1].Cells["Y2000"].ShouldBe(5m);
			result[1].Cells["Y2010"].ShouldBe(15m);
		}

		[Test]
		public void PivotsInAnEagerLoadedCollection([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var childIds = ChildIdsOfPivotedParents();

			var query =
				from p in db.Parent
				where p.ParentID == 2 || p.ParentID == 3
				select new
				{
					p.ParentID,
					Cells = db.Child
						.Where(c => c.ParentID == p.ParentID)
						.GroupBy(c => c.ParentID)
						.Select(g => g
							.Pivot(x => x.ChildID, childIds)
							.Cell(rows => rows.Sum(x => x.ChildID))
							.AsDictionary())
						.ToList(),
				};

			var result = query
				.ToList()
				.OrderBy(r => r.ParentID)
				.ToList();

			result
				.Select(r => r.ParentID)
				.ShouldBe(_pivotedParents);

			foreach (var row in result)
			{
				row.Cells.Count.ShouldBe(1);

				foreach (var childId in childIds)
				{
					row.Cells[0][childId.ToString(CultureInfo.InvariantCulture)]
						.ShouldBe(ExpectedChildSum(row.ParentID, childId));
				}
			}
		}

		/// <summary>
		/// The value set and the cell names are part of the expanded tree the cache compares: an unchanged input hits, and
		/// any change - a wider set, a list mutated in place, a factory reading a changed capture, the same names over
		/// other values - is a different query.
		/// </summary>
		[Test, QueryCacheTest]
		public void QueryIdentityFollowsTheInputs([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var years  = new List<int> { 2000 };
			var prefix = "Y";

			IQueryable<KeyValuePair<string, PivotCells>> Build() => t
				.GroupBy(x => x.Category)
				.Select(g => new KeyValuePair<string, PivotCells>(
					g.Key,
					g
						.Pivot(x => x.Year, years)
						.Cell(rows => rows.Sum(x => x.Amount), y => prefix + y.ToString(CultureInfo.InvariantCulture))
						.AsDictionary()));

			PivotCells CellsOfA() => Build()
				.ToList()
				.Single(r => r.Key == "A")
				.Value;

			var probe = Build();

			probe.ClearCache();

			var start = probe.GetCacheMissCount();

			CellsOfA()["Y2000"]
				.ShouldBe(10m);
			CellsOfA()["Y2000"]
				.ShouldBe(10m);
			(probe.GetCacheMissCount() - start)
				.ShouldBe(1, "the same inputs reuse the cached query");

			years.Add(2010);
			CellsOfA()["Y2010"]
				.ShouldBe(20m);
			(probe.GetCacheMissCount() - start)
				.ShouldBe(2, "a wider value set is a different query");

			years[1] = 2020;
			CellsOfA()
				.Store
				.ContainsKey("Y2010")
				.ShouldBeFalse();
			(probe.GetCacheMissCount() - start)
				.ShouldBe(3, "a list mutated in place is a different query");

			prefix = "Z";
			CellsOfA()["Z2000"]
				.ShouldBe(10m);
			(probe.GetCacheMissCount() - start)
				.ShouldBe(4, "a name factory reading a changed capture is a different query");
		}

		/// <summary>Names that do not encode the value cannot keep two value sets apart; the values in the tree do.</summary>
		[Test, QueryCacheTest]
		public void SameNamesOverDifferentValuesReadTheirOwnValues([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			IQueryable<KeyValuePair<string, PivotCells>> Build(int current, int prior) => t
				.GroupBy(x => x.Category)
				.Select(g => new KeyValuePair<string, PivotCells>(
					g.Key,
					g
						.Pivot(x => x.Year, new[] { current, prior })
						.Cell(rows => rows.Sum(x => x.Amount), y => y == current ? "Current" : "Prior")
						.AsDictionary()));

			var probe = Build(2010, 2000);

			probe.ClearCache();

			var start = probe.GetCacheMissCount();

			var first = Build(2010, 2000)
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			var second = Build(2000, 2010)
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			(probe.GetCacheMissCount() - start)
				.ShouldBe(2);

			first[0].Value["Current"].ShouldBe(20m);
			first[0].Value["Prior"]  .ShouldBe(10m);
			first[1].Value["Current"].ShouldBe(15m);
			first[1].Value["Prior"]  .ShouldBe(5m);

			second[0].Value["Current"].ShouldBe(10m);
			second[0].Value["Prior"]  .ShouldBe(20m);
			second[1].Value["Current"].ShouldBe(5m);
			second[1].Value["Prior"]  .ShouldBe(15m);
		}

		static object? CellVOfA(IQueryable<KeyValuePair<string, PivotCells>> query)
			=> query
				.ToList()
				.Single(r => r.Key == "A")
				.Value["V"];

		/// <summary>
		/// A compiled query reads the value set from its arguments when it is built, and keys its builds on the argument: a
		/// different set is a different build, a repeated one reuses its build. The name does not encode the value, so a
		/// replayed build would read the wrong cell.
		/// </summary>
		// Compiled-query build identity is provider-agnostic.
		[Test]
		public void CompiledPivotKeysOnItsValueSet([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var byArray = CompiledQuery.Compile((IDataContext dc, int[] years) =>
				dc.GetTable<Sales>()
					.GroupBy(x => x.Category)
					.Select(g => new KeyValuePair<string, PivotCells>(
						g.Key,
						g
							.Pivot(x => x.Year, years)
							.Cell(rows => rows.Sum(x => x.Amount), _ => "V")
							.AsDictionary())));

			var array1 = byArray(db, [2000]);
			var array2 = byArray(db, [2010]);
			var array3 = byArray(db, [2000]);

			CellVOfA(array1)
				.ShouldBe(10m);
			CellVOfA(array2)
				.ShouldBe(20m);
			CellVOfA(array3)
				.ShouldBe(10m);

			array2
				.GetCompiledQueryInfo()
				.ShouldNotBeSameAs(array1.GetCompiledQueryInfo());

			array3
				.GetCompiledQueryInfo()
				.ShouldBeSameAs(array1.GetCompiledQueryInfo());

			var byList = CompiledQuery.Compile((IDataContext dc, List<int> years) =>
				dc.GetTable<Sales>()
					.GroupBy(x => x.Category)
					.Select(g => new KeyValuePair<string, PivotCells>(
						g.Key,
						g
							.Pivot(x => x.Year, years)
							.Cell(rows => rows.Sum(x => x.Amount), _ => "V")
							.AsDictionary())));

			var list1 = byList(db, new List<int> { 2000 });
			var list2 = byList(db, new List<int> { 2010 });
			var list3 = byList(db, new List<int> { 2000 });
			var empty = byList(db, new List<int>());

			CellVOfA(list1)
				.ShouldBe(10m);
			CellVOfA(list2)
				.ShouldBe(20m);
			CellVOfA(list3)
				.ShouldBe(10m);

			empty
				.ToList()
				.ShouldAllBe(r => r.Value.Store.Count == 0);

			list2
				.GetCompiledQueryInfo()
				.ShouldNotBeSameAs(list1.GetCompiledQueryInfo());

			list3
				.GetCompiledQueryInfo()
				.ShouldBeSameAs(list1.GetCompiledQueryInfo());

			Action nullSet = () => byArray(db, null!)
				.ToList();

			Assert.That(() => nullSet(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(Message(ErrorHelper.Pivot.Error_PivotArgumentNull, "value set")));
		}

		/// <summary>
		/// A value set the compiled lambda holds itself - a literal, or a capture - is read when the query is first built,
		/// like any other value the compiled query folds into its shape.
		/// </summary>
		// Compiled-query build identity is provider-agnostic.
		[Test]
		public void CompiledPivotOverAValueSetItHolds([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var byLiteral = CompiledQuery.Compile((IDataContext dc) =>
				dc.GetTable<Sales>()
					.GroupBy(x => x.Category)
					.Select(g => new KeyValuePair<string, PivotCells>(
						g.Key,
						g
							.Pivot(x => x.Year, new[] { 2010 })
							.Cell(rows => rows.Sum(x => x.Amount), _ => "V")
							.AsDictionary())));

			var literal1 = byLiteral(db);
			var literal2 = byLiteral(db);

			CellVOfA(literal1)
				.ShouldBe(20m);
			CellVOfA(literal2)
				.ShouldBe(20m);

			literal2
				.GetCompiledQueryInfo()
				.ShouldBeSameAs(literal1.GetCompiledQueryInfo());

			var captured = new List<int> { 2000 };

			var byCapture = CompiledQuery.Compile((IDataContext dc) =>
				dc.GetTable<Sales>()
					.GroupBy(x => x.Category)
					.Select(g => new KeyValuePair<string, PivotCells>(
						g.Key,
						g
							.Pivot(x => x.Year, captured)
							.Cell(rows => rows.Sum(x => x.Amount), _ => "V")
							.AsDictionary())));

			var capture1 = byCapture(db);

			CellVOfA(capture1)
				.ShouldBe(10m);

			// The capture is not part of the compiled query's key, so the first build is reused: #6045. A fix flips this arm.
			captured[0] = 2010;

			var capture2 = byCapture(db);

			CellVOfA(capture2)
				.ShouldBe(10m);

			capture2
				.GetCompiledQueryInfo()
				.ShouldBeSameAs(capture1.GetCompiledQueryInfo());

			// An argument that falls back to a capture is keyed on the argument only.
			var mixed = CompiledQuery.Compile((IDataContext dc, int[]? years) =>
				dc.GetTable<Sales>()
					.GroupBy(x => x.Category)
					.Select(g => new KeyValuePair<string, PivotCells>(
						g.Key,
						g
							.Pivot(x => x.Year, (IEnumerable<int>?)years ?? captured)
							.Cell(rows => rows.Sum(x => x.Amount), _ => "V")
							.AsDictionary())));

			var mixed1 = mixed(db, [2000]);
			var mixed2 = mixed(db, null);

			CellVOfA(mixed1)
				.ShouldBe(10m);
			CellVOfA(mixed2)
				.ShouldBe(20m);

			mixed2
				.GetCompiledQueryInfo()
				.ShouldNotBeSameAs(mixed1.GetCompiledQueryInfo());
		}

		static string[] ReadCategories(IQueryable<string> query)
			=> query
				.Distinct()
				.ToArray();

		static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int[]> _pivotYears = new();

		static int[] PivotYears(string context) => _pivotYears[context];

		sealed class YearSource
		{
			public int[] Years = null!;

			public int[] GetYears() => Years;
		}

		sealed class GuardedName
		{
			public string? Prefix;
			public int     Reads;

			public string Read()
			{
				Reads++;
				return Prefix!;
			}
		}

		static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _currentYears = new();

		static int CurrentYear(string context) => _currentYears[context];

		/// <summary>
		/// Written in a sub-query, every input is read again on each execution: a captured array, a static and an instance
		/// method, a list mutated in place, a runtime cell set and a name factory's capture.
		/// </summary>
		[Test]
		public void InASubQueryEachExecutionRereadsItsInputs([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			// A captured array, reassigned.
			var years = new[] { 2010 };

			IQueryable<string> ByCapturedArray() =>
				from c in t
				where t
					.GroupBy(x => x.Category)
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, years)
							.Cell(rows => rows.Sum(x => x.Amount), _ => "V")
							.AsDictionary(),
					})
					.Any(r => r.Key == c.Category && Sql.Property<decimal?>(r.Cells, "V") > 16)
				select c.Category;

			ReadCategories(ByCapturedArray())
				.ShouldBe(new[] { "A" });
			years = new[] { 2000 };
			ReadCategories(ByCapturedArray())
				.ShouldBeEmpty();

			// A static method.
			_pivotYears[context] = new[] { 2010 };

			IQueryable<string> ByStaticMethod() =>
				from c in t
				where t
					.GroupBy(x => x.Category)
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, PivotYears(context))
							.Cell(rows => rows.Sum(x => x.Amount), _ => "V")
							.AsDictionary(),
					})
					.Any(r => r.Key == c.Category && Sql.Property<decimal?>(r.Cells, "V") > 16)
				select c.Category;

			ReadCategories(ByStaticMethod())
				.ShouldBe(new[] { "A" });
			_pivotYears[context] = new[] { 2000 };
			ReadCategories(ByStaticMethod())
				.ShouldBeEmpty();

			// An instance method.
			var source = new YearSource { Years = new[] { 2010 } };

			IQueryable<string> ByInstanceMethod() =>
				from c in t
				where t
					.GroupBy(x => x.Category)
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, source.GetYears())
							.Cell(rows => rows.Sum(x => x.Amount), _ => "V")
							.AsDictionary(),
					})
					.Any(r => r.Key == c.Category && Sql.Property<decimal?>(r.Cells, "V") > 16)
				select c.Category;

			ReadCategories(ByInstanceMethod())
				.ShouldBe(new[] { "A" });
			source.Years = new[] { 2000 };
			ReadCategories(ByInstanceMethod())
				.ShouldBeEmpty();

			// A list mutated in place.
			var list = new List<int> { 2010 };

			IQueryable<string> ByMutatedList() =>
				from c in t
				where t
					.GroupBy(x => x.Category)
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, list)
							.Cell(rows => rows.Sum(x => x.Amount), _ => "V")
							.AsDictionary(),
					})
					.Any(r => r.Key == c.Category && Sql.Property<decimal?>(r.Cells, "V") > 16)
				select c.Category;

			ReadCategories(ByMutatedList())
				.ShouldBe(new[] { "A" });
			list[0] = 2000;
			ReadCategories(ByMutatedList())
				.ShouldBeEmpty();

			// A runtime cell set whose factory reads a value.
			_currentYears[context] = 2010;

			IQueryable<string> ByCellSet()
			{
				var templates = new[]
				{
					PivotCell<CategorySales, int>.Create(rows => rows.Sum(x => x.Amount), y => y == CurrentYear(context) ? "V" : "W"),
				};

				return
					from c in t
					where t
						.GroupBy(x => x.Category)
						.Select(g => new
						{
							g.Key,
							Cells = g
								.Pivot(x => x.Year, new[] { 2000, 2010 })
								.Cells(templates)
								.AsDictionary(),
						})
						.Any(r => r.Key == c.Category && Sql.Property<decimal?>(r.Cells, "V") > 16)
					select c.Category;
			}

			ReadCategories(ByCellSet())
				.ShouldBe(new[] { "A" });
			_currentYears[context] = 2000;
			ReadCategories(ByCellSet())
				.ShouldBeEmpty();

			// A name factory's capture.
			var current = 2010;

			IQueryable<string> ByFactoryCapture() =>
				from c in t
				where t
					.GroupBy(x => x.Category)
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, new[] { 2000, 2010 })
							.Cell(rows => rows.Sum(x => x.Amount), y => y == current ? "V" : "W")
							.AsDictionary(),
					})
					.Any(r => r.Key == c.Category && Sql.Property<decimal?>(r.Cells, "V") > 16)
				select c.Category;

			ReadCategories(ByFactoryCapture())
				.ShouldBe(new[] { "A" });
			current = 2000;
			ReadCategories(ByFactoryCapture())
				.ShouldBeEmpty();
		}

		/// <summary>
		/// The name factory runs only on the pivoted values, as a direct call would, so a read behind its guard is never
		/// reached for a value the guard rules out.
		/// </summary>
		[Test, QueryCacheTest]
		public void NameFactoryGuardIsNotEvaluatedEagerly([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var name = new GuardedName();

			IQueryable<string> Categories() =>
				from c in t
				where t
					.GroupBy(x => x.Category)
					.Select(g => new
					{
						g.Key,
						Cells = g
							.Pivot(x => x.Year, new[] { 2010 })
							.Cell(rows => rows.Sum(x => x.Amount), y => y > 3000 ? name.Read() : "V")
							.AsDictionary(),
					})
					.Any(r => r.Key == c.Category && Sql.Property<decimal?>(r.Cells, "V") > 16)
				select c.Category;

			var probe = Categories();

			probe.ClearCache();

			var start = probe.GetCacheMissCount();

			ReadCategories(Categories())
				.ShouldBe(new[] { "A" });
			ReadCategories(Categories())
				.ShouldBe(new[] { "A" });

			name.Prefix = "W";
			ReadCategories(Categories())
				.ShouldBe(new[] { "A" });

			name.Reads.ShouldBe(0);
			(probe.GetCacheMissCount() - start)
				.ShouldBe(1, "a capture the factory never reads does not change the query");
		}

		[Table]
		sealed class ModTemplate
		{
			[PrimaryKey(ProviderName.Ydb, 1)]
			[Column] public int      Id         { get; set; }
			[PrimaryKey(ProviderName.Ydb, 2)]
			[Column] public int      TheKey     { get; set; }
			[Column] public int?     PosRubId   { get; set; }
			[Column] public int?     RubId      { get; set; }
			[Column] public DateTime ModifiedAt { get; set; }

			public static readonly ModTemplate[] Data =
			{
				new() { Id = 1, TheKey = 10, PosRubId = 100, RubId = 1000, ModifiedAt = new DateTime(2020, 1, 1) },
				new() { Id = 1, TheKey = 20, PosRubId = 100, RubId = 1001, ModifiedAt = new DateTime(2020, 1, 2) },
				new() { Id = 1, TheKey = 30, PosRubId = 100, RubId = null, ModifiedAt = new DateTime(2020, 1, 3) },
				// An activity the configuration table does not carry: the inner join drops the row, and with it the
				// ModifiedAt that would otherwise win its group.
				new() { Id = 1, TheKey = 99, PosRubId = 100, RubId = 1002, ModifiedAt = new DateTime(2029, 1, 1) },
				new() { Id = 2, TheKey = 10, PosRubId = 200, RubId = 1003, ModifiedAt = new DateTime(2021, 1, 1) },
				new() { Id = 2, TheKey = 30, PosRubId = 200, RubId = 1004, ModifiedAt = new DateTime(2021, 2, 1) },
			};
		}

		[Table]
		sealed class Activity
		{
			[PrimaryKey(Configuration = ProviderName.Ydb)]
			[Column] public int Id { get; set; }

			public static readonly Activity[] Data = { new() { Id = 10 }, new() { Id = 20 }, new() { Id = 30 } };
		}

		[Table]
		sealed class CoaMask
		{
			[PrimaryKey(Configuration = ProviderName.Ydb)]
			[Column] public int     Id   { get; set; }
			[Column] public string? Name { get; set; }

			public static readonly CoaMask[] Data = { new() { Id = 1, Name = "Mask one" }, new() { Id = 2, Name = "Mask two" } };
		}

		[Table]
		sealed class AtiRub
		{
			[PrimaryKey(Configuration = ProviderName.Ydb)]
			[Column] public int     Id     { get; set; }
			[Column] public string? LibRub { get; set; }

			public static readonly AtiRub[] Data = { new() { Id = 100, LibRub = "POS one" }, new() { Id = 200, LibRub = "POS two" } };
		}

		[Table]
		sealed class IasRub
		{
			[PrimaryKey(Configuration = ProviderName.Ydb)]
			[Column] public int     Id     { get; set; }
			[Column] public string? IdeRub { get; set; }
			[Column] public string? LibRub { get; set; }

			public static readonly IasRub[] Data =
			{
				new() { Id = 1000, IdeRub = "IDE-A", LibRub = "LIB-A" },
				new() { Id = 1001, IdeRub = "IDE-B", LibRub = "LIB-B" },
				new() { Id = 1002, IdeRub = "IDE-X", LibRub = "LIB-X" },
				new() { Id = 1003, IdeRub = "IDE-C", LibRub = "LIB-C" },
				new() { Id = 1004, IdeRub = "IDE-D", LibRub = "LIB-D" },
			};
		}

		sealed class ListDto
		{
			public int        Id         { get; set; }
			public string?    CoaName    { get; set; }
			public string?    PosLibRub  { get; set; }
			public DateTime?  ModifiedAt { get; set; }
			public PivotCells Cells      { get; set; } = null!;
		}

		static string Ide(int activityId) => "IDE_" + activityId.ToString(CultureInfo.InvariantCulture);
		static string Lib(int activityId) => "LIB_" + activityId.ToString(CultureInfo.InvariantCulture);

		/// <summary>
		/// The acceptance case from
		/// <a href="https://github.com/linq2db/linq2db/pull/5708#issuecomment-4970892781">PR #5708</a>: a joined and grouped
		/// source, two cells crossed with a runtime activity list, and static aggregates beside them.
		/// </summary>
		static IQueryable<ListDto> ProductionShape(
			IQueryable<ModTemplate> mods,
			IQueryable<Activity>    acts,
			IQueryable<CoaMask>     masks,
			IQueryable<AtiRub>      atiRubs,
			IQueryable<IasRub>      iasRubs,
			IEnumerable<int>        ids)
		{
			// The grouped row stays an anonymous projection, as it is in the original query.
			return
				from e in mods
				join act in acts  on e.TheKey equals act.Id
				join cm  in masks on e.Id     equals cm.Id
				from pos in atiRubs.LeftJoin(r => r.Id == e.PosRubId)
				from rub in iasRubs.LeftJoin(r => r.Id == e.RubId)
				group new { e, cm, pos, rub } by e.Id into g
				select new ListDto
				{
					Id         = g.Key,
					CoaName    = g.Max(x => x.cm.Name),
					PosLibRub  = g.Max(x => x.pos.LibRub),
					ModifiedAt = g.Max(x => x.e.ModifiedAt),
					Cells      = g
						.Pivot(x => x.e.TheKey, ids)
						.Cell(rows => rows.Max(x => x.rub.IdeRub), Ide)
						.Cell(rows => rows.Max(x => x.rub.LibRub), Lib)
						.AsDictionary(),
				};
		}

		[Test]
		public void PivotsProductionShapeIntoUserType([DataSources] string context)
		{
			using var db      = GetDataContext(context);
			using var mods    = db.CreateLocalTable(ModTemplate.Data);
			using var acts    = db.CreateLocalTable(Activity.Data);
			using var masks   = db.CreateLocalTable(CoaMask.Data);
			using var atiRubs = db.CreateLocalTable(AtiRub.Data);
			using var iasRubs = db.CreateLocalTable(IasRub.Data);

			var activityIds = acts
				.Select(a => a.Id)
				.OrderBy(id => id)
				.ToList();

			IQueryable<ListDto> Build(IEnumerable<int> ids) => ProductionShape(mods, acts, masks, atiRubs, iasRubs, ids);

			Build(activityIds)
				.ClearCache();

			var rows = Build(activityIds)
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			rows.Count.ShouldBe(2);

			var first = rows[0];

			first.Id        .ShouldBe(1);
			first.CoaName   .ShouldBe("Mask one");
			first.PosLibRub .ShouldBe("POS one");
			first.ModifiedAt.ShouldBe(new DateTime(2020, 1, 3));

			first.Cells.Store.Keys
				.OrderBy(k => k, StringComparer.Ordinal)
				.ShouldBe(new[] { "IDE_10", "IDE_20", "IDE_30", "LIB_10", "LIB_20", "LIB_30" });

			first.Cells["IDE_10"].ShouldBe("IDE-A");
			first.Cells["LIB_10"].ShouldBe("LIB-A");
			first.Cells["IDE_20"].ShouldBe("IDE-B");
			first.Cells["LIB_20"].ShouldBe("LIB-B");
			first.Cells["IDE_30"].ShouldBeNull();
			first.Cells["LIB_30"].ShouldBeNull();

			var second = rows[1];

			second.Id        .ShouldBe(2);
			second.CoaName   .ShouldBe("Mask two");
			second.PosLibRub .ShouldBe("POS two");
			second.ModifiedAt.ShouldBe(new DateTime(2021, 2, 1));

			second.Cells["IDE_10"].ShouldBe("IDE-C");
			second.Cells["IDE_20"].ShouldBeNull();
			second.Cells["LIB_30"].ShouldBe("LIB-D");

			// Still an IQueryable afterwards: a filter over a cell composes.
			var filtered = Build(activityIds)
				.Where(r => r.Id == 1 && Sql.Property<string>(r.Cells, "IDE_20") == "IDE-B")
				.ToList();

			filtered.Count.ShouldBe(1);
			filtered[0].Cells["IDE_20"].ShouldBe("IDE-B");

			var wider = activityIds
				.Concat(new[] { 40 })
				.ToList();

			var widened = Build(wider)
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			widened[0].Cells.Store.Count.ShouldBe(8);
			widened[0].Cells["IDE_40"]  .ShouldBeNull();
			widened[0].Cells["IDE_10"]  .ShouldBe("IDE-A");
		}

		// Query-cache identity is provider-agnostic.
		[Test, QueryCacheTest]
		[ActiveIssue(6028, Configurations = [ProviderName.SQLiteMS], ErrorType = typeof(LinqToDB.Common.LinqToDBConvertException), ErrorMessage = "value failed")]
		public void PivotsProductionShapeQueryCache([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db      = GetDataContext(context);
			using var mods    = db.CreateLocalTable(ModTemplate.Data);
			using var acts    = db.CreateLocalTable(Activity.Data);
			using var masks   = db.CreateLocalTable(CoaMask.Data);
			using var atiRubs = db.CreateLocalTable(AtiRub.Data);
			using var iasRubs = db.CreateLocalTable(IasRub.Data);

			var activityIds = acts
				.Select(a => a.Id)
				.OrderBy(id => id)
				.ToList();

			IQueryable<ListDto> Build(IEnumerable<int> ids) => ProductionShape(mods, acts, masks, atiRubs, iasRubs, ids);

			var probe = Build(activityIds);

			probe.ClearCache();

			var start = probe.GetCacheMissCount();

			_ = Build(activityIds)
				.ToList();
			(probe.GetCacheMissCount() - start)
				.ShouldBe(1);

			_ = Build(activityIds)
				.ToList();
			(probe.GetCacheMissCount() - start)
				.ShouldBe(1, "the same value set reuses the cached query");

			var wider = activityIds
				.Concat(new[] { 40 })
				.ToList();

			_ = Build(wider)
				.ToList();
			(probe.GetCacheMissCount() - start)
				.ShouldBe(2, "a wider value set is a different query");
		}

		/// <summary>The same production shape with no result type declared at all.</summary>
		[Test]
		public void PivotsProductionShapeWithoutAResultType([DataSources] string context)
		{
			using var db      = GetDataContext(context);
			using var mods    = db.CreateLocalTable(ModTemplate.Data);
			using var acts    = db.CreateLocalTable(Activity.Data);
			using var masks   = db.CreateLocalTable(CoaMask.Data);
			using var atiRubs = db.CreateLocalTable(AtiRub.Data);
			using var iasRubs = db.CreateLocalTable(IasRub.Data);

			var activityIds = acts
				.Select(a => a.Id)
				.OrderBy(id => id)
				.ToList();

			var query =
				from e in mods
				join act in acts  on e.TheKey equals act.Id
				join cm  in masks on e.Id     equals cm.Id
				from pos in atiRubs.LeftJoin(r => r.Id == e.PosRubId)
				from rub in iasRubs.LeftJoin(r => r.Id == e.RubId)
				group new { e, cm, pos, rub } by e.Id into g
				select new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.e.TheKey, activityIds)
						.Cell(rows => rows.Max(x => x.rub.IdeRub), Ide)
						.Cell(rows => rows.Max(x => x.rub.LibRub), Lib)
						.AsDictionary(),
				};

			var rows = query
				.ToList()
				.OrderBy(r => r.Key)
				.ToList();

			rows.Count.ShouldBe(2);

			rows[0].Key.ShouldBe(1);
			rows[0].Cells
				.Get<string>("IDE_10")
				.ShouldBe("IDE-A");
			rows[0].Cells["LIB_20"]            .ShouldBe("LIB-B");
			rows[0].Cells["IDE_30"]            .ShouldBeNull();

			rows[1].Key.ShouldBe(2);
			rows[1].Cells
				.Get<string>("IDE_10")
				.ShouldBe("IDE-C");
			rows[1].Cells["LIB_30"]            .ShouldBe("LIB-D");
			rows[1].Cells["IDE_20"]            .ShouldBeNull();

			// A name that was never generated reads as absent rather than throwing.
			rows[0].Cells["IDE_99"]            .ShouldBeNull();
			rows[0].Cells
				.Get<string>("IDE_99")
				.ShouldBeNull();
		}

		/// <summary>
		/// One conditional aggregate per cell, and a filter over a cell becomes a <c>HAVING</c> over that same aggregate
		/// rather than falling back to the client.
		/// </summary>
		// Asserts SQL text.
		[Test]
		public void ProductionShapeLowersToConditionalAggregates([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db      = GetDataConnection(context);
			using var mods    = db.CreateLocalTable(ModTemplate.Data);
			using var acts    = db.CreateLocalTable(Activity.Data);
			using var masks   = db.CreateLocalTable(CoaMask.Data);
			using var atiRubs = db.CreateLocalTable(AtiRub.Data);
			using var iasRubs = db.CreateLocalTable(IasRub.Data);

			var activityIds = acts
				.Select(a => a.Id)
				.OrderBy(id => id)
				.ToList();

			var query =
				from e in mods
				join act in acts  on e.TheKey equals act.Id
				join cm  in masks on e.Id     equals cm.Id
				from pos in atiRubs.LeftJoin(r => r.Id == e.PosRubId)
				from rub in iasRubs.LeftJoin(r => r.Id == e.RubId)
				group new { e, cm, pos, rub } by e.Id into g
				select new
				{
					g.Key,
					Cells = g
						.Pivot(x => x.e.TheKey, activityIds)
						.Cell(rows => rows.Max(x => x.rub.IdeRub), Ide)
						.Cell(rows => rows.Max(x => x.rub.LibRub), Lib)
						.AsDictionary(),
				};

			_ = query
				.Where(r => Sql.Property<string>(r.Cells, "IDE_20") == "IDE-B")
				.ToList();

			var sql = db.LastQuery!;

			// One conditional aggregate per cell, plus the one the filter repeats in HAVING.
			Occurrences(sql, "WHEN")
				.ShouldBe(activityIds.Count * 2 + 1);
			sql.ShouldContain("HAVING");
		}

		static int Occurrences(string text, string token)
		{
			var count = 0;

			for (var i = text.IndexOf(token, StringComparison.Ordinal); i >= 0; i = text.IndexOf(token, i + token.Length, StringComparison.Ordinal))
				count++;

			return count;
		}
	}
}
