using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
				[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer, TestProvName.AllMySql, TestProvName.AllSQLite, TestProvName.AllFirebird, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileDisc)]
		public void PercentileDiscGrouping([DataSources(TestProvName.AllOracleNative)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					g.Key,
					PercentileDisc = g.PercentileDisc(0.5, (e, f) => f.OrderBy(e.IntValue)),
				};

				_ = query.ToList();
		}

		[Test]
		// TODO: MySQL 5.7 error message does not propagate correctly from aggregation builder
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileDisc)]
		public void PercentileDiscGroupingProjection([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				group t by t.CategoryId into g
				select new
				{
					g.Key,
					PercentileDisc = g.AsQueryable().Select(e => new
					{
						PD = g.PercentileDisc(0.5, (e2, f) => f.OrderBy(e2.IntValue)),
					}).First()
				};

				_ = query.ToList();
		}

		[Test, Explicit("IQueryable overload ambiguity with public WindowFunctionBuilder — needs review")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		public async Task PercentileDisc([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var result1 = table.PercentileDisc(0.5, (e, f) => f.OrderBy(e.IntValue));

			var result2 = await table.PercentileDiscAsync(0.5, (e, f) => f.OrderBy(e.IntValue));
		}
	}
}
