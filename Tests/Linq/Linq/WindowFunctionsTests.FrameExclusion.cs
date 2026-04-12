using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void FrameRowsExcludeCurrentRow([IncludeDataSources(
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
					ExcludeCurrentRow = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
				};

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

		[Test]
		public void FrameRowsExcludeGroup([IncludeDataSources(
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
					ExcludeGroup = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeGroup()),
				};

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

		[Test]
		public void FrameRowsExcludeTies([IncludeDataSources(
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
					ExcludeTies = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeTies()),
				};

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

		[Test]
		public void FrameRangeExclude([IncludeDataSources(
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
					ExcludeCurrentRow = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
				};

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

		[Test]
		public void FrameGroupsExclude([IncludeDataSources(
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
					ExcludeTies = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.Unbounded.And.Unbounded.ExcludeTies()),
				};

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}
	}
}
