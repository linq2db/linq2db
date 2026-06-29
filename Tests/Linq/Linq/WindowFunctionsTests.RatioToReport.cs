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
		public void RatioToReportBasic([SupportsAnalyticFunctionsContext] string context)
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

		// A zero partition total must yield NULL (native RATIO_TO_REPORT semantics), not a division-by-zero —
		// the emulation wraps the denominator in NULLIF. Without it this errors on PostgreSQL (x / 0).
		[Test]
		public void RatioToReportZeroPartitionSum([SupportsAnalyticFunctionsContext] string context)
		{
			var data = new[]
			{
				new WindowFunctionTestEntity { Id = 1, CategoryId = 1, IntValue =  10 },
				new WindowFunctionTestEntity { Id = 2, CategoryId = 1, IntValue = -10 },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var result =
				(from t in table
				 select new
				 {
					 Id            = t.Id,
					 RatioToReport = Sql.Window.RatioToReport(t.IntValue, w => w.PartitionBy(t.CategoryId)),
				 })
				.ToList();

			result.ShouldAllBe(r => r.RatioToReport == null);
		}
	}
}
