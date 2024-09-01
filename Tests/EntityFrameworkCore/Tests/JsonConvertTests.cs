using System;
using System.Linq;

using LinqToDB.EntityFrameworkCore.Tests.Models.JsonConverter;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

using NUnit.Framework;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	[TestFixture]
	public class JsonConvertTests : ContextTestBase<JsonConvertContext>
	{
		protected override JsonConvertContext CreateProviderContext(string provider, DbContextOptions<JsonConvertContext> options)
		{
			return new JsonConvertContext(options);
		}

#pragma warning disable NUnit1028 // The non-test method is public
		public static string JsonValue(string? column, [NotParameterized] string path)
#pragma warning restore NUnit1028 // The non-test method is public
		{
			throw new NotSupportedException();
		}

		[Test]
		public void TestJsonConvert([EFIncludeDataSources(TestProvName.AllSqlServer2016Plus)] string provider)
		{
			// // converting from string, because usually JSON is stored as string, but it depends on DataProvider
			// Mapping.MappingSchema.Default.SetConverter<string, LocalizedString>(v => JsonConvert.DeserializeObject<LocalizedString>(v));
			//
			// // here we told linq2db how to pass converted value as DataParameter.
			// Mapping.MappingSchema.Default.SetConverter<LocalizedString, DataParameter>(v => new DataParameter("", JsonConvert.SerializeObject(v), LinqToDB.DataType.NVarChar));

			using var ctx = CreateContext(provider);

			ctx.EventScheduleItems.Delete();

			ctx.EventScheduleItems.Add(new EventScheduleItem()
			{
				NameLocalized = new LocalizedString() { English = "English", German = "German", Slovak = "Slovak" },
				GuidColumn = Guid.NewGuid()
			});
			ctx.SaveChanges();

			var queryable = ctx.EventScheduleItems
					.Where(p => p.Id < 10).ToLinqToDB();

			var path = "some";

			var items = queryable
					.Select(p => new
					{
						p.Id,
						p.NameLocalized,
						p.CrashEnum,
						p.GuidColumn,
						JsonValue = JsonValue(p.JsonColumn, path)
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
