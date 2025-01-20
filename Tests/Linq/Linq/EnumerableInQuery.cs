using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class EnumerableInQuery : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void SelectQueryFromList([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var items = new[]
			{
				new SampleClass { Id = 1, Value = 11 }, new SampleClass { Id = 2, Value = 22 },
				new SampleClass { Id = 3, Value = 33 }, new SampleClass { Id = 4, Value = 44 }
			};
			using (var db = GetDataContext(context))
			{

				IQueryable<SampleClass>? itemsQuery = null;

				for (int i = 0; i < items.Length; i++)
				{
					var item = items[i];
					var current = i % 2 == 0
						? db.SelectQuery(() => new SampleClass
						{
							Id = item.Id,
							Value = item.Value,
						})
						: db.SelectQuery(() => new SampleClass
						{
							Value = item.Value,
							Id = item.Id,
						});

					itemsQuery = itemsQuery == null ? current : itemsQuery.Concat(current);
				}

				var result = itemsQuery!.AsCte().ToArray();

				AreEqual(items, result, ComparerBuilder.GetEqualityComparer<SampleClass>());
			}
		}

		[Test]
		public void FieldProjection([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var collection = db.Person.ToList();
				var query = db.Person
					.Select(x => collection.First(r => r.ID == x.ID).ID);

				AssertQuery(query);
			}
		}

		[Test]
		public void AnonymousProjection([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var collection = db.Parent.Select(_ => new { _.ParentID }).ToList();

				var query = db.Parent
					.Select(_ => new
					{
						Children = collection.Where(c1 => c1.ParentID == _.ParentID).ToArray()
					});

				AssertQuery(query);
			}
		}

		[Test]
		public void EnumerableAsQueryable([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var resultQuery = Array.Empty<Model.Person>().AsQueryable();

			var query = db.GetTable<Model.Person>()
				.Where(_ => !resultQuery.Select(m => m.ID).Contains(_.ID));

			AssertQuery(query);
		}

		[Table]
		sealed class SomeItem
		{
			[Column] public int Id { get; set; }

			[Column] public int? ColorId { get; set; }

			[Association(ThisKey = nameof(ColorId), OtherKey = nameof(SomeColor.Id))]
			public SomeColor? Color { get; set; }

			[Column] public int? StyleId { get; set; }

			[Association(ThisKey = nameof(StyleId), OtherKey = nameof(SomeStyle.Id))]
			public SomeStyle? Style { get; set; }

			public static (SomeItem[], SomeColor[], SomeStyle[]) Seed()
			{
				var colors = new[]
				{
					new SomeColor { Id = 1, Name = "Red" },
					new SomeColor { Id = 2, Name = "Green" },
					new SomeColor { Id = 3, Name = "Blue" }
				};

				var styles = new[]
				{
					new SomeStyle { Id = 1, Name = "Bold" },
					new SomeStyle { Id = 2, Name = "Italic" },
					new SomeStyle { Id = 3, Name = "Underline" }
				};

				var items = new[]
				{
					new SomeItem { Id = 1, ColorId = 1, StyleId    = 1 },
					new SomeItem { Id = 2, ColorId = 2, StyleId    = 2 },
					new SomeItem { Id = 3, ColorId = 3, StyleId    = 3 },
					new SomeItem { Id = 4, ColorId = 1, StyleId    = 2 },
					new SomeItem { Id = 5, ColorId = 2, StyleId    = 3 },
					new SomeItem { Id = 6, ColorId = null, StyleId = 1 }, // No color
					new SomeItem { Id = 7, ColorId = 3, StyleId    = null }, // No style
					new SomeItem { Id = 8, ColorId = null, StyleId = null } // No color and no style
				};

				return (items, colors, styles);
			}
		}

		[Table]
		sealed class SomeColor
		{
			[Column] public int    Id   { get; set; }
			[Column] public string Name { get; set; } = null!;
		}

		[Table]
		sealed class SomeStyle
		{
			[Column] public int    Id   { get; set; }
			[Column] public string Name { get; set; } = null!;
		}

		[Test]
		public void WithAssociations([IncludeDataSources(
			TestProvName.AllFirebird4Plus,
			TestProvName.AllMySql80,
			TestProvName.AllOracle12Plus,
			TestProvName.AllPostgreSQL93Plus,
			ProviderName.SqlCe,
			TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var (items, colors, styles) = SomeItem.Seed();

			using var itemsTable = db.CreateLocalTable(items);
			using var colorsTable = db.CreateLocalTable(colors);
			using var stylesTable = db.CreateLocalTable(styles);

			var query =
				from item in itemsTable.LoadWith(it => it.Color).LoadWith(it => it.Style)
				from it in new[]
				{
					new {ColorName = (string?)(item.Color!.Name), StyleName = item.Style!.Name },
					new {ColorName = (string?)null, StyleName               = item.Style!.Name },
				}.DefaultIfEmpty()
				where it.ColorName == "Red"
				select it;

			AssertQuery(query);
		}
	}
}
