using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class SetOperatorOptimizationTests : TestBase
	{
		[Table]
		public class Item
		{
			[Column, PrimaryKey] public int  ItemId     { get; set; }
			[Column]             public int  AltItemId  { get; set; }
			[Column]             public int? ItemNumber { get; set; }
		}

		[Table]
		public class Thing
		{
			[Column, PrimaryKey] public int  Id         { get; set; }
			[Column]             public int? ItemId     { get; set; }
			[Column]             public int? ItemNumber { get; set; }
		}

		static readonly Item[] _itemData =
		[
			new() { ItemId = 1, AltItemId = 10, ItemNumber = null },
			new() { ItemId = 2, AltItemId = 15, ItemNumber = 640 },
			new() { ItemId = 3, AltItemId = 29, ItemNumber = 480 },
			new() { ItemId = 4, AltItemId = 42, ItemNumber = 800 },
			new() { ItemId = 5, AltItemId = 50, ItemNumber = 600 },
		];

		static readonly Thing[] _thingData =
		[
			new() { Id = 1, ItemId = 1  },
			new() { Id = 2, ItemId = 2  },
			new() { Id = 3, ItemId = 3  },
			new() { Id = 4, ItemId = 10 },
			new() { Id = 5, ItemId = 42 },
		];

		/// <summary>
		/// <see href="https://github.com/linq2db/linq2db/issues/5447"/>
		/// WHERE on Concat result fed into another Concat.
		/// Regression: WHERE was applied only to first UNION ALL operand during flattening.
		/// </summary>
		[Test]
		public void WhereOnConcatInsideConcat([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(_itemData);
			using var things = db.CreateLocalTable(_thingData);

			var byItemId =
				from t in things
				from itm in items
				where itm.ItemId == t.ItemId
				select itm;

			var byAltItemId =
				from t in things
				from itm in items
				where itm.AltItemId == t.ItemId
				select itm;

			var thingNumbers =
				from t in things
				where t.ItemNumber != null
				select t.ItemNumber;

			var query = byItemId
				.Concat(byAltItemId)
				.Where(s => s.ItemNumber != null)
				.Select(s => s.ItemNumber)
				.Concat(thingNumbers)
				.OrderBy(n => n);

			AssertQuery(query);
		}

		/// <summary>
		/// <see href="https://github.com/linq2db/linq2db/issues/5447"/>
		/// Distinct on Concat result fed into another Concat.
		/// Guards HasModifier check in OptimizeUnions / IsMovingUpValid.
		/// </summary>
		[Test]
		public void DistinctOnConcatInsideConcat([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(_itemData);
			using var things = db.CreateLocalTable(_thingData);

			var byItemId =
				from t in things
				from itm in items
				where itm.ItemId == t.ItemId
				select itm.ItemNumber;

			var byAltItemId =
				from t in things
				from itm in items
				where itm.AltItemId == t.ItemId
				select itm.ItemNumber;

			var thingNumbers =
				from t in things
				select t.ItemNumber;

			var query = byItemId
				.Concat(byAltItemId)
				.Distinct()
				.Concat(thingNumbers)
				.OrderBy(n => n);

			AssertQuery(query);
		}

		/// <summary>
		/// <see href="https://github.com/linq2db/linq2db/issues/5447"/>
		/// GroupBy on Concat result fed into another Concat.
		/// Guards HasGroupBy check in OptimizeUnions / IsMovingUpValid.
		/// </summary>
		[Test]
		public void GroupByOnConcatInsideConcat([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(_itemData);
			using var things = db.CreateLocalTable(_thingData);

			var byItemId =
				from t in things
				from itm in items
				where itm.ItemId == t.ItemId
				select itm;

			var byAltItemId =
				from t in things
				from itm in items
				where itm.AltItemId == t.ItemId
				select itm;

			var grouped = byItemId
				.Concat(byAltItemId)
				.GroupBy(s => s.ItemNumber)
				.Select(g => new { ItemNumber = g.Key, Count = g.Count() });

			var thingGrouped =
				from t in things
				group t by t.ItemNumber into g
				select new { ItemNumber = g.Key, Count = g.Count() };

			var query = grouped
				.Concat(thingGrouped)
				.OrderBy(r => r.ItemNumber)
				.ThenBy(r => r.Count);

			AssertQuery(query);
		}

		/// <summary>
		/// <see href="https://github.com/linq2db/linq2db/issues/5447"/>
		/// Where on Concat result without outer Concat — pure MoveSubQueryUp path.
		/// Ensures IsMovingUpValid guard works when parent has WHERE and subquery has set operators.
		/// </summary>
		[Test]
		public void WhereOnConcatStandalone([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(_itemData);
			using var things = db.CreateLocalTable(_thingData);

			var byItemId =
				from t in things
				from itm in items
				where itm.ItemId == t.ItemId
				select itm;

			var byAltItemId =
				from t in things
				from itm in items
				where itm.AltItemId == t.ItemId
				select itm;

			var query = byItemId
				.Concat(byAltItemId)
				.Where(s => s.ItemNumber != null)
				.Select(s => s.ItemNumber)
				.OrderBy(n => n);

			AssertQuery(query);
		}
	}
}
