using System;
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
		public void TestMappingCombine([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();
			mb.Entity<Class1>().HasTableName("Class1Table");

			using (var db = new DataConnection("SQLite.MS", ms))
			{
				using (var u = db.CreateLocalTable<Class1>())
				{
					var l = db.GetTable<Class1>().ToList();
				}
			}

			var newMs = new MappingSchema(ms);
			var mb2 = newMs.GetFluentMappingBuilder();
			mb2.Entity<Class2>().HasTableName("Class2Table");
			using (var db = new DataConnection("SQLite.MS", newMs))
			{
				var ed1 = newMs.GetEntityDescriptor(typeof(Class2));
				var ed2 = db.MappingSchema.GetEntityDescriptor(typeof(Class2));

				try
				{
					var l = db.GetTable<Class2>().ToList();
				}
				catch(Exception ex)
				{
					var msg = ex.Message;
					StringAssert.Contains("Class2Table", msg);
				}
			}
		}
	}
}
