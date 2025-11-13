using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void MinOverloads([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let w = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					IntSum             = Sql.Window.Min(t.IntValue,             w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableIntSum     = Sql.Window.Min(t.NullableIntValue,     w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					LongSum            = Sql.Window.Min(t.LongValue,            w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableLongSum    = Sql.Window.Min(t.NullableLongValue,    w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					DoubleSum          = Sql.Window.Min(t.DoubleValue,          w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableDoubleSum  = Sql.Window.Min(t.NullableDoubleValue,  w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					DecimalSum         = Sql.Window.Min(t.DecimalValue,         w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableDecimalSum = Sql.Window.Min(t.NullableDecimalValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					FloatSum           = Sql.Window.Min(t.FloatValue,           w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableFloatSum   = Sql.Window.Min(t.NullableFloatValue,   w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					ShortSum           = Sql.Window.Min(t.ShortValue,           w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableShortSum   = Sql.Window.Min(t.NullableShortValue,   w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					ByteSum            = Sql.Window.Min(t.ByteValue,            w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					NullableByteSum    = Sql.Window.Min(t.NullableByteValue,    w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				};

			Assert.DoesNotThrow(() =>
			{
				query.ToList();
			});
		}

		[Test]
		public void MinOverloadsViaWindow([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					IntSum             = Sql.Window.Min(t.IntValue,             w => w.UseWindow(wnd)),
					NullableIntSum     = Sql.Window.Min(t.NullableIntValue,     w => w.UseWindow(wnd)),
					LongSum            = Sql.Window.Min(t.LongValue,            w => w.UseWindow(wnd)),
					NullableLongSum    = Sql.Window.Min(t.NullableLongValue,    w => w.UseWindow(wnd)),
					DoubleSum          = Sql.Window.Min(t.DoubleValue,          w => w.UseWindow(wnd)),
					NullableDoubleSum  = Sql.Window.Min(t.NullableDoubleValue,  w => w.UseWindow(wnd)),
					DecimalSum         = Sql.Window.Min(t.DecimalValue,         w => w.UseWindow(wnd)),
					NullableDecimalSum = Sql.Window.Min(t.NullableDecimalValue, w => w.UseWindow(wnd)),
					FloatSum           = Sql.Window.Min(t.FloatValue,           w => w.UseWindow(wnd)),
					NullableFloatSum   = Sql.Window.Min(t.NullableFloatValue,   w => w.UseWindow(wnd)),
					ShortSum           = Sql.Window.Min(t.ShortValue,           w => w.UseWindow(wnd)),
					NullableShortSum   = Sql.Window.Min(t.NullableShortValue,   w => w.UseWindow(wnd)),
					ByteSum            = Sql.Window.Min(t.ByteValue,            w => w.UseWindow(wnd)),
					NullableByteSum    = Sql.Window.Min(t.NullableByteValue,    w => w.UseWindow(wnd))
				};

			Assert.DoesNotThrow(() =>
			{
				query.ToList();
			});
		}

	}

}
