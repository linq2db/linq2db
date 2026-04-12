using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void FirstValueBasic([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
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

		[Test]
		public void FirstValueWithFrame([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
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
		public void FirstValueWithDefineWindow([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
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
	}
}
