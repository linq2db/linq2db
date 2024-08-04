using System;
using System.Linq;
using LinqToDB.EntityFrameworkCore.BaseTests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer
{
	[TestFixture]
	public class JsonConvertTests : TestsBase
	{
		private DbContextOptions<JsonConvertContext> _options;

		private sealed class LocalizedString
		{
			public string English { get; set; } = null!;
			public string German { get; set; } = null!;
			public string Slovak { get; set; } = null!;
		}

		private class EventScheduleItemBase
		{
			public int Id { get; set; }
			public virtual LocalizedString NameLocalized { get; set; } = null!;
			public virtual string? JsonColumn { get; set; }
		}

#pragma warning disable CA1028 // Enum Storage should be Int32
		private enum CrashEnum : byte
#pragma warning restore CA1028 // Enum Storage should be Int32
		{
			OneValue = 0,
			OtherValue = 1
		}

		private sealed class EventScheduleItem : EventScheduleItemBase
		{
			public CrashEnum CrashEnum { get; set; }
			public Guid GuidColumn { get; set; }
		}

		private sealed class JsonConvertContext : DbContext
		{
			public JsonConvertContext()
			{
			}

			public JsonConvertContext(DbContextOptions<JsonConvertContext> options)
				: base(options)
			{
			}

			public DbSet<EventScheduleItem> EventScheduleItems { get; set; } = null!;

			protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
			{
				if (!optionsBuilder.IsConfigured) optionsBuilder.UseSqlServer("conn string");
			}

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity<EventScheduleItem>(entity =>
				{
					entity.ToTable("EventScheduleItem");
					entity.Property(e => e.NameLocalized)
						.HasColumnName("NameLocalized_JSON")
						.HasConversion(v => JsonConvert.SerializeObject(v),
							v => JsonConvert.DeserializeObject<LocalizedString>(v) ?? new());
					entity.Property(e => e.CrashEnum).HasColumnType("tinyint");
					entity.Property(e => e.GuidColumn).HasColumnType("uniqueidentifier");
				});

#if !NETFRAMEWORK
				modelBuilder.HasDbFunction(typeof(JsonConvertTests).GetMethod(nameof(JsonValue))!)
					.HasTranslation(e => new SqlFunctionExpression(
						"JSON_VALUE", e, true, e.Select(_ => false), typeof(string), null));
#endif
			}
		}

		public JsonConvertTests()
		{
			var optionsBuilder = new DbContextOptionsBuilder<JsonConvertContext>();
			//new SqlServerDbContextOptionsBuilder(optionsBuilder);

			optionsBuilder.UseSqlServer(Settings.JsonConvertConnectionString);
			optionsBuilder.UseLoggerFactory(TestUtils.LoggerFactory);

			_options = optionsBuilder.Options;
		}

#if !NETFRAMEWORK
#pragma warning disable NUnit1028 // The non-test method is public
		public static string JsonValue(string? column, [NotParameterized] string path)
#pragma warning restore NUnit1028 // The non-test method is public
		{
			throw new NotSupportedException();
		}
#endif

		[Test]
		public void TestJsonConvert()
		{
			LinqToDBForEFTools.Initialize();
			
			// // converting from string, because usually JSON is stored as string, but it depends on DataProvider
			// Mapping.MappingSchema.Default.SetConverter<string, LocalizedString>(v => JsonConvert.DeserializeObject<LocalizedString>(v));
			//
			// // here we told linq2db how to pass converted value as DataParameter.
			// Mapping.MappingSchema.Default.SetConverter<LocalizedString, DataParameter>(v => new DataParameter("", JsonConvert.SerializeObject(v), LinqToDB.DataType.NVarChar));

			using (var ctx = new JsonConvertContext(_options))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();

				ctx.EventScheduleItems.Delete();

				ctx.EventScheduleItems.Add(new EventScheduleItem()
				{
					NameLocalized = new LocalizedString() { English = "English", German = "German", Slovak = "Slovak" },
					GuidColumn = Guid.NewGuid()
				});
				ctx.SaveChanges();

				var queryable = ctx.EventScheduleItems
					.Where(p => p.Id < 10).ToLinqToDB();

#if !NETFRAMEWORK
				var path = "some";
#endif

				var items = queryable
					.Select(p => new
					{
						p.Id,
						p.NameLocalized,
						p.CrashEnum,
						p.GuidColumn,
#if !NETFRAMEWORK
						JsonValue = JsonValue(p.JsonColumn, path)
#endif
					});

				var item = items.FirstOrDefault();

				Assert.That(item, Is.Not.Null);
				Assert.Multiple(() =>
				{
					Assert.That(item!.NameLocalized.English, Is.EqualTo("English"));
					Assert.That(item.NameLocalized.German, Is.EqualTo("German"));
					Assert.That(item.NameLocalized.Slovak, Is.EqualTo("Slovak"));
				});

				//TODO: make it work
				// var concrete = queryable.Select(p => new
				// {
				// 	p.Id,
				// 	English = p.NameLocalized.English
				// }).FirstOrDefault();
				//
				// Assert.That(concrete.English, Is.EqualTo("English"));
			}
		}
	}
}
