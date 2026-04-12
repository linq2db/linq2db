using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void NthValueBasic([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
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

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

		[Test]
		public void NthValueWithFrame([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
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

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

		[Test]
		public void NthValueWithDefineWindow([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
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

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}
	}
}
