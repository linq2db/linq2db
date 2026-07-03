using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		// Hypothetical-set RANK(value) WITHIN GROUP (ORDER BY key): the rank the value would have (with gaps) in the
		// ordered group. Native on Oracle and PostgreSQL; throws elsewhere.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllSQLite, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllClickHouse,
			TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllDuckDB, TestProvName.AllDB2,
			ErrorMessage = ErrorHelper.Error_WindowFunction_HypotheticalSet)]
		public void HypotheticalRank([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					g.Key,
					Rank1 = g.Rank(1000, (e, f) => f.OrderBy(e.IntValue)),
					Rank2 = g.Rank(1000, 2000L, (e, f) => f.OrderBy(e.IntValue).ThenBy(e.LongValue)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("WITHIN GROUP");
			sql.ShouldContain("RANK");

				_ = query.ToList();
		}

		// Hypothetical-set DENSE_RANK(value) WITHIN GROUP (ORDER BY key): the rank the value would have (no gaps).
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllSQLite, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllClickHouse,
			TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllDuckDB, TestProvName.AllDB2,
			ErrorMessage = ErrorHelper.Error_WindowFunction_HypotheticalSet)]
		public void HypotheticalDenseRank([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					g.Key,
					DenseRank1 = g.DenseRank(1000, (e, f) => f.OrderBy(e.IntValue)),
					DenseRank2 = g.DenseRank(1000, 2000L, (e, f) => f.OrderBy(e.IntValue).ThenBy(e.LongValue)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("WITHIN GROUP");
			sql.ShouldContain("DENSE_RANK");

				_ = query.ToList();
		}

		// Hypothetical-set PERCENT_RANK(value) WITHIN GROUP (ORDER BY key): the relative rank (0..1) the value would have.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllSQLite, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllClickHouse,
			TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllDuckDB, TestProvName.AllDB2,
			ErrorMessage = ErrorHelper.Error_WindowFunction_HypotheticalSet)]
		public void HypotheticalPercentRank([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					g.Key,
					PercentRank1 = g.PercentRank(1000, (e, f) => f.OrderBy(e.IntValue)),
					PercentRank2 = g.PercentRank(1000, 2000L, (e, f) => f.OrderBy(e.IntValue).ThenBy(e.LongValue)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("WITHIN GROUP");
			sql.ShouldContain("PERCENT_RANK");

				_ = query.ToList();
		}

		// Hypothetical-set CUME_DIST(value) WITHIN GROUP (ORDER BY key): the cumulative distribution (0..1) of the value.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllSQLite, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllClickHouse,
			TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, TestProvName.AllDuckDB, TestProvName.AllDB2,
			ErrorMessage = ErrorHelper.Error_WindowFunction_HypotheticalSet)]
		public void HypotheticalCumeDist([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					g.Key,
					CumeDist1 = g.CumeDist(1000, (e, f) => f.OrderBy(e.IntValue)),
					CumeDist2 = g.CumeDist(1000, 2000L, (e, f) => f.OrderBy(e.IntValue).ThenBy(e.LongValue)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("WITHIN GROUP");
			sql.ShouldContain("CUME_DIST");

				_ = query.ToList();
		}
	}
}
