using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class WindowFunctionsTests
	{
		[Test]
		public void RowNumberWithMultiplePartitions([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp)),
					rn2    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Value)),
					rn3    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp)),
					rn4    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Value)),
					rn5    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderBy(x.Timestamp).ThenBy(x.Value)),
					rn6    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId, x.Name).OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		[Test]
		public void RowNumberWithMultiplePartitionsWithDefineWindow([SupportsAnalyticFunctionsContext] string context)
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
					rn1   = Sql.Window.RowNumber(f => f.UseWindow(wnd1)),
					rn2   = Sql.Window.RowNumber(f => f.UseWindow(wnd2)),
					rn3   = Sql.Window.RowNumber(f => f.UseWindow(wnd3)),
					rn4   = Sql.Window.RowNumber(f => f.UseWindow(wnd4)),
					rn5   = Sql.Window.RowNumber(f => f.UseWindow(wnd5)),
					rn6   = Sql.Window.RowNumber(f => f.UseWindow(wnd6))
				}
				into s
				orderby s.Entity.Id
				select s;

				_ = query.ToList();
		}

		[Test]
		//TODO: we can emulate it for other providers by using additional order by with CASE:
		//ROW_NUMBER() OVER (ORDER BY CASE WHEN x.Value IS NULL THEN 1 ELSE 0 END, x.Value)
		public void RowNumberWithNulls([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn7    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId).OrderBy(x.Timestamp, Sql.NullsPosition.First)),
					rn8    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId).OrderByDesc(x.Timestamp, Sql.NullsPosition.Last))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		[Test]
		public void RowNumberWithoutPartition([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.RowNumber(f => f.OrderBy(x.Timestamp)),
					rn2    = Sql.Window.RowNumber(f => f.OrderBy(x.Value)),
					rn3    = Sql.Window.RowNumber(f => f.OrderByDesc(x.Timestamp)),
					rn4    = Sql.Window.RowNumber(f => f.OrderByDesc(x.Value)),
					rn5    = Sql.Window.RowNumber(f => f.OrderBy(x.Timestamp).ThenBy(x.Value)),
					rn6    = Sql.Window.RowNumber(f => f.OrderByDesc(x.Timestamp).ThenByDesc(x.Value))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		// ORDER BY and PARTITION BY are value positions: a boolean expression has to be folded into a value, since
		// providers without a native boolean type reject a bare predicate there.
		[Test]
		public void RowNumberWithBoolean([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var query = table
				.Select(x => new
				{
					Entity = x,
					rn1    = Sql.Window.RowNumber(f => f.OrderBy(x.IntValue == 20).ThenBy(x.Id)),
					rn2    = Sql.Window.RowNumber(f => f.PartitionBy(x.IntValue == 20).OrderBy(x.Id)),
					rn3    = Sql.Window.RowNumber(f => f.PartitionBy(x.NullableIntValue != null).OrderBy(x.Id)),
					rn4    = Sql.Window.RowNumber(f => f.PartitionBy(x.CategoryId).OrderBy(x.NullableIntValue != null).ThenBy(x.Id))
				})
				.OrderBy(x => x.Entity.Id);

				_ = query.ToList();
		}

		// Value assertion for the boolean fold: valid SQL is not enough, since an inverted or constant-folded
		// flag would still execute. Exactly one seeded row has IntValue == 20 (Id 2), so both the ordering and
		// the partitioning below are fully determined.
		[Test]
		public void RowNumberWithBooleanComputedValues([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var result = table
				.Select(x => new
				{
					x.Id,
					// ORDER BY false-then-true: the single matching row sorts last, so it gets the highest number.
					ByOrder     = Sql.Window.RowNumber(f => f.OrderBy(x.IntValue == 20).ThenBy(x.Id)),
					// PARTITION BY splits 8 non-matching rows from the 1 matching row, which restarts at 1.
					ByPartition = Sql.Window.RowNumber(f => f.PartitionBy(x.IntValue == 20).OrderBy(x.Id)),
				})
				.OrderBy(x => x.Id)
				.ToList();

			var byId = result.ToDictionary(r => r.Id);

			byId[1].ByOrder.ShouldBe(1);
			byId[9].ByOrder.ShouldBe(8);
			byId[2].ByOrder.ShouldBe(9); // inverted folding would make this 1, a constant one would make it 2

			byId[1].ByPartition.ShouldBe(1);
			byId[9].ByPartition.ShouldBe(8);
			byId[2].ByPartition.ShouldBe(1); // a dropped/constant partition key would make this 2
		}

		// A boolean column in ORDER BY / PARTITION BY is a storable value and is left unfolded; the equivalent
		// predicate is folded. Both must produce identical numbering.
		[Test]
		public void RowNumberWithBooleanColumn([SupportsAnalyticFunctionsContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(WindowFunctionTestEntity.Seed());

			var result = table
				.Select(x => new
				{
					x.Id,
					OrderColumn        = Sql.Window.RowNumber(f => f.OrderBy(x.BoolValue).ThenBy(x.Id)),
					OrderPredicate     = Sql.Window.RowNumber(f => f.OrderBy(x.IntValue % 20 == 0).ThenBy(x.Id)),
					PartitionColumn    = Sql.Window.RowNumber(f => f.PartitionBy(x.BoolValue).OrderBy(x.Id)),
					PartitionPredicate = Sql.Window.RowNumber(f => f.PartitionBy(x.IntValue % 20 == 0).OrderBy(x.Id)),
					PartitionNullable  = Sql.Window.RowNumber(f => f.PartitionBy(x.NullableBoolValue).OrderBy(x.Id)),
				})
				.OrderBy(x => x.Id)
				.ToList();

			result.ShouldAllBe(r => r.OrderColumn     == r.OrderPredicate);
			result.ShouldAllBe(r => r.PartitionColumn == r.PartitionPredicate);

			// Id 9 is the only row whose NullableBoolValue is NULL, so it is alone in its partition.
			result.Single(r => r.Id == 9).PartitionNullable.ShouldBe(1);
		}

		// Value assertion (not just SQL shape): ROW_NUMBER and SUM OVER (PARTITION BY ...) over deterministic
		// integer data must compute the expected per-partition rank and total on every supported provider.
		[Test]
		public void RowNumberAndSumComputedValues([SupportsAnalyticFunctionsContext] string context)
		{
			var data = new[]
			{
				new WindowFunctionTestEntity { Id = 1, CategoryId = 1, IntValue = 10 },
				new WindowFunctionTestEntity { Id = 2, CategoryId = 1, IntValue = 20 },
				new WindowFunctionTestEntity { Id = 3, CategoryId = 2, IntValue = 30 },
				new WindowFunctionTestEntity { Id = 4, CategoryId = 2, IntValue = 40 },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var result =
				(from t in table
				 select new
				 {
					 t.Id,
					 RN  = Sql.Window.RowNumber(f => f.PartitionBy(t.CategoryId).OrderBy(t.Id)),
					 Sum = Sql.Window.Sum(t.IntValue, f => f.PartitionBy(t.CategoryId)),
				 })
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Select(r => r.RN).ShouldBe(new long[] { 1, 2, 1, 2 });
			result.Select(r => r.Sum).ShouldBe(new[]      { 30, 30, 70, 70 });
		}
	}
}
