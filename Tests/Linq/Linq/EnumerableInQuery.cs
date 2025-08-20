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

		[Test]
		public void WithSubqueries([IncludeDataSources(
			TestProvName.AllFirebird4Plus,
			TestProvName.AllMySql80,
			TestProvName.AllOracle12Plus,
			TestProvName.AllPostgreSQL93Plus,
			ProviderName.SqlCe,
			TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var (items, colors, styles) = SomeItem.Seed();

			using var itemsTable  = db.CreateLocalTable(items);
			using var colorsTable = db.CreateLocalTable(colors);
			using var stylesTable = db.CreateLocalTable(styles);

			var query =
				from item in itemsTable.LoadWith(it => it.Color).LoadWith(it => it.Style)
				from it in new[]
				{
					new {ColorName = (string?)(item.Color!.Name), StyleName = item.Style!.Name, Count = itemsTable.Count() },
					new {ColorName = (string?)null, StyleName               = item.Style!.Name, Count = 0 },
				}.DefaultIfEmpty()
				where it.ColorName == "Red"
				select it;

			AssertQuery(query);
		}

		class IntermediateProjection
		{
			public string?    ColorName   { get; set; }
			public string?    StyleName   { get; set; }
			public int        Count       { get; set; }
			public int        Conditional { get; set; }
			public List<int>? ArrayOfInts { get; set; }
		}

		[Test]
		public void WithComplexProjection([IncludeDataSources(
			TestProvName.AllFirebird4Plus,
			TestProvName.AllMySql80,
			TestProvName.AllOracle12Plus,
			TestProvName.AllPostgreSQL93Plus,
			ProviderName.SqlCe,
			TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var (items, colors, styles) = SomeItem.Seed();

			using var itemsTable  = db.CreateLocalTable(items);
			using var colorsTable = db.CreateLocalTable(colors);
			using var stylesTable = db.CreateLocalTable(styles);

			var query =
				from item in itemsTable.LoadWith(it => it.Color).LoadWith(it => it.Style)
				from it in new[]
				{
					new IntermediateProjection
					{
						ColorName   = item.Color!.Name,
						StyleName   = item.Style!.Name,
						Count       = itemsTable.Count(),
						Conditional = item.Color!.Name == "Red" ? itemsTable.Count() : 0,
						ArrayOfInts = new List<int> { 1, 2, 3 }
					},
					new IntermediateProjection()
					{
						ColorName   = null, 
						StyleName   = item.Style!.Name, 
						Count       = 0,
						ArrayOfInts = new List<int> { 4, 5, 6 }
					},
				}.DefaultIfEmpty()
				where it.ColorName == "Red" || it.Count == 0
				select it;

			AssertQuery(query);

			var distinctQuery = query.Select(x => x.Conditional).Distinct();

			AssertQuery(distinctQuery);
		}

		[Test]
		public void WithGroupedProjection([IncludeDataSources(
			TestProvName.AllPostgreSQL93Plus,
			TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var (items, colors, styles) = SomeItem.Seed();

			using var itemsTable  = db.CreateLocalTable(items);
			using var colorsTable = db.CreateLocalTable(colors);
			using var stylesTable = db.CreateLocalTable(styles);

			var groupingQuery =
				from item in itemsTable.LoadWith(it => it.Color).LoadWith(it => it.Style)
				group item by new { ColorName = item.Color!.Name ?? "", StyleName = item.Style!.Name ?? "" } into g
				select new
				{
					ColorName   = g.Key.ColorName,
					StyleName   = g.Key.StyleName,
					Count       = g.Count(),
				};

			var query =
				from item in groupingQuery
				from it in new[]
				{
					new IntermediateProjection
					{
						ColorName   = item.ColorName,
						StyleName   = item.StyleName,
						Count       = item.Count,
						Conditional = item.ColorName == "Red" ? item.Count : 0,
						ArrayOfInts = new List<int> { 1, 2, 3 }
					},
					new IntermediateProjection()
					{
						ColorName   = null, 
						StyleName   = item.StyleName, 
						Count       = 0,
						ArrayOfInts = new List<int> { 4, 5, 6 }
					},
				}.DefaultIfEmpty()
				where it.ColorName == "Red" || it.Count == 0
				select it;

			AssertQuery(query);
		}

		[Test]
		public void CoalesceColumnSelection([IncludeDataSources(
			TestProvName.AllPostgreSQL93Plus,
			TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var (items, colors, styles) = SomeItem.Seed();

			using var itemsTable  = db.CreateLocalTable(items);
			using var colorsTable = db.CreateLocalTable(colors);
			using var stylesTable = db.CreateLocalTable(styles);

			var query =
				from item in itemsTable.LoadWith(it => it.Color).LoadWith(it => it.Style)
				from it in new[]
				{
					new
					{
						ColorName   = (string?)item.Color!.Name,
						StyleName   = item.Style!.Name,
						ColorId     = (item.Color == null ? null : item.ColorId) ?? 0,
						Conditional = item.Color!.Name == "Red" ? itemsTable.Count() : 0,
					},
					new
					{
						ColorName   = (string?)null,
						StyleName   = item.Style!.Name,
						ColorId     = (item.Color == null ? null : item.ColorId) ?? 0,
						Conditional = 0,
					},
				}.DefaultIfEmpty()
				where it.ColorName == "Red"
				select new
				{
					it.ColorName,
					it.StyleName,
					it.Conditional,
#pragma warning disable CS8602
					StrValue = Sql.ConcatStrings(",", "", it.StyleName) == null ? null : Sql.ConcatStrings(",", "", it.StyleName) + ":" + Sql.ToNullable(it!.ColorId).ToString().PadLeft(4, '0'),
#pragma warning restore CS8602
				};

			query = query
				.OrderBy(x => x.StrValue);

			AssertQuery(query);
		}

	}
}
