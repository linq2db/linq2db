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

		// A zero partition total must yield NULL (native RATIO_TO_REPORT semantics on Oracle, and the emulation wraps
		// the denominator in NULLIF), not a division-by-zero — without it this errors on PostgreSQL (x / 0).
		// DB2 diverges: RATIO_TO_REPORT yields a DECFLOAT Infinity which the reader can't map (see #5663).
		[Test]
		[ActiveIssue(5663, Configurations = [TestProvName.AllDB2], Details = "DB2 RATIO_TO_REPORT yields DECFLOAT Infinity on a zero partition sum; reader can't map the special value")]
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
