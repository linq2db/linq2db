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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		public void FirstValueBasic([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id         = t.Id,
					FirstValue = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		// An argument is a value position: a boolean expression has to be folded into a value, since providers
		// without a native boolean type reject a bare predicate there.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		public void FirstValueBoolean([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var result =
				(from t in table
				select new
				{
					Id      = t.Id,
					Boolean = Sql.Window.FirstValue(t.IntValue == 20, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Value   = Sql.Window.FirstValue(t.IntValue,       w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				})
				.OrderBy(t => t.Id)
				.ToList();

			// The folded flag must round-trip as a bool matching the same window's first value.
			result.ShouldAllBe(r => r.Boolean == (r.Value == 20));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void FirstValueWithFrame([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id         = t.Id,
					FirstValue = Sql.Window.FirstValue(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		public void FirstValueWithDefineWindow([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					Id         = t.Id,
					FirstValue = Sql.Window.FirstValue(t.IntValue, w => w.UseWindow(wnd)),
				};

				_ = query.ToList();
		}

		// IGNORE NULLS for FIRST_VALUE/LAST_VALUE is supported by Oracle, DB2, Informix, SQL Server 2022+ and YDB (not in the test matrix).
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllPostgreSQL18Minus, TestProvName.AllMySql8Plus, TestProvName.AllSQLite, TestProvName.AllClickHouse,
			TestProvName.AllFirebird3Plus, TestProvName.AllSapHana,
			TestProvName.AllSqlServer2012, TestProvName.AllSqlServer2014, TestProvName.AllSqlServer2016, TestProvName.AllSqlServer2017, TestProvName.AllSqlServer2019,
			ErrorMessage = ErrorHelper.Error_WindowFunction_NullTreatment)]
		public void FirstValueIgnoreNulls([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id         = t.Id,
					FirstValue = Sql.Window.FirstValue(t.IntValue, w => w.IgnoreNulls().PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		// RESPECT NULLS is the SQL default; it emits nothing and is never gated, so it behaves like a plain FIRST_VALUE.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_FirstLastValue)]
		public void FirstValueRespectNulls([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id         = t.Id,
					FirstValue = Sql.Window.FirstValue(t.IntValue, w => w.RespectNulls().PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}
	}
}
