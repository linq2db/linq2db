using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void PercentileContGrouping([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
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
					PercentileContQueryableInt     = g.AsQueryable().PercentileCont(0.5, (e, f) => f.OrderByDesc(e.IntValue))
				};

			Assert.DoesNotThrow(() =>
			{
				query.ToList();
			});
		}

		[Test]
		public void PercentileSubquery([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
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

			query.ToSqlQuery().Sql.Should().Contain("PERCENTILE_CONT");

			Assert.DoesNotThrow(() =>
			{
				query.ToList();
			});
		}

		[Test]
		public async Task PercentileCont([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var decimalValue = table.PercentileCont(0.5, (e, f) => f.OrderBy(e.DecimalValue));
			var intValue     = await table.PercentileContAsync(0.5, (e, f) => f.OrderByDesc(e.IntValue));
		}

	}
}
