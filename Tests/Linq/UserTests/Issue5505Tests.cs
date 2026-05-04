using System;
using System.Linq;
using System.Text.Json;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	/// <summary>
	/// Regression: UPDATE with a <c>ServerSideOnly</c> SQL function targeting a column
	/// that has a value converter fails translation because the builder wraps the
	/// function result with the converter's ToProvider expression.
	/// <see href="https://github.com/linq2db/linq2db/issues/5505"/>
	/// </summary>
	[TestFixture]
	public class Issue5505Tests : TestBase
	{
		public sealed record JsonData
		{
			public string? SomeKey { get; init; }
			public bool?   Updated { get; init; }
		}

		sealed class JsonDataConverterAttribute : ValueConverterAttribute
		{
			public JsonDataConverterAttribute()
			{
				ValueConverter = new ValueConverter<JsonData?, string?>(
					v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					s => s == null ? null : JsonSerializer.Deserialize<JsonData>(s, (JsonSerializerOptions?)null),
					handlesNulls: true);
			}
		}

		[Table]
		sealed class Issue5505Table
		{
			[PrimaryKey]                                                                public int       Id   { get; set; }
			[Column(DataType = DataType.BinaryJson, CanBeNull = true), JsonDataConverter] public JsonData? Data { get; set; }
		}

		[Sql.Expression("jsonb_set({0}, ARRAY[{1}], {2}::jsonb)", ServerSideOnly = true, InlineParameters = true)]
		static T? JsonbSet<T>(T? jsonb, string key, string value)
		{
			throw new InvalidOperationException();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5505")]
		public void UpdateWithServerSideOnlyAndValueConverter([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<Issue5505Table>();

			db.Insert(new Issue5505Table { Id = 1, Data = new JsonData { SomeKey = "abc" } });

			var affected = table
				.Where(x => x.Id == 1)
				.Set(x => x.Data, x => JsonbSet(x.Data, "updated", "true"))
				.Update();

			affected.ShouldBe(1);

			var result = table.First(x => x.Id == 1);
			result.Data.ShouldNotBeNull();
			result.Data.SomeKey.ShouldBe("abc");
			result.Data.Updated.ShouldBe(true);
		}
	}
}
