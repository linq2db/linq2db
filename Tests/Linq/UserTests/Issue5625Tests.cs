using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5625Tests : TestBase
	{
		[Table]
		sealed class Entity
		{
			[PrimaryKey] public int  EntityId   { get; set; }
			[Column]     public bool Applicable { get; set; }

			public static readonly Entity[] Data =
			{
				new() { EntityId = 1, Applicable = false },
				new() { EntityId = 2, Applicable = false },
				new() { EntityId = 3, Applicable = true  },
				new() { EntityId = 4, Applicable = true  },
			};
		}

		[Table]
		sealed class Item
		{
			[PrimaryKey] public int  ItemId     { get; set; }
			[Column]     public int  AltItemId  { get; set; }
			[Column]     public int? ItemNumber { get; set; }
			[Column]     public int  EntityId   { get; set; }

			public static readonly Item[] Data =
			{
				new() { ItemId = 1, AltItemId = 10, ItemNumber = null, EntityId = 1 },
				new() { ItemId = 2, AltItemId = 15, ItemNumber = 640,  EntityId = 2 },
				new() { ItemId = 3, AltItemId = 29, ItemNumber = 480,  EntityId = 3 },
				new() { ItemId = 4, AltItemId = 42, ItemNumber = 600,  EntityId = 4 },
				new() { ItemId = 5, AltItemId = 50, ItemNumber = 800,  EntityId = 1 },
			};
		}

		[Table]
		sealed class Thing
		{
			[PrimaryKey] public int  Id     { get; set; }
			[Column]     public int? ItemId { get; set; }

			public static readonly Thing[] Data =
			{
				new() { Id = 1, ItemId = 1  },
				new() { Id = 2, ItemId = 2  },
				new() { Id = 3, ItemId = 3  },
				new() { Id = 4, ItemId = 10 },
				new() { Id = 5, ItemId = 15 },
				new() { Id = 6, ItemId = 42 },
			};
		}

		// Issue #5625: a `.Concat(...)` whose result is then `.Join(...)`-ed to a table used
		// only for filtering (the join projects the left side and discards the joined row)
		// threw `LinqToDBException: Table not found for '...EntityId'` while building SQL.
		// The set-operation subquery was incorrectly folded into the parent, leaving the
		// filtering join attached to a single union branch with a dangling column reference.
		[Test]
		public void ConcatThenFilteringJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db       = GetDataContext(context);
			using var things   = db.CreateLocalTable(Thing.Data);
			using var items    = db.CreateLocalTable(Item.Data);
			using var entities = db.CreateLocalTable(Entity.Data);

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

			var query =
				from itm in byItemId.Concat(byAltItemId)
				join entity in entities.Where(entity => entity.Applicable)
					on itm.EntityId equals entity.EntityId
				select itm.ItemId;

			AssertQuery(query);

			query.ToList().ShouldBe(new[] { 3, 4 }, ignoreOrder: true);
		}

		// Same root cause, reached via a plain inner join with no WHERE on the joined table
		// (so it isn't blocked as a set-operation barrier). Also threw before the fix.
		[Test]
		public void ConcatThenPlainInnerJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db       = GetDataContext(context);
			using var things   = db.CreateLocalTable(Thing.Data);
			using var items    = db.CreateLocalTable(Item.Data);
			using var entities = db.CreateLocalTable(Entity.Data);

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

			var query =
				from itm in byItemId.Concat(byAltItemId)
				join entity in entities on itm.EntityId equals entity.EntityId
				select itm.ItemId;

			AssertQuery(query);

			query.ToList().ShouldBe(new[] { 1, 1, 2, 2, 3, 4 }, ignoreOrder: true);
		}

		// Two joins on top of the Concat — also threw before the fix.
		[Test]
		public void ConcatThenTwoJoins([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db       = GetDataContext(context);
			using var things   = db.CreateLocalTable(Thing.Data);
			using var items    = db.CreateLocalTable(Item.Data);
			using var entities = db.CreateLocalTable(Entity.Data);

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

			var query =
				from itm in byItemId.Concat(byAltItemId)
				join applicable in entities.Where(entity => entity.Applicable)
					on itm.EntityId equals applicable.EntityId
				join other in entities
					on itm.EntityId equals other.EntityId
				select itm.ItemId;

			AssertQuery(query);

			query.ToList().ShouldBe(new[] { 3, 4 }, ignoreOrder: true);
		}
	}
}
