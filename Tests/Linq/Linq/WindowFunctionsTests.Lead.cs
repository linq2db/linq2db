using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void LeadSimple([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id   = t.Id,
					Lead = Sql.Window.Lead(t.IntValue, w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		public void LeadWithOffset([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id   = t.Id,
					Lead = Sql.Window.Lead(t.IntValue, 2, w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		public void LeadWithOffsetAndDefault([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id   = t.Id,
					Lead = Sql.Window.Lead(t.IntValue, 2, 0, w => w.OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		public void LeadWithPartition([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				select new
				{
					Id   = t.Id,
					Lead = Sql.Window.Lead(t.IntValue, w => w.PartitionBy(t.CategoryId).OrderBy(t.Id)),
				};

				_ = query.ToList();
		}

		[Test]
		public void LeadWithDefineWindow([DataSources(TestProvName.AllOracleNative, TestProvName.AllMySql57)] string context)
		{
			var data = WindowFunctionTestEntity.Seed();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query =
				from t in table
				let wnd = Sql.Window.DefineWindow(w => w.PartitionBy(t.CategoryId).OrderBy(t.Id))
				select new
				{
					Id   = t.Id,
					Lead = Sql.Window.Lead(t.IntValue, w => w.UseWindow(wnd)),
				};

				_ = query.ToList();
		}
	}
}
