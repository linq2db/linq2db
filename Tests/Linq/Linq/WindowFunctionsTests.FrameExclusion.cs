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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, ProviderName.Firebird4, ProviderName.Firebird5, TestProvName.AllDB2, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllSapHana, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void FrameRowsExcludeCurrentRow([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id                = t.Id,
					ExcludeCurrentRow = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("EXCLUDE");

			foreach (var row in query.ToList())
				row.ExcludeCurrentRow.ShouldBe(ExpectedFrameSum(data, row.Id, range: false, "UP", 0, "UF", 0, "current"));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, ProviderName.Firebird4, ProviderName.Firebird5, TestProvName.AllDB2, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllSapHana, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void FrameRowsExcludeGroup([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id           = t.Id,
					ExcludeGroup = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeGroup()),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("EXCLUDE");

			foreach (var row in query.ToList())
				row.ExcludeGroup.ShouldBe(ExpectedFrameSum(data, row.Id, range: false, "UP", 0, "UF", 0, "group"));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, ProviderName.Firebird4, ProviderName.Firebird5, TestProvName.AllDB2, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllSapHana, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void FrameRowsExcludeTies([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id          = t.Id,
					ExcludeTies = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded.ExcludeTies()),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("EXCLUDE");

			// Unique Id ⇒ no ties ⇒ EXCLUDE TIES removes nothing ⇒ the full partition total. The EXCLUDE-clause
			// presence is guarded by the ShouldContain above, since the value alone equals the no-EXCLUDE result.
			foreach (var row in query.ToList())
				row.ExcludeTies.ShouldBe(ExpectedFrameSum(data, row.Id, range: false, "UP", 0, "UF", 0, "ties"));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, TestProvName.AllSapHana, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRange)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, ProviderName.Firebird4, ProviderName.Firebird5, TestProvName.AllDB2, TestProvName.AllInformix, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameExclude)]
		public void FrameRangeExclude([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id                = t.Id,
					ExcludeCurrentRow = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RangeBetween.Unbounded.And.Unbounded.ExcludeCurrentRow()),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("EXCLUDE");

			foreach (var row in query.ToList())
				row.ExcludeCurrentRow.ShouldBe(ExpectedFrameSum(data, row.Id, range: true, "UP", 0, "UF", 0, "current"));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, ProviderName.Firebird3, ProviderName.Firebird4, ProviderName.Firebird5, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllOracle, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameGroups)]
		public void FrameGroupsExclude([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id          = t.Id,
					ExcludeTies = Sql.Window.Sum(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).GroupsBetween.Unbounded.And.Unbounded.ExcludeTies()),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");
			sql.ShouldContain("EXCLUDE");

			// GROUPS over unique Id ⇒ each group is one row ⇒ EXCLUDE TIES removes nothing ⇒ full partition total.
			foreach (var row in query.ToList())
				row.ExcludeTies.ShouldBe(ExpectedFrameSum(data, row.Id, range: false, "UP", 0, "UF", 0, "ties"));
		}
	}
}
