using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2506Tests : TestBase
	{
		[Table(Schema = "dbo", Name = "Item")]
		public partial class Item
		{
			[PrimaryKey, NotNull] public int Id { get; set; } // int
			[Column, NotNull] public string Name { get; set; } = null!; // nvarchar(50)

			#region Associations

			/// <summary>
			/// FK_ItemText_Item_BackReference
			/// </summary>
			[Association(ThisKey = "Id", OtherKey = "ItemId", CanBeNull = true)]
			public IEnumerable<ItemText> ItemTexts { get; set; } = null!;

			#endregion
		}

		[Table(Schema = "dbo", Name = "ItemText")]
		public partial class ItemText
		{
			[PrimaryKey(1), NotNull] public int ItemId { get; set; } // int
			[PrimaryKey(2), NotNull] public string Lang { get; set; } = null!; // varchar(2)
			[Column, NotNull] public string Text { get; set; } = null!; // nvarchar(50)

			#region Associations

			/// <summary>
			/// FK_ItemText_Item
			/// </summary>
			[Association(ThisKey = "ItemId", OtherKey = "Id", CanBeNull = false)]
			public Item Item { get; set; } = null!;

			#endregion
		}

		[Test]
		public void ParameterizedEagerLoad([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values("en", "de")] string lang)
		{
			var items = new Item[]
			{
				new Item { Id = 1, Name = "Item 1" },
				new Item { Id = 2, Name = "Item 2" }
			};

			var itemTexts = new ItemText[]
			{
				new ItemText{ ItemId = 1, Lang = "de", Text = "Item 1 german text"},
				new ItemText{ ItemId = 1, Lang = "en", Text = "Item 1 english text"},
				new ItemText{ ItemId = 2, Lang = "de", Text = "Item 2 german text"},
				new ItemText{ ItemId = 2, Lang = "en", Text = "Item 2 english text"},
			};

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(items))
			using (db.CreateLocalTable(itemTexts))
			{
				var query = db.GetTable<Item>().Select(item => new
				{
					item.Name,
					Texts = item.ItemTexts.Where(x => x.Lang == lang).Select(itemText => new
					{
						itemText.Lang,
						itemText.Text
					}).ToArray()
				}).ToArray();

				var expected = items.Select(item => new
				{
					item.Name,
					Texts = itemTexts.Where(x => x.ItemId == item.Id && x.Lang == lang).Select(itemText => new
					{
						itemText.Lang,
						itemText.Text
					}).ToArray()
				}).ToArray();

				AreEqualWithComparer(expected, query);
			}
		}
	}
}
