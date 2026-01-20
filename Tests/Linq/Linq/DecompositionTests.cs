using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	public class DecompositionTests : TestBase
	{
		class Item
		{
			[PrimaryKey]
			public int ItemId { get; set; }

			public int Kind { get; set; }
			public string ItemCode { get; set; } = default!;
			public string Style { get; set; } = default!;
			public string Color { get; set; } = default!;

			public Item[] Seed()
			{
				return new[]
				{
					new Item { ItemId = 1, Kind = 1, ItemCode = "01020102", Style = "Style1", Color = "White" },
					new Item { ItemId = 2, Kind = 1, ItemCode = "01020102", Style = "Style1", Color = "White" },
					new Item { ItemId = 3, Kind = 1, ItemCode = "01020102", Style = "Style1", Color = "White" },
					new Item { ItemId = 4, Kind = 2, ItemCode = "03020302", Style = "Style3", Color = "White" },
					new Item { ItemId = 5, Kind = 2, ItemCode = "01040104", Style = "Style1", Color = "Blue"  },
					new Item { ItemId = 6, Kind = 2, ItemCode = "01010104", Style = "Style1", Color = "Black" },
					new Item { ItemId = 7, Kind = 3, ItemCode = "03020302", Style = "Style3", Color = "White" },
					new Item { ItemId = 8, Kind = 3, ItemCode = "01040104", Style = "Style1", Color = "Blue"  },
					new Item { ItemId = 9, Kind = 3, ItemCode = "01010104", Style = "Style1", Color = "Black" },
				};
			}
		}

		public class ItemInfo
		{
			public required ItemPart? Top { get; set; }
			public required ItemPart? Bottom { get; set; }
		}

		public class ItemPart
		{
			public required StyleInfo?  Color { get; set; } = default!;
			public required StyleInfo? Size  { get; set; } = default!;
		}

		public class StyleInfo
		{
			public required string ItemCode { get; set; } = default!;
			public required string Name { get; set; } = default!;
		}

		IQueryable<ItemInfo> DecomposeItems(IQueryable<Item> items)
		{
			return
				from t in items
				select new ItemInfo
				{
					Top = t.Kind == 1 || t.Kind == 2 ? new ItemPart
					{
						Color = t.Kind == 1 ? null : new StyleInfo { ItemCode = t.ItemCode.Substring(0, 2), Name = t.Color },
						Size = new StyleInfo { ItemCode = t.ItemCode.Substring(2, 2), Name = t.Style }
					} : null,
					Bottom = t.Kind == 1 || t.Kind == 3 ? new ItemPart
					{
						Color = new StyleInfo { ItemCode = t.ItemCode.Substring(4, 2), Name = t.Color },
						Size  = t.Kind == 1 ? null : new StyleInfo { ItemCode = t.ItemCode.Substring(6, 2), Name = t.Style }
					} : null
				};
		}

		[Test]
		public void ExtractValuesAndCombining([DataSources] string context)
		{
			using var db       = GetDataContext(context);
			using var disposal = db.CreateLocalTable(new Item().Seed());

			var items = DecomposeItems(db.GetTable<Item>());

			var tops =
				from i in items
				where i.Top != null
				select i.Top;

			AssertQuery(tops.Where(x => x.Color != null));

			var bottoms =
				from i in items
				where i.Bottom != null
				select i.Bottom;

			AssertQuery(bottoms.Where(x => x.Color != null));

			var flatItems = tops.Concat(bottoms);

			AssertQuery(flatItems);
			AssertQuery(flatItems.Where(x => x.Color != null));
			AssertQuery(flatItems.Where(x => x.Color == null));
			AssertQuery(flatItems.Where(x => x.Size != null));
			AssertQuery(flatItems.Where(x => x.Size == null));
		}

		[Test]
		public void ExtractValuesAndCombiningCoalesce([DataSources] string context)
		{
			using var db       = GetDataContext(context);
			using var disposal = db.CreateLocalTable(new Item().Seed());

			var items = DecomposeItems(db.GetTable<Item>());

			var topOrBottoms =
				from i in items
				let part = i.Top ?? i.Bottom
				select part;

			//AssertQuery(topOrBottoms);
			AssertQuery(topOrBottoms.Where(x => x.Color != null));
			/*AssertQuery(topOrBottoms.Where(x => x.Color == null));
			AssertQuery(topOrBottoms.Where(x => x.Size  != null));
			AssertQuery(topOrBottoms.Where(x => x.Size  == null));*/
		}

	}
}
