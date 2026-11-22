using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class WindowFunctionsTests
	{
		[Test]
		public void RankWithMultiplePartitions([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp)),
					rn2    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Value)),
					rn3    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp)),
					rn4    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Value)),
					rn5    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp).ThenBy(x.Value)),
					rn6    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		[Test]
		public void RankWithMultiplePartitionsWithDefineWindow([SupportsAnalyticFunctionsContext] string context)
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
					rn1   = Sql.Window.Rank(f => f.UseWindow(wnd1)),
					rn2   = Sql.Window.Rank(f => f.UseWindow(wnd2)),
					rn3   = Sql.Window.Rank(f => f.UseWindow(wnd3)),
					rn4   = Sql.Window.Rank(f => f.UseWindow(wnd4)),
					rn5   = Sql.Window.Rank(f => f.UseWindow(wnd5)),
					rn6   = Sql.Window.Rank(f => f.UseWindow(wnd6))
				}
				into s
				orderby s.Entity.Id
				select s;

				_ = query.ToList();
		}

		// ORDER BY and PARTITION BY are value positions: a boolean expression has to be folded into a value, since
		// providers without a native boolean type reject a bare predicate there.
		[Test]
		public void RankWithBooleanOrderBy([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.Rank(f => f.OrderBy(x.IntValue == 20)),
					rn2    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId).OrderBy(x.IntValue == 20).ThenBy(x.Id)),
					rn3    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId).OrderByDesc(x.IntValue == 20).ThenBy(x.Id)),
					rn4    = Sql.Window.Rank(f => f.OrderBy(x.NullableIntValue != null).ThenBy(x.Id))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		[Test]
		public void RankWithBooleanPartition([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.Rank(f => f.PartitionBy(x.IntValue == 20).OrderBy(x.Id)),
					rn2    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId, x.IntValue == 20).OrderBy(x.Id)),
					rn3    = Sql.Window.Rank(f => f.PartitionBy(x.NullableIntValue != null).OrderBy(x.Id))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		[Test]
		public void RankWithNulls([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn7    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId).OrderBy(x.Timestamp, Sql.NullsPosition.First)),
					rn8    = Sql.Window.Rank(f => f.PartitionBy(x.CategoryId).OrderByDesc(x.Timestamp, Sql.NullsPosition.Last))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		[Test]
		public void RankWithoutPartition([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.Rank(f => f.OrderBy(x.Timestamp)),
					rn2    = Sql.Window.Rank(f => f.OrderBy(x.Value)),
					rn3    = Sql.Window.Rank(f => f.OrderByDesc(x.Timestamp)),
					rn4    = Sql.Window.Rank(f => f.OrderByDesc(x.Value)),
					rn5    = Sql.Window.Rank(f => f.OrderBy(x.Timestamp).ThenBy(x.Value)),
					rn6    = Sql.Window.Rank(f => f.OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}
	}
}

