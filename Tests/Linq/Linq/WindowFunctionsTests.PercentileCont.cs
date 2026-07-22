using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		// WITHIN GROUP (ORDER BY ...) items are SqlWindowOrderItem, same as OVER (ORDER BY): a boolean sort key
		// is a value position and has to be folded.
		//
		// PostgreSQL and DuckDB are excluded rather than gated: they have a native boolean type, so nothing is
		// folded there, and PERCENTILE_CONT requires a numeric sort key — percentile_cont(numeric, boolean) /
		// quantile_cont(BOOLEAN, ...) simply does not exist. That is a provider limitation, not a translation bug.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllPostgreSQL93Minus, TestProvName.AllSqlServer2008Minus, TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileCont)]
		public void PercentileContWithBooleanOrderBy([SupportsAnalyticFunctionsContext(TestProvName.AllPostgreSQL, TestProvName.AllDuckDB)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					CategoryId = g.Key,
					Boolean    = g.PercentileCont(0.5, (e, f) => f.OrderBy(e.IntValue == 20)),
					NullCheck  = g.PercentileCont(0.5, (e, f) => f.OrderBy(e.NullableIntValue != null)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllPostgreSQL93Minus, TestProvName.AllSqlServer2008Minus, TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileCont)]
		public void PercentileContGrouping([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					CategoryId                     = g.Key,
					PercentileContDecimal          = g.PercentileCont(0.5, (e,               f) => f.OrderBy(e.DecimalValue)),
					PercentileContQueryableDecimal = g.AsQueryable().PercentileCont(0.5, (e, f) => f.OrderBy(e.DecimalValue)),
					PercentileContInt              = g.PercentileCont(0.5, (e,               f) => f.OrderByDesc(e.IntValue)),
					PercentileContQueryableInt     = g.AsQueryable().PercentileCont(0.5, (e, f) => f.OrderByDesc(e.IntValue)),
				};

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("SELECT", Exactly.Once());
			sql.ShouldContain("PERCENTILE_CONT", Exactly.Times(4));

				query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllPostgreSQL93Minus, TestProvName.AllSqlServer2008Minus, TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileCont)]
		// PERCENTILE_CONT works on Oracle/DuckDB/DB2; FILTER on an ordered-set aggregate is supported only by PostgreSQL and DuckDB.
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllOracle, TestProvName.AllDB2, ErrorMessage = ErrorHelper.Error_WindowFunction_OrderedSetFilter)]
		public void PercentileContFilter([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					CategoryId = g.Key,
					Median     = g.PercentileCont(0.5, (e, f) => f.OrderBy(e.DecimalValue).Filter(e.IntValue > 0)),
				};

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("PERCENTILE_CONT", Exactly.Once());
			sql.ShouldContain("FILTER",          Exactly.Once());

			query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllPostgreSQL93Minus, TestProvName.AllSqlServer2008Minus, TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileCont)]
		public void PercentileContGroupingProjection([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					CategoryId                           = g.Key,
					PercentileContQueryableIntProjection = g.AsQueryable().Select(x => new {x}).PercentileCont(0.5, (e, f) => f.OrderByDesc(e.x.IntValue)),
					PercentileContInt                    = g.PercentileCont(0.5, (e,                                    f) => f.OrderByDesc(e.IntValue)),
				};

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("SELECT", Exactly.Once());
			sql.ShouldContain("PERCENTILE_CONT", Exactly.Twice());

				query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllPostgreSQL93Minus, TestProvName.AllSqlServer2008Minus, TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileCont)]
		public void PercentileSubquery([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					CategoryId = t.CategoryId, 
					PercentileContDecimal = table.Where(x => x.Id > t.Id).Where(x => x.DecimalValue > 1).PercentileCont(0.5, (e, f) => f.OrderBy(e.DecimalValue)),
				};

			query.ToSqlQuery().Sql.ShouldContain("PERCENTILE_CONT");

				query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllPostgreSQL93Minus, TestProvName.AllSqlServer2008Minus, TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileCont)]
		public async Task PercentileCont([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var decimalValue = table.AggregateExecute(g => g.PercentileCont(0.5, (e, f) => f.OrderByDesc(e.DecimalValue)));
			var intValue     = await table.AggregateExecuteAsync(g => g.PercentileCont(0.5, (e, f) => f.OrderByDesc(e.IntValue)));
		}

		// Windowed ordered-set form: PERCENTILE_CONT(f) WITHIN GROUP (ORDER BY k) OVER (PARTITION BY ...). Native on
		// Oracle, SQL Server 2012+ and MariaDB; PostgreSQL/DB2/DuckDB support only the group form (g.PercentileCont) above.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSQLite, TestProvName.AllSqlServer2008Minus, TestProvName.AllPostgreSQL, TestProvName.AllMySql80, TestProvName.AllClickHouse,
			ProviderName.Firebird3, ProviderName.Firebird4, TestProvName.AllFirebird5Plus, TestProvName.AllInformix, ProviderName.Ydb,
			TestProvName.AllDB2, TestProvName.AllDuckDB,
			ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileCont)]
		public void PercentileContWindowed([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id      = t.Id,
					Percent = Sql.Window.PercentileCont(0.5, w => w.OrderBy(t.DoubleValue).PartitionBy(t.CategoryId)),
				};

				_ = query.ToList();
		}
	}
}
