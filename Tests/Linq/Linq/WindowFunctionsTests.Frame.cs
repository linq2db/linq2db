using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void FrameRows([IncludeDataSources(
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
				let wndRowsCurrentAndUnbounded = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.Unbounded)
				let wndRowsCurrentAndCurrent = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.CurrentRow)
				let wndRowsValueAndValue = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Value(1).And.Value(2))
				select new
				{
					RowsCurrentAndUnbounded = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.Unbounded),
					RowsCurrentAndCurrent   = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.CurrentRow),
					RowsValueAndValue       = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Value(1).And.Value(2)),

					RowsCurrentAndUnboundedDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRowsCurrentAndUnbounded)),
					RowsCurrentAndCurrentDefine   = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRowsCurrentAndCurrent)),
					RowsValueAndValueDefine       = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRowsValueAndValue)),
				};

			Assert.DoesNotThrow(() =>
			{
				query.ToList();
			});
		}

		[Test]
		public void FrameGroups([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wndGroupsCurrentAndUnbounded = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.CurrentRow.And.Unbounded)
				let wndGroupsCurrentAndCurrent = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.CurrentRow.And.CurrentRow)
				let wndGroupsValueAndValue = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.Value(1).And.Value(2))
				select new
				{
					GroupsCurrentAndUnbounded = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.CurrentRow.And.Unbounded),
					GroupsCurrentAndCurrent   = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.CurrentRow.And.CurrentRow),
					GroupsValueAndValue       = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.Value(1).And.Value(2)),

					GroupsCurrentAndUnboundedDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndGroupsCurrentAndUnbounded)),
					GroupsCurrentAndCurrentDefine   = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndGroupsCurrentAndCurrent)),
					GroupsValueAndValueDefine       = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndGroupsValueAndValue))
				};

			Assert.DoesNotThrow(() =>
			{
				query.ToList();
			});
		}

		[Test]
		public void FrameRangeValue([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wndRangeValueAndValue = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.Value(1).And.Value(2))
				select new
				{
					RangeValueAndValue = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.Value(1).And.Value(2)),

					RangeValueAndValueDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRangeValueAndValue))
				};

			Assert.DoesNotThrow(() =>
			{
				query.ToList();
			});
		}

		[Test]
		public void FrameRangeNoValue([IncludeDataSources(
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
				let wndRangeCurrentAndUnbounded = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.CurrentRow.And.Unbounded)
				let wndRangeCurrentAndCurrent = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.CurrentRow.And.CurrentRow)
				select new
				{
					RangeCurrentAndUnbounded = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.CurrentRow.And.Unbounded),
					RangeCurrentAndCurrent   = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.CurrentRow.And.CurrentRow),

					RangeCurrentAndUnboundedDefine = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRangeCurrentAndUnbounded)),
					RangeCurrentAndCurrentDefine   = Sql.Window.Sum(t.IntValue, w => w.UseWindow(wndRangeCurrentAndCurrent)),
				};

			Assert.DoesNotThrow(() =>
			{
				query.ToList();
			});
		}

	}
}
