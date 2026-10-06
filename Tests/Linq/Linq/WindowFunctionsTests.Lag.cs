using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

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

		// An argument is a value position: a boolean expression has to be folded into a value, since providers
		// without a native boolean type reject a bare predicate there.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_LeadLag)]
		public void LagBoolean([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var result =
				(from t in table
				select new
				{
					Id      = t.Id,
					Boolean = Sql.Window.Lag(t.IntValue == 20, w => w.OrderBy(t.Id)),
					Value   = Sql.Window.Lag(t.IntValue,       w => w.OrderBy(t.Id)),
				})
				.OrderBy(t => t.Id)
				.ToList();

			// The folded flag must round-trip as a bool matching the same window's lagged value.
			result.ShouldAllBe(r => r.Boolean == (r.Value == 20));
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
