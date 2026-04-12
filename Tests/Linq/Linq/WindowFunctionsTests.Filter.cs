using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void SumWithFilter([IncludeDataSources(
			true,
			TestProvName.AllPostgreSQL)] string context)
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

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

		[Test]
		public void AverageWithFilter([IncludeDataSources(
			true,
			TestProvName.AllPostgreSQL)] string context)
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

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}
	}
}
