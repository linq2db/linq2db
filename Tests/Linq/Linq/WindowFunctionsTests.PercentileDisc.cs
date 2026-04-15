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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
				[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileDisc)]
		public void PercentileDiscGrouping([DataSources(TestProvName.AllOracleNative, TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3, TestProvName.AllSqlCe)] string context)
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
		// TODO: error message does not propagate correctly from aggregation builder for some providers
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, TestProvName.AllSqlServer2012Plus, TestProvName.AllMySql80, TestProvName.AllMariaDB, TestProvName.AllSQLite, TestProvName.AllFirebird3Plus, TestProvName.AllDB2, TestProvName.AllInformix, ProviderName.Ydb, ErrorMessage = ErrorHelper.Error_WindowFunction_PercentileDisc)]
		// TODO: Oracle 11 error propagation issue in aggregation builder
		public void PercentileDiscGroupingProjection([DataSources(TestProvName.AllOracleNative, TestProvName.AllOracle11, TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3, TestProvName.AllSqlCe)] string context)
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
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
