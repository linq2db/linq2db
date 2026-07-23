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
		public void DenseRankWithMultiplePartitions([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.DenseRank(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp)),
					rn2    = Sql.Window.DenseRank(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Value)),
					rn3    = Sql.Window.DenseRank(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp)),
					rn4    = Sql.Window.DenseRank(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Value)),
					rn5    = Sql.Window.DenseRank(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp).ThenBy(x.Value)),
					rn6    = Sql.Window.DenseRank(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		[Test]
		public void DenseRankWithMultiplePartitionsWithDefineWindow([SupportsAnalyticFunctionsContext] string context)
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
					rn1   = Sql.Window.DenseRank(f => f.UseWindow(wnd1)),
					rn2   = Sql.Window.DenseRank(f => f.UseWindow(wnd2)),
					rn3   = Sql.Window.DenseRank(f => f.UseWindow(wnd3)),
					rn4   = Sql.Window.DenseRank(f => f.UseWindow(wnd4)),
					rn5   = Sql.Window.DenseRank(f => f.UseWindow(wnd5)),
					rn6   = Sql.Window.DenseRank(f => f.UseWindow(wnd6))
				}
				into s
				orderby s.Entity.Id
				select s;

				_ = query.ToList();
		}

		[Test]
		public void DenseRankWithNulls([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn7    = Sql.Window.DenseRank(f => f.PartitionBy(x.CategoryId).OrderBy(x.Timestamp, Sql.NullsPosition.First)),
					rn8    = Sql.Window.DenseRank(f => f.PartitionBy(x.CategoryId).OrderByDesc(x.Timestamp, Sql.NullsPosition.Last))
				})
				.OrderBy(x => x.Entity.Id);

			var result = query.ToList();

			// Timestamp is nullable and row Id=9 (CategoryId=1) has a NULL Timestamp, so the requested NULLS
			// position is observable: rn7 places NULLS FIRST (ASC), rn8 places NULLS LAST (DESC). Timestamps
			// are distinct within each CategoryId, so DENSE_RANK has no ties. A provider whose NULLS emulation
			// (or native NULLS ordering) is wrong produces a different rank for the NULL row and fails here.
			var byId = result.ToDictionary(r => r.Entity.Id);

			(int Id, long Rn7, long Rn8)[] expected =
			[
				(1, 2, 4), (2, 3, 3), (3, 1, 2), (4, 2, 1), (5, 4, 2),
				(6, 1, 2), (7, 2, 1), (8, 5, 1), (9, 1, 5)
			];

			foreach (var (id, rn7, rn8) in expected)
			{
				byId[id].rn7.ShouldBe(rn7, $"rn7 (ASC NULLS FIRST) mismatch for Id={id}");
				byId[id].rn8.ShouldBe(rn8, $"rn8 (DESC NULLS LAST) mismatch for Id={id}");
			}
		}

		[Test]
		public void DenseRankWithoutPartition([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.DenseRank(f => f.OrderBy(x.Timestamp)),
					rn2    = Sql.Window.DenseRank(f => f.OrderBy(x.Value)),
					rn3    = Sql.Window.DenseRank(f => f.OrderByDesc(x.Timestamp)),
					rn4    = Sql.Window.DenseRank(f => f.OrderByDesc(x.Value)),
					rn5    = Sql.Window.DenseRank(f => f.OrderBy(x.Timestamp).ThenBy(x.Value)),
					rn6    = Sql.Window.DenseRank(f => f.OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}
	}
}
