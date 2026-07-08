using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebirdLess6, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileDisc)]
		public void PercentileDiscGrouping([SupportsAnalyticFunctionsContext] string context)
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
					PercentileDisc = g.PercentileDisc(0.5, (e, f) => f.OrderBy(e.IntValue)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebirdLess6, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileDisc)]
		public void PercentileDiscGroupingProjection([SupportsAnalyticFunctionsContext] string context)
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
					PercentileDisc = g.AsQueryable().Select(e => new
					{
						PD = g.PercentileDisc(0.5, (e2, f) => f.OrderBy(e2.IntValue)),
					}).First()
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebirdLess6, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileDisc)]
		public async Task PercentileDisc([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var result1 = table.AggregateExecute(g => g.PercentileDisc(0.5, (e, f) => f.OrderBy(e.IntValue)));

			var result2 = await table.AggregateExecuteAsync(g => g.PercentileDisc(0.5, (e, f) => f.OrderBy(e.IntValue)));
		}

		// Windowed ordered-set form: PERCENTILE_DISC(f) WITHIN GROUP (ORDER BY k) OVER (PARTITION BY ...). Native on
		// Oracle, SQL Server 2012+ and MariaDB; PostgreSQL/DB2/DuckDB support only the group form (g.PercentileDisc) above.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSQLite, TestProvName.AllSqlServer2008Minus, TestProvName.AllPostgreSQL, TestProvName.AllMySql80, TestProvName.AllClickHouse,
			ProviderName.Firebird3, ProviderName.Firebird4, TestProvName.AllFirebird5Plus, TestProvName.AllInformix, ProviderName.Ydb,
			TestProvName.AllDB2, TestProvName.AllDuckDB,
			ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileDisc)]
		public void PercentileDiscWindowed([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id      = t.Id,
					Percent = Sql.Window.PercentileDisc(0.5, w => w.OrderBy(t.IntValue).PartitionBy(t.CategoryId)),
				};

				_ = query.ToList();
		}
	}
}
