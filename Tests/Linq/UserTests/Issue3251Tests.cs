using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3251Tests : TestBase
	{

		public class Class1
		{
			public int Id { get; set; }
		}

		public class Class2
		{
			public int Id { get; set; }
		}

		[Test]
		public void TestMappingCombine([IncludeDataSources(ProviderName.SQLiteMS, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);
			mb.Entity<Class1>().HasTableName("Class1Table").Build();

			using (var db = new DataConnection(new DataOptions().UseConfiguration(ProviderName.SQLiteMS, ms)))
			{
				using (var u = db.CreateLocalTable<Class1>())
				{
					var l = db.GetTable<Class1>().ToList();
				}
			}

			var newMs = new MappingSchema(ms);
			var mb2 = new FluentMappingBuilder(newMs);
			mb2.Entity<Class2>().HasTableName("Class2Table").Build();
			using (var db = new DataConnection(new DataOptions().UseConfiguration(ProviderName.SQLiteMS, newMs)))
			{
				var ed1 = newMs.GetEntityDescriptor(typeof(Class2));
				var ed2 = db.MappingSchema.GetEntityDescriptor(typeof(Class2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(ed1.Name.Name, Is.EqualTo("Class2Table"));
					Assert.That(ed2.Name.Name, Is.EqualTo("Class2Table"));
				}
			}
		}
	}
}
