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
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSQLite, TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql8Plus, TestProvName.AllClickHouse,
			ProviderName.Firebird3, ProviderName.Firebird4, TestProvName.AllFirebird5Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb,
			ErrorMessage = ErrorHelper.Error_WindowFunction_LinearRegression)]
		public void RegrSlopeBasic([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id    = t.Id,
					Value = Sql.Window.RegrSlope(t.DoubleValue, t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("REGR_SLOPE");

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSQLite, TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql8Plus, TestProvName.AllClickHouse,
			ProviderName.Firebird3, ProviderName.Firebird4, TestProvName.AllFirebird5Plus, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb,
			ErrorMessage = ErrorHelper.Error_WindowFunction_LinearRegression)]
		public void RegrSlopeViaWindow([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					Id    = t.Id,
					Value = Sql.Window.RegrSlope(t.DoubleValue, t.IntValue, w => w.UseWindow(wnd)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("REGR_SLOPE");

				_ = query.ToList();
		}
	}
}
