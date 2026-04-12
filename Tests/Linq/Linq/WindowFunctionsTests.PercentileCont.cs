using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer, TestProvName.AllMySql, TestProvName.AllSQLite, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileCont)]
		public void PercentileContGrouping([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
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
					PercentileContQueryableInt     = g.AsQueryable().PercentileCont(0.5, (e, f) => f.OrderByDesc(e.IntValue)),
				};

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("SELECT", Exactly.Once());
			sql.ShouldContain("PERCENTILE_CONT", Exactly.Times(4));

				query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer, TestProvName.AllMySql, TestProvName.AllSQLite, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileCont)]
		public void PercentileContGroupingProjection([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					CategoryId                           = g.Key,
					PercentileContQueryableIntProjection = g.AsQueryable().Select(x => new {x}).PercentileCont(0.5, (e, f) => f.OrderByDesc(e.x.IntValue)),
					PercentileContInt                    = g.PercentileCont(0.5, (e,                                    f) => f.OrderByDesc(e.IntValue)),
				};

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("SELECT", Exactly.Once());
			sql.ShouldContain("PERCENTILE_CONT", Exactly.Twice());

				query.ToList();
		}

		[Test, Explicit("IQueryable overload ambiguity with public WindowFunctionBuilder — needs review")]
		public void PercentileSubquery([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
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

			query.ToSqlQuery().Sql.ShouldContain("PERCENTILE_CONT");

				query.ToList();
		}

		[Test, Explicit("IQueryable overload ambiguity with public WindowFunctionBuilder — needs review")]
		public async Task PercentileCont([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var decimalValue = table.PercentileCont(0.5, (e, f) => f.OrderBy(e.DecimalValue));
			var intValue     = await table.PercentileContAsync(0.5, (e, f) => f.OrderByDesc(e.IntValue));
		}

	}
}
