using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		public void SumOverloads([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let w = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					IntSum             = Sql.Window.Sum(t.IntValue,             w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableIntSum     = Sql.Window.Sum(t.NullableIntValue,     w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					LongSum            = Sql.Window.Sum(t.LongValue,            w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableLongSum    = Sql.Window.Sum(t.NullableLongValue,    w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					DoubleSum          = Sql.Window.Sum(t.DoubleValue,          w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableDoubleSum  = Sql.Window.Sum(t.NullableDoubleValue,  w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					DecimalSum         = Sql.Window.Sum(t.DecimalValue,         w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableDecimalSum = Sql.Window.Sum(t.NullableDecimalValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					FloatSum           = Sql.Window.Sum(t.FloatValue,           w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableFloatSum   = Sql.Window.Sum(t.NullableFloatValue,   w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					ShortSum           = Sql.Window.Sum(t.ShortValue,           w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableShortSum   = Sql.Window.Sum(t.NullableShortValue,   w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					ByteSum            = Sql.Window.Sum(t.ByteValue,            w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableByteSum    = Sql.Window.Sum(t.NullableByteValue,    w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				};

				_ = query.ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		public void SumOverloadsViaWindow([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					IntSum             = Sql.Window.Sum(t.IntValue,             w => w.UseWindow(wnd)),
					NullableIntSum     = Sql.Window.Sum(t.NullableIntValue,     w => w.UseWindow(wnd)),
					LongSum            = Sql.Window.Sum(t.LongValue,            w => w.UseWindow(wnd)),
					NullableLongSum    = Sql.Window.Sum(t.NullableLongValue,    w => w.UseWindow(wnd)),
					DoubleSum          = Sql.Window.Sum(t.DoubleValue,          w => w.UseWindow(wnd)),
					NullableDoubleSum  = Sql.Window.Sum(t.NullableDoubleValue,  w => w.UseWindow(wnd)),
					DecimalSum         = Sql.Window.Sum(t.DecimalValue,         w => w.UseWindow(wnd)),
					NullableDecimalSum = Sql.Window.Sum(t.NullableDecimalValue, w => w.UseWindow(wnd)),
					FloatSum           = Sql.Window.Sum(t.FloatValue,           w => w.UseWindow(wnd)),
					NullableFloatSum   = Sql.Window.Sum(t.NullableFloatValue,   w => w.UseWindow(wnd)),
					ShortSum           = Sql.Window.Sum(t.ShortValue,           w => w.UseWindow(wnd)),
					NullableShortSum   = Sql.Window.Sum(t.NullableShortValue,   w => w.UseWindow(wnd)),
					ByteSum            = Sql.Window.Sum(t.ByteValue,            w => w.UseWindow(wnd)),
					NullableByteSum    = Sql.Window.Sum(t.NullableByteValue,    w => w.UseWindow(wnd))
				};

				_ = query.ToList();
		}

		// DISTINCT in a window aggregate is unsupported by the target databases — gated with a descriptive error.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql57, TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllFirebirdLess3, ErrorMessage = ErrorHelper.Error_WindowFunction_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSqlServer2008Minus, ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateWindowFunctions)]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSqlServer2012Plus, TestProvName.AllPostgreSQL, TestProvName.AllMySql8Plus, TestProvName.AllSQLite,
			TestProvName.AllClickHouse, TestProvName.AllFirebird3Plus, TestProvName.AllSapHana, TestProvName.AllOracle,
			TestProvName.AllDB2, TestProvName.AllInformix, TestProvName.AllDuckDB,
			ErrorMessage = ErrorHelper.Error_WindowFunction_AggregateDistinct)]
		public void SumDistinct([DataSources] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id  = t.Id,
					Sum = Sql.Window.Sum(t.IntValue, w => w.Distinct().PartitionBy(t.CategoryId)),
				};

				_ = query.ToList();
		}

	}
}

