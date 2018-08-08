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


		[Test, Combinatorial]
		public void InsertWithDynamicColumn([DataSources] string context)
		{
			var ms = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();
			builder.Entity<RepresentTable>()
				.Property(x => Sql.Property<bool>(x, "IsDeleted"));

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<FullClass>())
			{
				var obj = new RepresentTable { Name = "Some" };
				obj.Values.Add("IsDeleted", true);
				db.InsertWithIdentity(obj);

				var loaded = db.GetTable<RepresentTable>().First();

				Assert.AreEqual(true, loaded.Values["IsDeleted"]);
			}
		}

	}
}
