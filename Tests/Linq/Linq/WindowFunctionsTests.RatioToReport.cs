using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		// RATIO_TO_REPORT is native on Oracle/DB2 and emulated as expr / SUM(expr) OVER (...) on every other window
		// provider, so it only fails where window functions are unsupported.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public void RatioToReportBasic([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id            = t.Id,
					RatioToReport = Sql.Window.RatioToReport(t.IntValue, w => w.PartitionBy(t.CategoryId)),
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("OVER");

				_ = query.ToList();
		}
	}
}
