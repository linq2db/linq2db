using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		public void SumWithFilter([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					SumFiltered = Sql.Window.Sum(t.IntValue, w => w.Filter(t.CategoryId == 1).PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

			_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		public void AverageWithFilter([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					AvgFiltered = Sql.Window.Average(t.DoubleValue, w => w.Filter(t.CategoryId == 1).PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

			_ = query.ToList();
		}
	}
}
