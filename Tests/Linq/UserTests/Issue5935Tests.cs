using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5935Tests : TestBase
	{
		[Table]
		sealed class Item
		{
			[Column, PrimaryKey] public int     Id    { get; set; }
			[Column]             public string? Value { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ItemLog.ItemId))]
			public IEnumerable<ItemLog> Logs { get; set; } = null!;
		}

		[Table]
		sealed class ItemLog
		{
			[Column, PrimaryKey] public int     Id     { get; set; }
			[Column]             public int     ItemId { get; set; }
			[Column]             public string? Log    { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ItemTag.LogId))]
			public IEnumerable<ItemTag> Tags { get; set; } = null!;
		}

		[Table]
		sealed class ItemTag
		{
			[Column, PrimaryKey] public int     Id    { get; set; }
			[Column]             public int     LogId { get; set; }
			[Column]             public string? Name  { get; set; }
		}

		static readonly Item[] ItemData =
		[
			new Item { Id = 1, Value = "one" },
			new Item { Id = 2, Value = "two" },
		];

		// Declared out of Id order on purpose: insertion order must not be able to stand in for the
		// ordering under test, so a run that applies no ordering at all can come back wrong.
		static readonly ItemLog[] LogData =
		[
			new ItemLog { Id = 3, ItemId = 1, Log = "line3" },
			new ItemLog { Id = 1, ItemId = 1, Log = "line1" },
			new ItemLog { Id = 2, ItemId = 1, Log = "line2" },
			new ItemLog { Id = 6, ItemId = 2, Log = "line6" },
			new ItemLog { Id = 4, ItemId = 2, Log = "line4" },
			new ItemLog { Id = 5, ItemId = 2, Log = "line5" },
		];

		static readonly ItemTag[] TagData =
		[
			new ItemTag { Id = 1, LogId = 1, Name = "t1" },
			new ItemTag { Id = 2, LogId = 2, Name = "t2" },
			new ItemTag { Id = 3, LogId = 3, Name = "t3" },
			new ItemTag { Id = 4, LogId = 4, Name = "t4" },
			new ItemTag { Id = 5, LogId = 5, Name = "t5" },
			new ItemTag { Id = 6, LogId = 6, Name = "t6" },
		];

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5935 - an eager-loaded OrderBy whose key is not in the projection must not throw")]
		public void OrderByKeyNotProjected(
			[DataSources] string context,
			[Values(EagerLoadingStrategy.Default, EagerLoadingStrategy.KeyedQuery)] EagerLoadingStrategy strategy)
		{
			using var db    = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(strategy));
			using var items = db.CreateLocalTable(ItemData);
			using var logs  = db.CreateLocalTable(LogData);

			var result = items
				.OrderBy(i => i.Id)
				.Select(i => new
				{
					i.Value,
					Logs = i.Logs.OrderBy(l => l.Id).Select(l => new { l.Log }).ToArray(),
				})
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Logs.Select(l => l.Log).ShouldBe(["line1", "line2", "line3"]);
			result[1].Logs.Select(l => l.Log).ShouldBe(["line4", "line5", "line6"]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5935 - descending order survives the same shape")]
		public void OrderByDescendingKeyNotProjected(
			[DataSources] string context,
			[Values(EagerLoadingStrategy.Default, EagerLoadingStrategy.KeyedQuery)] EagerLoadingStrategy strategy)
		{
			using var db    = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(strategy));
			using var items = db.CreateLocalTable(ItemData);
			using var logs  = db.CreateLocalTable(LogData);

			var result = items
				.OrderBy(i => i.Id)
				.Select(i => new
				{
					i.Value,
					Logs = i.Logs.OrderByDescending(l => l.Id).Select(l => new { l.Log }).ToArray(),
				})
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Logs.Select(l => l.Log).ShouldBe(["line3", "line2", "line1"]);
			result[1].Logs.Select(l => l.Log).ShouldBe(["line6", "line5", "line4"]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5935 - ThenBy with neither key in the projection")]
		public void ThenByKeyNotProjected(
			[DataSources] string context,
			[Values(EagerLoadingStrategy.Default, EagerLoadingStrategy.KeyedQuery)] EagerLoadingStrategy strategy)
		{
			using var db    = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(strategy));
			using var items = db.CreateLocalTable(ItemData);
			using var logs  = db.CreateLocalTable(LogData);

			var result = items
				.OrderBy(i => i.Id)
				.Select(i => new
				{
					i.Value,
					Logs = i.Logs.OrderBy(l => l.ItemId).ThenByDescending(l => l.Id).Select(l => new { l.Log }).ToArray(),
				})
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Logs.Select(l => l.Log).ShouldBe(["line3", "line2", "line1"]);
			result[1].Logs.Select(l => l.Log).ShouldBe(["line6", "line5", "line4"]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5935 - the key-present shape keeps remapping and keeps its order")]
		public void OrderByKeyProjected(
			[DataSources] string context,
			[Values(EagerLoadingStrategy.Default, EagerLoadingStrategy.KeyedQuery)] EagerLoadingStrategy strategy)
		{
			using var db    = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(strategy));
			using var items = db.CreateLocalTable(ItemData);
			using var logs  = db.CreateLocalTable(LogData);

			var result = items
				.OrderBy(i => i.Id)
				.Select(i => new
				{
					i.Value,
					Logs = i.Logs.OrderByDescending(l => l.Id).Select(l => new { l.Id, l.Log }).ToArray(),
				})
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Logs.Select(l => l.Log).ShouldBe(["line3", "line2", "line1"]);
			result[1].Logs.Select(l => l.Log).ShouldBe(["line6", "line5", "line4"]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5935 - the key-present ThenBy shape is what still reaches the ThenBy branch after the fix")]
		public void ThenByKeyProjected(
			[DataSources] string context,
			[Values(EagerLoadingStrategy.Default, EagerLoadingStrategy.KeyedQuery)] EagerLoadingStrategy strategy)
		{
			using var db    = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(strategy));
			using var items = db.CreateLocalTable(ItemData);
			using var logs  = db.CreateLocalTable(LogData);

			var result = items
				.OrderBy(i => i.Id)
				.Select(i => new
				{
					i.Value,
					Logs = i.Logs.OrderBy(l => l.ItemId).ThenByDescending(l => l.Id).Select(l => new { l.ItemId, l.Id, l.Log }).ToArray(),
				})
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Logs.Select(l => l.Log).ShouldBe(["line3", "line2", "line1"]);
			result[1].Logs.Select(l => l.Log).ShouldBe(["line6", "line5", "line4"]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5935 - SelectMany below an OrderBy changes the element type without a remap")]
		public void OrderByThenSelectMany(
			[DataSources] string context,
			[Values(EagerLoadingStrategy.Default, EagerLoadingStrategy.KeyedQuery)] EagerLoadingStrategy strategy)
		{
			if (strategy == EagerLoadingStrategy.KeyedQuery && context.IsAnyOf(TestProvName.AllAccess))
				Assert.Ignore("KeyedQuery emits its key set as a FROM-less UNION ALL derived table, which Access rejects.");

			using var db    = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(strategy));
			using var items = db.CreateLocalTable(ItemData);
			using var logs  = db.CreateLocalTable(LogData);
			using var tags  = db.CreateLocalTable(TagData);

			var result = items
				.OrderBy(i => i.Id)
				.Select(i => new
				{
					i.Value,
					Tags = i.Logs.OrderBy(l => l.Id).SelectMany(l => l.Tags).ToArray(),
				})
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Tags.Select(t => t.Name).OrderBy(n => n).ShouldBe(["t1", "t2", "t3"]);
			result[1].Tags.Select(t => t.Name).OrderBy(n => n).ShouldBe(["t4", "t5", "t6"]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5935 - Cast below an OrderBy changes the element type without a remap")]
		public void OrderByThenCast(
			[DataSources] string context,
			[Values(EagerLoadingStrategy.Default, EagerLoadingStrategy.KeyedQuery)] EagerLoadingStrategy strategy)
		{
			if (strategy == EagerLoadingStrategy.KeyedQuery && context.IsAnyOf(TestProvName.AllAccess))
				Assert.Ignore("KeyedQuery emits its key set as a FROM-less UNION ALL derived table, which Access rejects.");

			using var db    = GetDataContext(context, o => o.UseDefaultEagerLoadingStrategy(strategy));
			using var items = db.CreateLocalTable(ItemData);
			using var logs  = db.CreateLocalTable(LogData);

			var result = items
				.OrderBy(i => i.Id)
				.Select(i => new
				{
					i.Value,
					Logs = i.Logs.OrderBy(l => l.Id).Cast<object>().ToArray(),
				})
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Logs.Length.ShouldBe(3);
			result[1].Logs.Length.ShouldBe(3);
		}

		// Default strategy only: KeyedQuery rewrites the parent-key equality into a Contains, which leaves
		// Take applied to the whole filtered set rather than per parent, so a row count means nothing there.
		// Order is deliberately unasserted - for a limited detail the ORDER BY stays inside the APPLY
		// subquery and never reaches the outer preamble query.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5935 - a limited detail hits the same defect")]
		public void OrderByTakeKeyNotProjected([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(ItemData);
			using var logs  = db.CreateLocalTable(LogData);

			var result = items
				.OrderBy(i => i.Id)
				.Select(i => new
				{
					i.Value,
					Logs = i.Logs.OrderBy(l => l.Id).Take(2).Select(l => new { l.Log }).ToArray(),
				})
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Logs.Length.ShouldBe(2);
			result[1].Logs.Length.ShouldBe(2);
		}
	}
}
