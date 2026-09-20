using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class PivotDynamicTests : TestBase
	{
		[Table]
		sealed class Sales
		{
			[Column] public string   Category { get; set; } = null!;
			[Column] public int      Year     { get; set; }
			[Column] public decimal? Amount   { get; set; }
			[Column] public string?  Note     { get; set; }

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
			public string Category { get; set; } = null!;
			public decimal? Total  { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object> Cells { get; set; } = null!;
		}

		static string Year(int y) => "Y" + y.ToString(CultureInfo.InvariantCulture);

		[Test]
		public void PivotsRuntimeValuesIntoPivotRow([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			// Runtime, and built by a query so it cannot fold to a constant.
			var years = t.Select(x => x.Year).Distinct().ToList();

			var result = t
				.Pivot(x => x.Category, x => x.Year, years,
					PivotCell<Sales, int>.Sum(x => x.Amount, Year))
				.ToList()
				.OrderBy(r => r.Key)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0]["Y2000"].ShouldBe(10m);
			result[0]["Y2010"].ShouldBe(20m);

			result[1].Key.ShouldBe("B");
			result[1]["Y2000"].ShouldBe(5m);
			result[1]["Y2010"].ShouldBe(15m);
		}

		/// <summary>
		/// Several cell templates crossed with the runtime values, alongside static aggregated members - the
		/// shape a real report needs, and the one the static projection API cannot express.
		/// </summary>
		[Test]
		public void PivotsMultipleCellsIntoUserType([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			var years = new[] { 2000, 2010 };

			var result = t
				.Pivot(x => x.Category, x => x.Year, years,
					g => new SalesDto { Category = g.Key, Total = g.Sum(x => x.Amount) },
					PivotCell<Sales, int>.Sum(x => x.Amount, y => "AMT_" + Year(y)),
					PivotCell<Sales, int>.Max(x => x.Note,   y => "NOTE_" + Year(y)))
				.ToList()
				.OrderBy(r => r.Category)
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

		[Test]
		public void MultipleCellsWithoutNameFactoryThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			// Two templates would otherwise generate two columns called "2000".
			System.Action act = () => t
				.Pivot(x => x.Category, x => x.Year, new[] { 2000 },
					PivotCell<Sales, int>.Sum(x => x.Amount),
					PivotCell<Sales, int>.Max(x => x.Note))
				.ToList();

			act.ShouldThrow<System.ArgumentException>();
		}

		/// <summary>A cell named after <see cref="PivotRow{TKey}"/>'s own members would be shadowed by them.</summary>
		[Test]
		public void CellNamedAfterARowMemberThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Sales.Data);

			Action act = () => t
				.Pivot(x => x.Category, x => x.Year, new[] { 2000 },
					PivotCell<Sales, int>.Sum(x => x.Amount, _ => nameof(PivotRow<>.Key)))
				.ToList();

			act.ShouldThrow<ArgumentException>();
		}

		#region Compile-time-known value sets

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

			// A group whose distinct count differs from its row count, so a plain COUNT cannot pass by accident.
			public static readonly CategorySales[] DuplicateData =
			{
				new() { Category = "A", Year = 2000, Amount = 10m },
				new() { Category = "A", Year = 2000, Amount = 10m },
				new() { Category = "A", Year = 2000, Amount = 30m },
				new() { Category = "B", Year = 2000, Amount = 5m  },
			};
		}

		[Test]
		public void PivotsConstantValueSet([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var result = t
				.Pivot(x => x.Category, x => x.Year, new[] { 2000, 2010 },
					PivotCell<CategorySales, int>.Sum(x => x.Amount, Year))
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0]["Y2000"].ShouldBe(10m);
			result[0]["Y2010"].ShouldBe(20m);

			result[1].Key.ShouldBe("B");
			result[1]["Y2000"].ShouldBe(5m);
			result[1]["Y2010"].ShouldBe(15m);
		}

		[Test]
		public void PivotsMultipleAggregatesOfOneValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			var result = t
				.Pivot(x => x.Category, x => x.Year, new[] { 2000 },
					PivotCell<CategorySales, int>.Sum(x => x.Amount, y => "Sum" + Year(y)),
					PivotCell<CategorySales, int>.Count(y => "Cnt" + Year(y)))
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0]["SumY2000"].ShouldBe(10m);
			result[0]["CntY2000"].ShouldBe(1);

			result[1]["SumY2000"].ShouldBe(5m);
			result[1]["CntY2000"].ShouldBe(1);
		}

		[Test]
		public void PivotsAvgMinMaxCells([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.MultiRowData);

			var result = t
				.Pivot(x => x.Category, x => x.Year, new[] { 2000 },
					PivotCell<CategorySales, int>.Avg(x => x.Amount, y => "Avg" + Year(y)),
					PivotCell<CategorySales, int>.Min(x => x.Amount, y => "Min" + Year(y)),
					PivotCell<CategorySales, int>.Max(x => x.Amount, y => "Max" + Year(y)))
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0]["AvgY2000"].ShouldBe(20m);
			result[0]["MinY2000"].ShouldBe(10m);
			result[0]["MaxY2000"].ShouldBe(30m);

			result[1].Key.ShouldBe("B");
			result[1]["AvgY2000"].ShouldBe(5m);
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
		public void PivotsOnACompositeKey([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(RegionSales.Data);

			var result = t
				.Pivot(x => new { x.Category, x.Region }, x => x.Year, new[] { 2000, 2010 },
					PivotCell<RegionSales, int>.Sum(x => x.Amount, Year))
				.ToList()
				.OrderBy(r => r.Key.Category, StringComparer.Ordinal)
				.ThenBy(r => r.Key.Region, StringComparer.Ordinal)
				.ToList();

			// groups: (A,EU) Y2000=10 Y2010=20; (A,US) Y2000=3; (B,EU) Y2000=5
			result.Count.ShouldBe(3);

			result[0].Key.Category.ShouldBe("A");
			result[0].Key.Region  .ShouldBe("EU");
			result[0]["Y2000"]    .ShouldBe(10m);
			result[0]["Y2010"]    .ShouldBe(20m);

			result[1].Key.Region.ShouldBe("US");
			result[1]["Y2000"]  .ShouldBe(3m);

			result[2].Key.Category.ShouldBe("B");
			result[2]["Y2000"]    .ShouldBe(5m);
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
		public void PivotsOnACompositeValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(QuarterAmounts.Data);

			// The FOR side is a pair, so the cell predicate is an AND of the two member comparisons. The pair is an
			// anonymous type, so the cell has to come from the factory - TFor cannot be written down.
			var result = t
				.Pivot(
					x => x.Category,
					x => new { x.Year, x.Quarter },
					new[] { new { Year = 2000, Quarter = 1 }, new { Year = 2000, Quarter = 2 } },
					c => c.Sum(x => x.Amount, v => Year(v.Year) + "Q" + v.Quarter.ToString(CultureInfo.InvariantCulture)))
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			// A: Q1=10, Q2=20; B: Q1=5, Q2=null
			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0]["Y2000Q1"].ShouldBe(10m);
			result[0]["Y2000Q2"].ShouldBe(20m);

			result[1].Key.ShouldBe("B");
			result[1]["Y2000Q1"].ShouldBe(5m);
			result[1]["Y2000Q2"].ShouldBeNull();
		}

		[Test]
		public void ComposesAfterAConstantPivot([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.Data);

			// Where on a generated column plus a projection that drops another one - stresses column pruning.
			var result = t
				.Pivot(x => x.Category, x => x.Year, new[] { 2000, 2010 },
					PivotCell<CategorySales, int>.Sum(x => x.Amount, Year))
				.Where(r => Sql.Property<decimal?>(r, "Y2010") >= 15)
				.Select(r => new { r.Key, Y2010 = Sql.Property<decimal?>(r, "Y2010") })
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);
			result[0].Key.ShouldBe("A");
			result[0].Y2010.ShouldBe(20m);
			result[1].Key.ShouldBe("B");
			result[1].Y2010.ShouldBe(15m);
		}

		#endregion

		#region Custom aggregates and empty cells

		/// <summary>
		/// A cell can carry an aggregate outside the five named ones - here a distinct count, which the closed
		/// enum the cells used to be could not express at all.
		/// </summary>
		[Test]
		public void PivotsWithACustomAggregate([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(CategorySales.DuplicateData);

			var result = t
				.Pivot(x => x.Category, x => x.Year, new[] { 2000 },
					PivotCell<CategorySales, int>.Custom(rows => rows.Select(x => x.Amount).Distinct().Count(), y => "Distinct" + Year(y)),
					PivotCell<CategorySales, int>.Count(y => "Rows" + Year(y)))
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0]["DistinctY2000"].ShouldBe(2);
			result[0]["RowsY2000"]    .ShouldBe(3);

			result[1].Key.ShouldBe("B");
			result[1]["DistinctY2000"].ShouldBe(1);
			result[1]["RowsY2000"]    .ShouldBe(1);
		}

		[Table]
		sealed class StrictSales
		{
			[Column] public string   Category { get; set; } = null!;
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

		/// <summary>
		/// A cell no row matches reads null, not <c>default(TCell)</c> - asserted once per lift mechanism, since
		/// a named cell can only lift the aggregated value and a custom one can only lift its result.
		/// </summary>
		[Test]
		public void AnEmptyCellOverANonNullableColumnReadsNull([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(StrictSales.Data);

			var result = t
				.Pivot(x => x.Category, x => x.Year, new[] { 2000, 2010 },
					PivotCell<StrictSales, int>.Sum(x => x.Amount, y => "Sum" + Year(y)),
					PivotCell<StrictSales, int>.Custom(rows => rows.Max(x => x.At), y => "At" + Year(y)),
					// SUM over a non-nullable column has its own null-handling path (a COALESCE rewriter), so it
					// needs its own pin even though the result lift is the same one the Max cell proves.
					PivotCell<StrictSales, int>.Custom(rows => rows.Sum(x => x.Amount), y => "CustomSum" + Year(y)))
				.ToList()
				.OrderBy(r => r.Key, StringComparer.Ordinal)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Key.ShouldBe("A");
			result[0]["SumY2000"]      .ShouldBe(10);
			result[0]["CustomSumY2000"].ShouldBe(10);
			result[0]["AtY2000"]       .ShouldBe(new DateTime(2000, 1, 1));
			result[0]["SumY2010"]      .ShouldBeNull();
			result[0]["CustomSumY2010"].ShouldBeNull();
			result[0]["AtY2010"]       .ShouldBeNull();

			result[1].Key.ShouldBe("B");
			result[1]["SumY2000"]      .ShouldBeNull();
			result[1]["CustomSumY2000"].ShouldBeNull();
			result[1]["AtY2000"]       .ShouldBeNull();
			result[1]["SumY2010"]      .ShouldBe(20);
		}

		#endregion

		#region The production pivot from PR #5708

		[Table]
		sealed class ModTemplate
		{
			[Column] public int      Id         { get; set; }
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
			[Column] public int Id { get; set; }

			public static readonly Activity[] Data = { new() { Id = 10 }, new() { Id = 20 }, new() { Id = 30 } };
		}

		[Table]
		sealed class CoaMask
		{
			[Column] public int     Id   { get; set; }
			[Column] public string? Name { get; set; }

			public static readonly CoaMask[] Data = { new() { Id = 1, Name = "Mask one" }, new() { Id = 2, Name = "Mask two" } };
		}

		[Table]
		sealed class AtiRub
		{
			[Column] public int     Id     { get; set; }
			[Column] public string? LibRub { get; set; }

			public static readonly AtiRub[] Data = { new() { Id = 100, LibRub = "POS one" }, new() { Id = 200, LibRub = "POS two" } };
		}

		[Table]
		sealed class IasRub
		{
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
			public int       Id         { get; set; }
			public string?   CoaName    { get; set; }
			public string?   PosLibRub  { get; set; }
			public DateTime? ModifiedAt { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object> Cells { get; set; } = null!;
		}

		static string Ide(int activityId) => "IDE_" + activityId.ToString(CultureInfo.InvariantCulture);
		static string Lib(int activityId) => "LIB_" + activityId.ToString(CultureInfo.InvariantCulture);

		/// <summary>
		/// The acceptance case from
		/// <a href="https://github.com/linq2db/linq2db/pull/5708#issuecomment-4970892781">PR #5708</a>: a joined
		/// and grouped source, two cell templates crossed with a runtime activity list, and static aggregates
		/// beside the generated cells. It used to need a throwaway <see cref="MappingSchema"/>, a
		/// <c>ToSqlQuery().Sql</c> regex splice and a re-entry through <c>FromSql</c> that destroyed composability.
		/// </summary>
		[Test, QueryCacheTest]
		public void PivotsProductionShapeIntoUserType([DataSources] string context)
		{
			using var db      = GetDataContext(context);
			using var mods    = db.CreateLocalTable(ModTemplate.Data);
			using var acts    = db.CreateLocalTable(Activity.Data);
			using var masks   = db.CreateLocalTable(CoaMask.Data);
			using var atiRubs = db.CreateLocalTable(AtiRub.Data);
			using var iasRubs = db.CreateLocalTable(IasRub.Data);

			var activityIds = acts.Select(a => a.Id).OrderBy(id => id).ToList();

			// The source stays an anonymous projection, as it is in the original query: the cell templates come
			// from the factory, so the row type never has to be named.
			var source =
				from e in mods
				join act in acts  on e.TheKey equals act.Id
				join cm  in masks on e.Id     equals cm.Id
				from pos in atiRubs.LeftJoin(r => r.Id == e.PosRubId)
				from rub in iasRubs.LeftJoin(r => r.Id == e.RubId)
				select new { e, cm, pos, rub };

			IQueryable<ListDto> Build(IEnumerable<int> ids) => source
				.Pivot(
					x => x.e.Id,
					x => x.e.TheKey,
					ids,
					g => new ListDto
					{
						Id         = g.Key,
						CoaName    = g.Max(x => x.cm.Name),
						PosLibRub  = g.Max(x => x.pos.LibRub),
						ModifiedAt = g.Max(x => x.e.ModifiedAt),
					},
					c => c.Max(x => x.rub.IdeRub, Ide),
					c => c.Max(x => x.rub.LibRub, Lib));

			var rows = Build(activityIds).ToList().OrderBy(r => r.Id).ToList();

			rows.Count.ShouldBe(2);

			var first = rows[0];

			first.Id        .ShouldBe(1);
			first.CoaName   .ShouldBe("Mask one");
			first.PosLibRub .ShouldBe("POS one");
			first.ModifiedAt.ShouldBe(new DateTime(2020, 1, 3));

			first.Cells.Keys.OrderBy(k => k, StringComparer.Ordinal)
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

			// Still an IQueryable afterwards - a filter over a generated cell composes, which the FromSql
			// workaround could not do at all.
			var filtered = Build(activityIds).Where(r => r.Id == 1 && Sql.Property<string>(r, "IDE_20") == "IDE-B").ToList();

			filtered.Count.ShouldBe(1);
			filtered[0].Cells["IDE_20"].ShouldBe("IDE-B");

			var probe = Build(activityIds);

			probe.ClearCache();

			var start = probe.GetCacheMissCount();

			_ = Build(activityIds).ToList();
			(probe.GetCacheMissCount() - start).ShouldBe(1);

			var widened = Build(activityIds.Concat(new[] { 40 }).ToList()).ToList().OrderBy(r => r.Id).ToList();

			(probe.GetCacheMissCount() - start).ShouldBe(2, "a wider value set is a different query");

			widened[0].Cells.Count        .ShouldBe(8);
			widened[0].Cells["IDE_40"]    .ShouldBeNull();
			widened[0].Cells["IDE_10"]    .ShouldBe("IDE-A");
		}

		/// <summary>The same production shape into the built-in row type, so no result type has to be declared.</summary>
		[Test]
		public void PivotsProductionShapeIntoPivotRow([DataSources] string context)
		{
			using var db      = GetDataContext(context);
			using var mods    = db.CreateLocalTable(ModTemplate.Data);
			using var acts    = db.CreateLocalTable(Activity.Data);
			using var masks   = db.CreateLocalTable(CoaMask.Data);
			using var atiRubs = db.CreateLocalTable(AtiRub.Data);
			using var iasRubs = db.CreateLocalTable(IasRub.Data);

			var activityIds = acts.Select(a => a.Id).OrderBy(id => id).ToList();

			var source =
				from e in mods
				join act in acts  on e.TheKey equals act.Id
				join cm  in masks on e.Id     equals cm.Id
				from pos in atiRubs.LeftJoin(r => r.Id == e.PosRubId)
				from rub in iasRubs.LeftJoin(r => r.Id == e.RubId)
				select new { e, cm, pos, rub };

			var rows = source
				.Pivot(
					x => x.e.Id,
					x => x.e.TheKey,
					activityIds,
					c => c.Max(x => x.rub.IdeRub, Ide),
					c => c.Max(x => x.rub.LibRub, Lib))
				.ToList()
				.OrderBy(r => r.Key)
				.ToList();

			rows.Count.ShouldBe(2);

			rows[0].Key.ShouldBe(1);
			rows[0].Get<string>("IDE_10").ShouldBe("IDE-A");
			rows[0]["LIB_20"]            .ShouldBe("LIB-B");
			rows[0]["IDE_30"]            .ShouldBeNull();

			rows[1].Key.ShouldBe(2);
			rows[1].Get<string>("IDE_10").ShouldBe("IDE-C");
			rows[1]["LIB_30"]            .ShouldBe("LIB-D");
			rows[1]["IDE_20"]            .ShouldBeNull();
		}

		/// <summary>
		/// One conditional aggregate per generated column, and a filter over a generated column becomes a
		/// <c>HAVING</c> over that same aggregate rather than falling back to the client.
		/// </summary>
		[Test]
		public void ProductionShapeLowersToConditionalAggregates([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db      = GetDataConnection(context);
			using var mods    = db.CreateLocalTable(ModTemplate.Data);
			using var acts    = db.CreateLocalTable(Activity.Data);
			using var masks   = db.CreateLocalTable(CoaMask.Data);
			using var atiRubs = db.CreateLocalTable(AtiRub.Data);
			using var iasRubs = db.CreateLocalTable(IasRub.Data);

			var activityIds = acts.Select(a => a.Id).OrderBy(id => id).ToList();

			var source =
				from e in mods
				join act in acts  on e.TheKey equals act.Id
				join cm  in masks on e.Id     equals cm.Id
				from pos in atiRubs.LeftJoin(r => r.Id == e.PosRubId)
				from rub in iasRubs.LeftJoin(r => r.Id == e.RubId)
				select new { e, cm, pos, rub };

			_ = source
				.Pivot(
					x => x.e.Id,
					x => x.e.TheKey,
					activityIds,
					c => c.Max(x => x.rub.IdeRub, Ide),
					c => c.Max(x => x.rub.LibRub, Lib))
				.Where(r => Sql.Property<string>(r, "IDE_20") == "IDE-B")
				.ToList();

			var sql = db.LastQuery!;

			// One conditional aggregate per generated column, plus the one the filter repeats in HAVING - where
			// it stays server-side instead of falling back to the client.
			Occurrences(sql, "WHEN").ShouldBe(activityIds.Count * 2 + 1);
			sql.ShouldContain("HAVING");
		}

		static int Occurrences(string text, string token)
		{
			var count = 0;

			for (var i = text.IndexOf(token, StringComparison.Ordinal); i >= 0; i = text.IndexOf(token, i + token.Length, StringComparison.Ordinal))
				count++;

			return count;
		}

		#endregion
	}
}
