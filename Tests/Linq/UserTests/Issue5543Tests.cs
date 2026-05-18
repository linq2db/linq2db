using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5543Tests : TestBase
	{
		public record Primary
		{
			public Guid      Id        { get; init; }
			public string?   Code      { get; init; }
			public Secondary Secondary { get; init; } = new();
		}

		public record Secondary
		{
			public bool Field { get; init; }
		}

		static MappingSchema BuildSchema()
		{
			var ms      = new MappingSchema();
			var builder = new FluentMappingBuilder(ms);

			builder.Entity<Primary>()
				.HasTableName("Primary_5543")
				.Property(o => o.Id).IsPrimaryKey().HasColumnName("id")
				.Property(o => o.Code).HasColumnName("code")
				.Property(o => o.Secondary.Field).HasColumnName("field");

			builder.Build();
			return ms;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5543")]
		public void UpdateWhenMatchedWithNestedMapping([IncludeDataSources(TestProvName.AllPostgreSQL15Plus)] string context)
		{
			var ms = BuildSchema();

			using var db    = GetDataContext(context, o => o.UseMappingSchema(ms));
			using var table = db.CreateLocalTable<Primary>();

			var seedId = new Guid("11111111-1111-1111-1111-111111111111");

			db.Insert(new Primary
			{
				Id        = seedId,
				Code      = "first",
				Secondary = new Secondary { Field = false },
			});

			var updateRecords = new[]
			{
				new Primary
				{
					Id        = Guid.NewGuid(),
					Code      = "first",
					Secondary = new Secondary { Field = true },
				}
			};

			table.Merge()
				.Using(updateRecords)
				.On(t => t.Code, s => s.Code)
				.UpdateWhenMatched()
				.Merge();

			var row = table.Single(r => r.Code == "first");
			row.Secondary.Field.ShouldBeTrue();
		}
	}
}
