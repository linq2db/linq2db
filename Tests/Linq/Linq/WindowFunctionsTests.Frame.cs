using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void FrameRows([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wndRowsCurrentAndUnbounded = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.Unbounded)
				let wndRowsCurrentAndCurrent = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.CurrentRow)
				let wndRowsValueAndValue = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.ValuePreceding(1).And.ValueFollowing(2))
				select new
				{
					Id = t.Id,

					RowsCurrentAndUnbounded = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.Unbounded),
					RowsCurrentAndCurrent   = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.CurrentRow),
					RowsValueAndValue       = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.ValuePreceding(1).And.ValueFollowing(2)),

					RowsCurrentAndUnboundedDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRowsCurrentAndUnbounded)),
					RowsCurrentAndCurrentDefine   = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRowsCurrentAndCurrent)),
					RowsValueAndValueDefine       = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRowsValueAndValue)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("ROWS");

			foreach (var row in query.ToList())
			{
				var currentAndUnbounded = ExpectedFrameSum(data, row.Id, range: false, "CR", 0, "UF", 0);
				var currentAndCurrent   = ExpectedFrameSum(data, row.Id, range: false, "CR", 0, "CR", 0);
				var valueAndValue       = ExpectedFrameSum(data, row.Id, range: false, "P",  1, "F",  2);

				row.RowsCurrentAndUnbounded.ShouldBe(currentAndUnbounded);
				row.RowsCurrentAndCurrent.ShouldBe(currentAndCurrent);
				row.RowsValueAndValue.ShouldBe(valueAndValue);

				row.RowsCurrentAndUnboundedDefine.ShouldBe(currentAndUnbounded);
				row.RowsCurrentAndCurrentDefine.ShouldBe(currentAndCurrent);
				row.RowsValueAndValueDefine.ShouldBe(valueAndValue);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, ProviderName.Firebird3, ProviderName.Firebird4, ProviderName.Firebird5, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameGroups)]
		public void FrameGroups([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wndGroupsCurrentAndUnbounded = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.CurrentRow.And.Unbounded)
				let wndGroupsCurrentAndCurrent = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.CurrentRow.And.CurrentRow)
				let wndGroupsValueAndValue = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.ValuePreceding(1).And.ValueFollowing(2))
				select new
				{
					Id = t.Id,

					GroupsCurrentAndUnbounded = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.CurrentRow.And.Unbounded),
					GroupsCurrentAndCurrent   = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.CurrentRow.And.CurrentRow),
					GroupsValueAndValue       = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.ValuePreceding(1).And.ValueFollowing(2)),

					GroupsCurrentAndUnboundedDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndGroupsCurrentAndUnbounded)),
					GroupsCurrentAndCurrentDefine   = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndGroupsCurrentAndCurrent)),
					GroupsValueAndValueDefine       = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndGroupsValueAndValue))
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("GROUPS");

			// Unique Id ⇒ each ORDER BY peer group is a single row ⇒ GROUPS frames are equivalent to ROWS.
			foreach (var row in query.ToList())
			{
				var currentAndUnbounded = ExpectedFrameSum(data, row.Id, range: false, "CR", 0, "UF", 0);
				var currentAndCurrent   = ExpectedFrameSum(data, row.Id, range: false, "CR", 0, "CR", 0);
				var valueAndValue       = ExpectedFrameSum(data, row.Id, range: false, "P",  1, "F",  2);

				row.GroupsCurrentAndUnbounded.ShouldBe(currentAndUnbounded);
				row.GroupsCurrentAndCurrent.ShouldBe(currentAndCurrent);
				row.GroupsValueAndValue.ShouldBe(valueAndValue);

				row.GroupsCurrentAndUnboundedDefine.ShouldBe(currentAndUnbounded);
				row.GroupsCurrentAndCurrentDefine.ShouldBe(currentAndCurrent);
				row.GroupsValueAndValueDefine.ShouldBe(valueAndValue);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, TestProvName.AllSapHana, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRange)]
		public void FrameRangeValue([SupportsAnalyticFunctionsContext(
			// SQL Server does not support RANGE with value offsets
			TestProvName.AllSqlServer)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wndRangeValueAndValue = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.ValuePreceding(1).And.ValueFollowing(2))
				select new
				{
					Id = t.Id,

					RangeValueAndValue = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.ValuePreceding(1).And.ValueFollowing(2)),

					RangeValueAndValueDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRangeValueAndValue))
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("RANGE");

			foreach (var row in query.ToList())
			{
				var valueAndValue = ExpectedFrameSum(data, row.Id, range: true, "P", 1, "F", 2);

				row.RangeValueAndValue.ShouldBe(valueAndValue);
				row.RangeValueAndValueDefine.ShouldBe(valueAndValue);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, TestProvName.AllSapHana, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRange)]
		public void FrameRangeNoValue([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wndRangeCurrentAndUnbounded = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.CurrentRow.And.Unbounded)
				let wndRangeCurrentAndCurrent = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.CurrentRow.And.CurrentRow)
				select new
				{
					Id = t.Id,

					RangeCurrentAndUnbounded = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.CurrentRow.And.Unbounded),
					RangeCurrentAndCurrent   = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.CurrentRow.And.CurrentRow),

					RangeCurrentAndUnboundedDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRangeCurrentAndUnbounded)),
					RangeCurrentAndCurrentDefine   = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRangeCurrentAndCurrent)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("RANGE");

			foreach (var row in query.ToList())
			{
				var currentAndUnbounded = ExpectedFrameSum(data, row.Id, range: true, "CR", 0, "UF", 0);
				var currentAndCurrent   = ExpectedFrameSum(data, row.Id, range: true, "CR", 0, "CR", 0);

				row.RangeCurrentAndUnbounded.ShouldBe(currentAndUnbounded);
				row.RangeCurrentAndCurrent.ShouldBe(currentAndCurrent);

				row.RangeCurrentAndUnboundedDefine.ShouldBe(currentAndUnbounded);
				row.RangeCurrentAndCurrentDefine.ShouldBe(currentAndCurrent);
			}
		}

		// Explicit boundary direction — same-direction frames the old positional .Value(...) API could not express.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void FrameRowsExplicitDirection([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id = t.Id,

					// ROWS BETWEEN 5 PRECEDING AND 2 PRECEDING
					RowsPrecedingPreceding = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.ValuePreceding(5).And.ValuePreceding(2)),
					// ROWS BETWEEN 1 FOLLOWING AND 3 FOLLOWING
					RowsFollowingFollowing = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.ValueFollowing(1).And.ValueFollowing(3)),
					// ROWS BETWEEN 1 PRECEDING AND 2 FOLLOWING (shortcut)
					RowsShortcut           = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetweenValues(1, 2)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("ROWS");

			foreach (var row in query.ToList())
			{
				// Same-direction frames can be empty for the leading/trailing rows of a partition; an empty frame
				// aggregates to SQL NULL, materialized as 0 in the non-nullable projection.
				row.RowsPrecedingPreceding.ShouldBe(ExpectedFrameSum(data, row.Id, range: false, "P", 5, "P", 2));
				row.RowsFollowingFollowing.ShouldBe(ExpectedFrameSum(data, row.Id, range: false, "F", 1, "F", 3));
				row.RowsShortcut.ShouldBe(ExpectedFrameSum(data, row.Id, range: false, "P", 1, "F", 2));
			}
		}

		// GROUPS BETWEEN n PRECEDING AND m FOLLOWING shortcut.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, ProviderName.Firebird3, ProviderName.Firebird4, ProviderName.Firebird5, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameGroups)]
		public void FrameGroupsValuesShortcut([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id = t.Id,

					// GROUPS BETWEEN 1 PRECEDING AND 2 FOLLOWING (shortcut)
					GroupsShortcut = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetweenValues(1, 2)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("GROUPS");

			foreach (var row in query.ToList())
				row.GroupsShortcut.ShouldBe(ExpectedFrameSum(data, row.Id, range: false, "P", 1, "F", 2));
		}

		// RANGE BETWEEN n PRECEDING AND m FOLLOWING shortcut (RANGE-with-offset unsupported on SQL Server).
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, TestProvName.AllSapHana, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRange)]
		public void FrameRangeValuesShortcut([SupportsAnalyticFunctionsContext(
			// SQL Server does not support RANGE with value offsets
			TestProvName.AllSqlServer)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id = t.Id,

					// RANGE BETWEEN 1 PRECEDING AND 2 FOLLOWING (shortcut)
					RangeShortcut = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetweenValues(1, 2)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("RANGE");

			foreach (var row in query.ToList())
				row.RangeShortcut.ShouldBe(ExpectedFrameSum(data, row.Id, range: true, "P", 1, "F", 2));
		}

	}
}
