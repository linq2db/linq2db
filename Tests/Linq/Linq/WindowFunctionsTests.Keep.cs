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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2012Plus, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllPostgreSQL, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_Keep)]
		public void KeepFirstBasic([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					MinFirst = Sql.Window.Min(t.IntValue, f => f.KeepFirst().OrderBy(t.DoubleValue).PartitionBy(t.CategoryId)),
					MaxLast  = Sql.Window.Max(t.IntValue, f => f.KeepLast().OrderBy(t.DoubleValue).PartitionBy(t.CategoryId)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2012Plus, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllPostgreSQL, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_Keep)]
		public void KeepWithMultipleOrderBy([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					SumFirst = Sql.Window.Sum(t.IntValue, f => f.KeepFirst().OrderBy(t.DoubleValue).ThenByDesc(t.Id).PartitionBy(t.CategoryId)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2012Plus, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllPostgreSQL, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_Keep)]
		public void KeepWithoutPartition([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					MinFirst = Sql.Window.Min(t.IntValue, f => f.KeepFirst().OrderBy(t.DoubleValue)),
					MaxLast  = Sql.Window.Max(t.IntValue, f => f.KeepLast().OrderByDesc(t.DoubleValue)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2012Plus, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllPostgreSQL, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_Keep)]
		public void KeepAllAggregates([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					MinFirst = Sql.Window.Min(t.IntValue,     f => f.KeepFirst().OrderBy(t.DoubleValue).PartitionBy(t.CategoryId)),
					MaxFirst = Sql.Window.Max(t.IntValue,     f => f.KeepFirst().OrderBy(t.DoubleValue).PartitionBy(t.CategoryId)),
					SumFirst = Sql.Window.Sum(t.IntValue,     f => f.KeepFirst().OrderBy(t.DoubleValue).PartitionBy(t.CategoryId)),
					AvgFirst = Sql.Window.Average(t.IntValue, f => f.KeepFirst().OrderBy(t.DoubleValue).PartitionBy(t.CategoryId)),
				};

			_ = query.ToList();
		}

		// Note: UseWindow is intentionally NOT available after KeepFirst/KeepLast.
		// KEEP only supports PARTITION BY in the OVER clause — no ORDER BY or frame.
	}
}
