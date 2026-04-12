using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		/// <summary>
		/// Aggregate window functions with various Filter + Frame + Partition + Order combinations.
		/// </summary>
		[Test]
		public void AggregateCombinations([IncludeDataSources(
			true,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					// SUM: filter + partition + order
					SumFiltered             = Sql.Window.Sum(t.IntValue, w => w.Filter(t.CategoryId == 1).PartitionBy(t.CategoryId).OrderBy(t.Id)),
					// SUM: filter + order + frame
					SumFilteredWithFrame    = Sql.Window.Sum(t.IntValue, w => w.Filter(t.IntValue > 20).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					// SUM: partition + order + frame + exclude
					SumFrameExclude         = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
					// SUM: filter + partition + order + frame
					SumFilterPartitionFrame = Sql.Window.Sum(t.IntValue, w => w.Filter(t.IntValue > 10).PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Value(1).And.Value(1)),

					// AVG: filter + order + range frame
					AvgFilteredRange        = Sql.Window.Average(t.DoubleValue, w => w.Filter(t.DoubleValue > 15.0).OrderBy(t.Id).RangeBetween.Unbounded.And.CurrentRow),
					// AVG: partition + order + groups frame + exclude ties
					AvgGroupsExcludeTies    = Sql.Window.Average(t.DoubleValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.Unbounded.And.Unbounded.ExcludeTies()),

					// MIN: filter + partition
					MinFiltered             = Sql.Window.Min(t.IntValue, w => w.Filter(t.IntValue > 10).PartitionBy(t.CategoryId).OrderBy(t.Id)),
					// MAX: filter + frame
					MaxFilteredFrame        = Sql.Window.Max(t.IntValue, w => w.Filter(t.IntValue < 80).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),

					// COUNT: no args + filter
					CountFiltered           = Sql.Window.Count(w => w.Filter(t.IntValue > 20).PartitionBy(t.CategoryId).OrderBy(t.Id)),
					// COUNT: with arg + partition + order + frame
					CountArgWithFrame       = Sql.Window.Count(w => w.Argument(t.NullableIntValue).PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
				};

				_ = query.ToList();
		}

		/// <summary>
		/// Aggregate window functions with Filter on SQL Server (using CASE WHEN workaround — no native FILTER).
		/// </summary>
		[Test]
		public void AggregateCombinationsSqlServer([IncludeDataSources(
			true,
			TestProvName.AllSqlServer2012Plus)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					// SUM: partition + order + frame
					SumFrame           = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					// SUM: partition + order + frame + value boundaries
					SumFrameValues     = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Value(2).And.Value(2)),
					// SUM: range frame
					SumRangeFrame      = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RangeBetween.Unbounded.And.CurrentRow),

					// AVG: partition + order + frame
					AvgFrame           = Sql.Window.Average(t.DoubleValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Value(1).And.CurrentRow),

					// COUNT: with frame
					CountFrame         = Sql.Window.Count(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					// COUNT: arg + frame
					CountArgFrame      = Sql.Window.Count(w => w.Argument(t.NullableIntValue).OrderBy(t.Id).RowsBetween.Value(2).And.Value(2)),

					// Multiple ranking in same select
					RowNum             = Sql.Window.RowNumber(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Rank               = Sql.Window.Rank(w => w.PartitionBy(t.CategoryId).OrderBy(t.IntValue)),
					DenseRank          = Sql.Window.DenseRank(w => w.PartitionBy(t.CategoryId).OrderBy(t.IntValue)),

					// Lead/Lag alongside aggregates
					LeadVal            = Sql.Window.Lead(t.IntValue, 1, 0, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					LagVal             = Sql.Window.Lag(t.IntValue, 1, 0, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),

					// FirstValue/LastValue with frame
					First              = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					Last               = Sql.Window.LastValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
				};

				_ = query.ToList();
		}

		/// <summary>
		/// Value window functions (FirstValue, LastValue, NthValue) with various frame combinations.
		/// </summary>
		[Test]
		public void ValueFunctionFrameCombinations([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			// ClickHouse does not support EXCLUDE in frame clause
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					// FIRST_VALUE: various frames
					FirstRows         = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					FirstRange        = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.Unbounded.And.CurrentRow),

					// LAST_VALUE: needs unbounded following to be useful
					LastRowsAll       = Sql.Window.LastValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
					LastRowsCurrent   = Sql.Window.LastValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.Unbounded),

					// NTH_VALUE: with frame
					NthRowsAll        = Sql.Window.NthValue(t.IntValue, 2L, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),

					// FIRST_VALUE: with frame exclusion
					FirstExclude      = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
				};

				_ = query.ToList();
		}

		/// <summary>
		/// All frame boundary combinations in a single query.
		/// </summary>
		[Test]
		public void AllFrameBoundaryCombinations([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					// ROWS: all 9 boundary combos
					RowsUnbCurr       = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
					RowsUnbUnb        = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
					RowsUnbVal        = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.Value(3)),
					RowsCurrCurr      = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.CurrentRow.And.CurrentRow),
					RowsCurrUnb       = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.CurrentRow.And.Unbounded),
					RowsCurrVal       = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.CurrentRow.And.Value(3)),
					RowsValCurr       = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Value(2).And.CurrentRow),
					RowsValUnb        = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Value(2).And.Unbounded),
					RowsValVal        = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Value(2).And.Value(3)),

					// RANGE: non-value boundaries (value boundaries need single ORDER BY + matching type)
					RangeUnbCurr      = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RangeBetween.Unbounded.And.CurrentRow),
					RangeUnbUnb       = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RangeBetween.Unbounded.And.Unbounded),
					RangeCurrUnb      = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RangeBetween.CurrentRow.And.Unbounded),
				};

				_ = query.ToList();
		}

		/// <summary>
		/// DefineWindow reuse: multiple functions sharing the same window definition.
		/// </summary>
		[Test]
		public void DefineWindowReuse([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wndOrder = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				let wndFrame = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded)
				select new
				{
					// Ranking uses order window
					RowNum     = Sql.Window.RowNumber(w => w.UseWindow(wndOrder)),
					Rank       = Sql.Window.Rank(w => w.UseWindow(wndOrder)),

					// Aggregates use order window
					Sum        = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndOrder)),
					Avg        = Sql.Window.Average(t.DoubleValue, w => w.UseWindow(wndOrder)),
					Min        = Sql.Window.Min(t.IntValue, w => w.UseWindow(wndOrder)),
					Max        = Sql.Window.Max(t.IntValue, w => w.UseWindow(wndOrder)),
					Count      = Sql.Window.Count(w => w.UseWindow(wndOrder)),

					// Lead/Lag use order window
					Lead       = Sql.Window.Lead(t.IntValue, w => w.UseWindow(wndOrder)),
					Lag        = Sql.Window.Lag(t.IntValue, w => w.UseWindow(wndOrder)),

					// Value functions use frame window
					First      = Sql.Window.FirstValue(t.IntValue, w => w.UseWindow(wndFrame)),
					Last       = Sql.Window.LastValue(t.IntValue, w => w.UseWindow(wndFrame)),
				};

				_ = query.ToList();
		}

		/// <summary>
		/// Frame exclusion with all frame types and exclusion modes.
		/// </summary>
		[Test]
		public void FrameExclusionCombinations([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			// ClickHouse does not support EXCLUDE in frame clause
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					// ROWS + all exclusions
					RowsExclCurrRow    = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
					RowsExclGroup      = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeGroup()),
					RowsExclTies       = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeTies()),

					// RANGE + exclusion
					RangeExclCurrRow   = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RangeBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),

					// GROUPS + exclusion
					GroupsExclTies     = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).GroupsBetween.Unbounded.And.Unbounded.ExcludeTies()),

					// Value boundaries + exclusion
					RowsValExcl        = Sql.Window.Sum(t.IntValue, w => w.OrderBy(t.Id).RowsBetween.Value(2).And.Value(2).ExcludeCurrentRow()),
				};

				_ = query.ToList();
		}

		/// <summary>
		/// Lead/Lag combinations with all overloads.
		/// </summary>
		[Test]
		public void LeadLagCombinations([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					// Lead: all overloads
					Lead1          = Sql.Window.Lead(t.IntValue,          w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Lead2          = Sql.Window.Lead(t.IntValue, 2,       w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Lead3          = Sql.Window.Lead(t.IntValue, 2, 0,    w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),

					// Lag: all overloads
					Lag1           = Sql.Window.Lag(t.IntValue,           w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Lag2           = Sql.Window.Lag(t.IntValue, 2,        w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Lag3           = Sql.Window.Lag(t.IntValue, 2, 0,     w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),

					// Lead/Lag with different types
					LeadDouble     = Sql.Window.Lead(t.DoubleValue,       w => w.OrderBy(t.Id)),
					LagDecimal     = Sql.Window.Lag(t.DecimalValue,       w => w.OrderBy(t.Id)),
					LeadNullable   = Sql.Window.Lead(t.NullableIntValue,  w => w.OrderBy(t.Id)),

					// Lead/Lag without partition
					LeadNoPartition = Sql.Window.Lead(t.IntValue,         w => w.OrderBy(t.Id)),
					LagNoPartition  = Sql.Window.Lag(t.IntValue,          w => w.OrderBy(t.Id)),

					// Multiple order by with ThenBy
					LeadMultiOrder = Sql.Window.Lead(t.IntValue,          w => w.OrderBy(t.CategoryId).ThenBy(t.Id)),
				};

				_ = query.ToList();
		}
	}
}
