using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void NTileWithMultiplePartitions([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					nt1    = Sql.Window.NTile(4, f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp)),
					nt2    = Sql.Window.NTile(4, f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Value)),
					nt3    = Sql.Window.NTile(4, f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp)),
					nt4    = Sql.Window.NTile(4, f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Value)),
					nt5    = Sql.Window.NTile(4, f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp).ThenBy(x.Value)),
					nt6    = Sql.Window.NTile(4, f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

		[Test]
		public void NTileWithMultiplePartitionsWithDefineWindow([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query =
				from x in table
				let wnd1 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId).OrderBy(x.Timestamp))
				let wnd2 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Value))
				let wnd3 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp))
				let wnd4 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Value))
				let wnd5 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp).ThenBy(x.Value))
				let wnd6 = Sql.Window.DefineWindow(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				select new
				{
					Entity = x,
					nt1   = Sql.Window.NTile(4, f => f.UseWindow(wnd1)),
					nt2   = Sql.Window.NTile(4, f => f.UseWindow(wnd2)),
					nt3   = Sql.Window.NTile(4, f => f.UseWindow(wnd3)),
					nt4   = Sql.Window.NTile(4, f => f.UseWindow(wnd4)),
					nt5   = Sql.Window.NTile(4, f => f.UseWindow(wnd5)),
					nt6   = Sql.Window.NTile(4, f => f.UseWindow(wnd6))
				}
				into s
				orderby s.Entity.Id
				select s;

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

		[Test]
		public void NTileWithNulls([IncludeDataSources(
			true,
			TestProvName.AllOracle12Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					nt7    = Sql.Window.NTile(4, f => f.PartitionBy(x.CategoryId).OrderBy(x.Timestamp, Sql.NullsPosition.First)),
					nt8    = Sql.Window.NTile(4, f => f.PartitionBy(x.CategoryId).OrderByDesc(x.Timestamp, Sql.NullsPosition.Last))
				})
				.OrderBy(x => x.Entity.Id);

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

		[Test]
		public void NTileWithoutPartition([IncludeDataSources(
			true,
			// native oracle provider crashes with AV
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			TestProvName.AllSqlServer2012Plus,
			TestProvName.AllClickHouse,
			TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					nt1    = Sql.Window.NTile(4, f => f.OrderBy(x.Timestamp)),
					nt2    = Sql.Window.NTile(4, f => f.OrderBy(x.Value)),
					nt3    = Sql.Window.NTile(4, f => f.OrderByDesc(x.Timestamp)),
					nt4    = Sql.Window.NTile(4, f => f.OrderByDesc(x.Value)),
					nt5    = Sql.Window.NTile(4, f => f.OrderBy(x.Timestamp).ThenBy(x.Value)),
					nt6    = Sql.Window.NTile(4, f => f.OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

			Assert.DoesNotThrow(() =>
			{
				_ = query.ToList();
			});
		}

	}
}
