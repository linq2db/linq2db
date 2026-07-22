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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		public void CountNoArgs([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					CountAll            = Sql.Window.Count(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					CountAllNoPartition = Sql.Window.Count(w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		public void CountWithArg([SupportsAnalyticFunctionsContext(
			ProviderName.Firebird3,
			// ClickHouse has DB bug with COUNT(nullable_column) in window context
			TestProvName.AllClickHouse)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					CountArg = Sql.Window.Count(t.NullableIntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		// An argument is a value position: a boolean expression has to be folded into a value, since providers
		// without a native boolean type reject a bare predicate there.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		public void CountWithBooleanArg([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var result =
				(from t in table
				select new
				{
					Id            = t.Id,
					CountBoolean  = Sql.Window.Count(t.IntValue == 20, w => w.PartitionBy(t.CategoryId)),
					CountNotNull  = Sql.Window.Count(t.IntValue,       w => w.PartitionBy(t.CategoryId)),
				})
				.OrderBy(t => t.Id)
				.ToList();

			// COUNT(<expr>) counts non-NULL values; neither expression can be NULL, so both count the whole partition.
			result.ShouldAllBe(r => r.CountBoolean == r.CountNotNull);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		public void CountWithFilter([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					CountFiltered = Sql.Window.Count(w => w.Filter(t.IntValue > 20).PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		public void CountWithDefineWindow([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					CountAll = Sql.Window.Count(w => w.UseWindow(wnd)),
				};

				_ = query.ToList();
		}

		// DISTINCT in a window aggregate is supported by Oracle, ClickHouse and DuckDB; on the providers below it is
		// rejected and gated with a descriptive error.
		// The argument modifier (DISTINCT) has to survive the boolean fold: the folded value replaces the
		// argument expression, not the SqlFunctionArgument that carries the modifier.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySql8Plus, TestProvName.AllSQLite,
			TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllDB2, TestProvName.AllInformix, ProviderName.Ydb,
			ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateDistinct)]
		public void CountDistinctWithBooleanArg([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var result =
				(from t in table
				select new
				{
					Id    = t.Id,
					Count = Sql.Window.Count(t.IntValue == 20, w => w.Distinct().PartitionBy(t.CategoryId)),
				})
				.OrderBy(t => t.Id)
				.ToList();

			// COUNT(DISTINCT <flag>) per category: category 1 holds both flag values, the others only one.
			result.ShouldAllBe(r => r.Count == (r.Id == 1 || r.Id == 2 || r.Id == 5 || r.Id == 8 || r.Id == 9 ? 2 : 1));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySql8Plus, TestProvName.AllSQLite,
			TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllDB2, TestProvName.AllInformix, ProviderName.Ydb,
			ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateDistinct)]
		public void CountDistinct([SupportsAnalyticFunctionsContext] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id    = t.Id,
					Count = Sql.Window.Count(t.IntValue, w => w.Distinct().PartitionBy(t.CategoryId)),
				};

				_ = query.ToList();
		}
	}
}
