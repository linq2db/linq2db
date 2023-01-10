using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1268Tests : TestBase
	{
		[Table("DynamicColumnTable")]
		sealed class FullClass
		{
			[Column]
			         public int     Id        { get; set; }
			[Column] public string? Name      { get; set; }
			[Column] public bool    IsDeleted { get; set; }
		}

		[Table("DynamicColumnTable")]
		sealed class RepresentTable
		{
			[Column]
			         public int     Id        { get; set; }
			[Column] public string? Name      { get; set; }

			[DynamicColumnsStore]
			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/37999", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void InsertWithDynamicColumn([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();
			builder.Entity<RepresentTable>()
				.Property(x => Sql.Property<bool>(x, "IsDeleted"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<FullClass>())
			{
				var obj1 = new RepresentTable { Id = 1, Name = "Some1" };
				obj1.Values.Add("IsDeleted", true);
				db.Insert(obj1);

				var obj2 = new RepresentTable { Id = 2, Name = "Some2" };
				db.Insert(obj2);

				var loaded1 = db.GetTable<RepresentTable>().First(e => e.Name == "Some1");
				Assert.AreEqual(true, loaded1.Values["IsDeleted"]);


				var loaded2 = db.GetTable<RepresentTable>().First(e => e.Name == "Some2");
				Assert.AreEqual(false, loaded2.Values["IsDeleted"]);
			}
		}
	}
}
