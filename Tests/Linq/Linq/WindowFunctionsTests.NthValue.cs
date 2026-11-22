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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		public void NthValueBoolean([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var result =
				(from t in table
				select new
				{
					Id      = t.Id,
					Boolean = Sql.Window.NthValue(t.IntValue == 20, 2L, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					Value   = Sql.Window.NthValue(t.IntValue,       2L, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				})
				.OrderBy(t => t.Id)
				.ToList();

			// The folded flag must round-trip as a bool matching the same window's nth value.
			result.ShouldAllBe(r => r.Boolean == (r.Value == 20));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		public void NthValueBasic([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id       = t.Id,
					NthValue = Sql.Window.NthValue(t.IntValue, 2L, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void NthValueWithFrame([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id       = t.Id,
					NthValue = Sql.Window.NthValue(t.IntValue, 2L, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird3, ErrorMessage = ErrorHelper.Error_WindowFunction_FrameRows)]
		public void NthValueWithDefineWindow([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id).RowsBetween.Unbounded.And.Unbounded)
				select new
				{
					Id       = t.Id,
					NthValue = Sql.Window.NthValue(t.IntValue, 2L, w => w.UseWindow(wnd)),
				};

				_ = query.ToList();
		}

		// IGNORE NULLS for NTH_VALUE is supported by Oracle, DB2 and YDB (not in the test matrix).
		// NTH_VALUE itself is unsupported on SQL Server and Informix; Firebird 3+ supports NTH_VALUE but not IGNORE NULLS.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllPostgreSQL18Minus, TestProvName.AllMySql8Plus, TestProvName.AllSQLite, TestProvName.AllClickHouse,
			ProviderName.Firebird3, ProviderName.Firebird4, TestProvName.AllFirebird5Plus, TestProvName.AllSapHana,
			ErrorMessage = ErrorHelper.Error_WindowFunction_NullTreatment)]
		public void NthValueIgnoreNulls([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id       = t.Id,
					NthValue = Sql.Window.NthValue(t.IntValue, 2L, w => w.IgnoreNulls().PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		// FROM LAST for NTH_VALUE is supported by Oracle, DB2 and Firebird 3+. FROM FIRST is the default and is never gated.
		// DuckDB supports NTH_VALUE IGNORE NULLS but not FROM FIRST/LAST.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, TestProvName.AllSqlServer2012Plus, TestProvName.AllInformix, ErrorMessage = ErrorHelper.Error_WindowFunction_NthValue)]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllPostgreSQL, TestProvName.AllMySql8Plus, TestProvName.AllSQLite, TestProvName.AllClickHouse,
			TestProvName.AllSapHana, TestProvName.AllDuckDB,
			ProviderName.Ydb,
			ErrorMessage = ErrorHelper.Error_WindowFunction_NthValueFrom)]
		public void NthValueFromLast([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id       = t.Id,
					NthValue = Sql.Window.NthValue(t.IntValue, 2L, w => w.FromLast().PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}
	}
}
