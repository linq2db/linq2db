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
			TestProvName.AllSQLite, TestProvName.AllSqlServer2008Minus,
			ProviderName.Firebird3, ProviderName.Firebird4, TestProvName.AllFirebird5Plus, ProviderName.Ydb,
			ErrorMessage = ErrorHelper.Error_WindowFunction_Variance)]
		public void StdDevSampBasic([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id    = t.Id,
					Value = Sql.Window.StdDevSamp(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSQLite, TestProvName.AllSqlServer2008Minus,
			ProviderName.Firebird3, ProviderName.Firebird4, TestProvName.AllFirebird5Plus, ProviderName.Ydb,
			ErrorMessage = ErrorHelper.Error_WindowFunction_Variance)]
		public void StdDevSampViaWindow([SupportsAnalyticFunctionsContext] string context)
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
					Value = Sql.Window.StdDevSamp(t.IntValue, w => w.UseWindow(wnd)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");

				_ = query.ToList();
		}
	}
}
