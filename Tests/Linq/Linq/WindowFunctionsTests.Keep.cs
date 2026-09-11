using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		// KEEP (DENSE_RANK FIRST/LAST ORDER BY ...) items are SqlWindowOrderItem, same as OVER (ORDER BY): a
		// boolean sort key is a value position and has to be folded.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllDuckDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllPostgreSQL, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_Keep)]
		public void KeepWithBooleanOrderBy([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					t.CategoryId,
					MinFirst = Sql.Window.Min(t.IntValue, f => f.KeepFirst().OrderBy(t.IntValue == 20).PartitionBy(t.CategoryId)),
					MaxLast  = Sql.Window.Max(t.IntValue, f => f.KeepLast().OrderBy(t.NullableIntValue != null).PartitionBy(t.CategoryId)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllDuckDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllPostgreSQL, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_Keep)]
		public void KeepFirstBasic([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					t.CategoryId,
					MinFirst = Sql.Window.Min(t.IntValue, f => f.KeepFirst().OrderBy(t.DoubleValue).PartitionBy(t.CategoryId)),
					MaxLast  = Sql.Window.Max(t.IntValue, f => f.KeepLast().OrderBy(t.DoubleValue).PartitionBy(t.CategoryId)),
				};

			var result = query.ToList();

			// KEEP (DENSE_RANK FIRST/LAST ORDER BY DoubleValue) evaluated per CategoryId partition.
			// DoubleValue is distinct within each partition, so FIRST is the row with the smallest
			// DoubleValue and LAST the row with the largest; the aggregate then reduces that single row.
			var byCategory = result.GroupBy(r => r.CategoryId).ToDictionary(g => g.Key, g => g.First());

			byCategory[1].MinFirst.ShouldBe(10);
			byCategory[1].MaxLast .ShouldBe(90);
			byCategory[2].MinFirst.ShouldBe(30);
			byCategory[2].MaxLast .ShouldBe(40);
			byCategory[3].MinFirst.ShouldBe(60);
			byCategory[3].MaxLast .ShouldBe(70);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllDuckDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllPostgreSQL, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_Keep)]
		public void KeepWithMultipleOrderBy([SupportsAnalyticFunctionsContext] string context)
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllDuckDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllPostgreSQL, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_Keep)]
		public void KeepWithoutPartition([SupportsAnalyticFunctionsContext] string context)
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer, TestProvName.AllClickHouse, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllDuckDB, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllPostgreSQL, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_Keep)]
		public void KeepAllAggregates([SupportsAnalyticFunctionsContext] string context)
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
