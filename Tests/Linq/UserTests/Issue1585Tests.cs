using System;
using System.Threading;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;

	[TestFixture]
	public class Issue1585Tests : TestBase
	{
		class Test1585
		{
			public int Id { get; set; }
		}

		MappingSchema SetFluentMappings()
		{
			var ms            = new MappingSchema();
			var tableName     = nameof(Test1585);
			var fluentBuilder = ms.GetFluentMappingBuilder();

			fluentBuilder.Entity<Test1585>()
				.HasTableName(tableName)
				.Property(x => x.Id).IsColumn().IsNullable(false).HasColumnName("Id").IsPrimaryKey();

			return ms;
		}

		[Test]
		public void TestEntityDescriptor([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = SetFluentMappings();

			EntityDescriptor ed1;
			EntityDescriptor ed2;

			using (var db = GetDataContext(context, ms))
			{
				try
				{
					db.DropTable<Test1585>();
				}
				catch
				{ }

				db.CreateTable<Test1585>();
				var data = db.GetTable<Test1585>();
				ed1 = db.MappingSchema.GetEntityDescriptor(typeof(Test1585));	
			}
			using (var db = GetDataContext(context, ms))
			{
				var data = db.GetTable<Test1585>();
				ed2 = db.MappingSchema.GetEntityDescriptor(typeof(Test1585));
			}

			Assert.AreEqual(ed1, ed2);
		}
	}
}
