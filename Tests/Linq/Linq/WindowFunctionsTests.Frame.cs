using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void FrameRows([DataSources] string context)
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
					RowsCurrentAndUnbounded = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.Unbounded),
					RowsCurrentAndCurrent   = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.CurrentRow),
					RowsValueAndValue       = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.ValuePreceding(1).And.ValueFollowing(2)),

					RowsCurrentAndUnboundedDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRowsCurrentAndUnbounded)),
					RowsCurrentAndCurrentDefine   = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRowsCurrentAndCurrent)),
					RowsValueAndValueDefine       = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRowsValueAndValue)),
				};

				query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, ProviderName.Firebird3, ProviderName.Firebird4, ProviderName.Firebird5, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameGroups)]
		public void FrameGroups([DataSources] string context)
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
					GroupsCurrentAndUnbounded = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.CurrentRow.And.Unbounded),
					GroupsCurrentAndCurrent   = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.CurrentRow.And.CurrentRow),
					GroupsValueAndValue       = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.ValuePreceding(1).And.ValueFollowing(2)),

					GroupsCurrentAndUnboundedDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndGroupsCurrentAndUnbounded)),
					GroupsCurrentAndCurrentDefine   = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndGroupsCurrentAndCurrent)),
					GroupsValueAndValueDefine       = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndGroupsValueAndValue))
				};

				query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, TestProvName.AllSapHana, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRange)]
		public void FrameRangeValue([DataSources(
			TestProvName.AllAccess,
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
					RangeValueAndValue = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.ValuePreceding(1).And.ValueFollowing(2)),

					RangeValueAndValueDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRangeValueAndValue))
				};

				query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, TestProvName.AllSapHana, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRange)]
		public void FrameRangeNoValue([DataSources] string context)
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
					RangeCurrentAndUnbounded = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.CurrentRow.And.Unbounded),
					RangeCurrentAndCurrent   = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.CurrentRow.And.CurrentRow),

					RangeCurrentAndUnboundedDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRangeCurrentAndUnbounded)),
					RangeCurrentAndCurrentDefine   = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRangeCurrentAndCurrent)),
				};

				query.ToList();
		}

		// Explicit boundary direction — same-direction frames the old positional .Value(...) API could not express.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void FrameRowsExplicitDirection([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					// ROWS BETWEEN 5 PRECEDING AND 2 PRECEDING
					RowsPrecedingPreceding = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.ValuePreceding(5).And.ValuePreceding(2)),
					// ROWS BETWEEN 1 FOLLOWING AND 3 FOLLOWING
					RowsFollowingFollowing = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.ValueFollowing(1).And.ValueFollowing(3)),
					// ROWS BETWEEN 1 PRECEDING AND 2 FOLLOWING (shortcut)
					RowsShortcut           = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetweenValues(1, 2)),
				};

				query.ToList();
		}

		// GROUPS BETWEEN n PRECEDING AND m FOLLOWING shortcut.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, ProviderName.Firebird3, ProviderName.Firebird4, ProviderName.Firebird5, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameGroups)]
		public void FrameGroupsValuesShortcut([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					// GROUPS BETWEEN 1 PRECEDING AND 2 FOLLOWING (shortcut)
					GroupsShortcut = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetweenValues(1, 2)),
				};

				query.ToList();
		}

		// RANGE BETWEEN n PRECEDING AND m FOLLOWING shortcut (RANGE-with-offset unsupported on SQL Server).
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, TestProvName.AllSapHana, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRange)]
		public void FrameRangeValuesShortcut([DataSources(
			TestProvName.AllAccess,
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
					// RANGE BETWEEN 1 PRECEDING AND 2 FOLLOWING (shortcut)
					RangeShortcut = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetweenValues(1, 2)),
				};

				query.ToList();
		}

	}
}
