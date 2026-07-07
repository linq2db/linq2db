using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_LeadLag)]
		public void LagSimple([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id  = t.Id,
					Lag = Sql.Window.Lag(t.IntValue, w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_LeadLag)]
		public void LagWithOffset([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id  = t.Id,
					Lag = Sql.Window.Lag(t.IntValue, 2, w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_LeadLag)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Ydb, TestProvName.AllMariaDB, ErrorMessage = ErrorHelper.Error_WindowFunction_LeadLagDefault)]
		public void LagWithOffsetAndDefault([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id  = t.Id,
					Lag = Sql.Window.Lag(t.IntValue, 2, 0, w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_LeadLag)]
		public void LagWithPartition([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id  = t.Id,
					Lag = Sql.Window.Lag(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		// IGNORE NULLS for LEAD/LAG is supported by Oracle, DB2, Informix and SQL Server 2022+.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_LeadLag)]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllPostgreSQL18Minus, TestProvName.AllMySql8Plus, TestProvName.AllSQLite, TestProvName.AllClickHouse,
			TestProvName.AllFirebird3Plus, TestProvName.AllSapHana,
			TestProvName.AllSqlServer2012, TestProvName.AllSqlServer2014, TestProvName.AllSqlServer2016, TestProvName.AllSqlServer2017, TestProvName.AllSqlServer2019,
			ProviderName.Ydb,
			ErrorMessage = ErrorHelper.Error_WindowFunction_NullTreatment)]
		public void LagIgnoreNulls([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id  = t.Id,
					Lag = Sql.Window.Lag(t.IntValue, w => w.IgnoreNulls().PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}
	}
}
