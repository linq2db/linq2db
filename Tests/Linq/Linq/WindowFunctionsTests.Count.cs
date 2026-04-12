using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void CountNoArgs([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
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
		public void CountWithArg([DataSources(
			TestProvName.AllOracleNative,
			TestProvName.AllMySql57,
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
					CountArg = Sql.Window.Count(w => w.Argument(t.NullableIntValue).PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		public void CountWithFilter([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
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
		public void CountWithDefineWindow([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
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
	}
}
