using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void FrameRowsExcludeCurrentRow([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
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

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void FrameRowsExcludeGroup([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
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

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void FrameRowsExcludeTies([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
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

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void FrameRangeExclude([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
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

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameGroups)]
		public void FrameGroupsExclude([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
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

				_ = query.ToList();
		}
	}
}
