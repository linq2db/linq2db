using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		// An argument is a value position: a boolean expression has to be folded into a value, since providers
		// without a native boolean type reject a bare predicate there.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void LastValueBoolean([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var result =
				(from t in table
				select new
				{
					Id      = t.Id,
					Boolean = Sql.Window.LastValue(t.IntValue == 20, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
					Value   = Sql.Window.LastValue(t.IntValue,       w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
				})
				.OrderBy(t => t.Id)
				.ToList();

			// The folded flag must round-trip as a bool matching the same window's last value.
			result.ShouldAllBe(r => r.Boolean == (r.Value == 20));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void LastValueBasic([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id        = t.Id,
					LastValue = Sql.Window.LastValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void LastValueWithFrame([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id        = t.Id,
					LastValue = Sql.Window.LastValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.CurrentRow.And.Unbounded),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void LastValueWithDefineWindow([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded)
				select new
				{
					Id        = t.Id,
					LastValue = Sql.Window.LastValue(t.IntValue, w => w.UseWindow(wnd)),
				};

				_ = query.ToList();
		}

		// IGNORE NULLS for FIRST_VALUE/LAST_VALUE is supported by Oracle, DB2, Informix, SQL Server 2022+ and YDB (not in the test matrix).
		// No frame is used here so the test isolates the null-treatment gate from frame-support gates.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllPostgreSQL18Minus, TestProvName.AllMySql8Plus, TestProvName.AllSQLite, TestProvName.AllClickHouse,
			TestProvName.AllFirebird3Plus, TestProvName.AllSapHana,
			TestProvName.AllSqlServer2012, TestProvName.AllSqlServer2014, TestProvName.AllSqlServer2016, TestProvName.AllSqlServer2017, TestProvName.AllSqlServer2019,
			ErrorMessage = ErrorHelper.Error_WindowFunction_NullTreatment)]
		public void LastValueIgnoreNulls([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id        = t.Id,
					LastValue = Sql.Window.LastValue(t.IntValue, w => w.IgnoreNulls().PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}
	}
}
