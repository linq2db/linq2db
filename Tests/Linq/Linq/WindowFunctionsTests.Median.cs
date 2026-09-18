using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		// MEDIAN(x) OVER (PARTITION BY ...) is native on Oracle, DB2, DuckDB and MariaDB; its OVER clause carries PARTITION BY only.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSQLite, TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllPostgreSQL, TestProvName.AllMySql80, TestProvName.AllClickHouse,
			ProviderName.Firebird3, ProviderName.Firebird4, TestProvName.AllFirebird5Plus, TestProvName.AllInformix, ProviderName.Ydb,
			ErrorMessage = ErrorHelper.Error_WindowFunction_Median)]
		public void MedianBasic([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id     = t.Id,
					Median = Sql.Window.Median(t.IntValue, w => w.PartitionBy(t.CategoryId)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("MEDIAN");

				_ = query.ToList();
		}
	}
}
