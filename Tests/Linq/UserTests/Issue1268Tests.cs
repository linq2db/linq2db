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
		class FullClass
		{
			[Column, Identity]
			         public int    Id        { get; set; }
			[Column] public string Name      { get; set; }
			[Column] public bool   IsDeleted { get; set; }
		}

		[Table("DynamicColumnTable")]
		class RepresentTable
		{
			[Column, Identity]
			         public int    Id        { get; set; }
			[Column] public string Name      { get; set; }

			[DynamicColumnsStore]
			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
		}


		[Test]
		public void InsertWithDynamicColumn(
			[IncludeDataSources(true, ProviderName.SQLite, ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
		string context)
		{
			var ms = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();
			builder.Entity<RepresentTable>()
				.Property(x => Sql.Property<bool>(x, "IsDeleted"));

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<FullClass>())
			{
				var obj1 = new RepresentTable { Name = "Some1" };
				obj1.Values.Add("IsDeleted", true);
				db.InsertWithIdentity(obj1);

				var obj2 = new RepresentTable { Name = "Some2" };
				db.InsertWithIdentity(obj2);

				var loaded1 = db.GetTable<RepresentTable>().First(e => e.Name == "Some1");
				Assert.AreEqual(true, loaded1.Values["IsDeleted"]);


				var loaded2 = db.GetTable<RepresentTable>().First(e => e.Name == "Some2");
				Assert.AreEqual(false, loaded2.Values["IsDeleted"]);
			}
		}

	}
}
