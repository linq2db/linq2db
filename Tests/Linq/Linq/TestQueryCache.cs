using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class TestQueryCache : TestBase
	{
		[Table]
		class SampleClass
		{
			public int Id        { get; set; }
			public string StrKey { get; set; }
			public string Value  { get; set; }
		}

		[Table]
		class SampleClassWithIdentity
		{
			[Identity]
			public int Id        { get; set; }
			public string Value  { get; set; }
		}

		[Test]
		public void BasicOperations([IncludeDataSources(false, ProviderName.SQLiteMS)] string context, [Values("Value1", "Value2")] string columnName)
		{
			var ms = CreateMappingSchema(columnName);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<SampleClass>())
			using (db.CreateLocalTable<SampleClassWithIdentity>())
			{
				db.Insert(new SampleClass() { Id = 1, StrKey = "K1", Value = "V1" });
				db.Insert(new SampleClass() { Id = 2, StrKey = "K2", Value = "V2" });
				db.InsertOrReplace(new SampleClass() { Id = 3, StrKey = "K3", Value = "V3" });
				db.InsertWithIdentity(new SampleClassWithIdentity() { Value = "V4" });

				db.Delete(new SampleClass() { Id = 1, StrKey = "K1" });
				db.Update(new SampleClass() { Id = 2, StrKey = "K2", Value = "VU" });

				var found = null != QueryVisitor.Find(table.GetSelectQuery(),
					            e => e is SqlField f && f.PhysicalName == columnName);

				var foundKey = null != QueryVisitor.Find(table.GetSelectQuery(),
					               e => e is SqlField f && f.PhysicalName == columnName);

				Assert.IsTrue(found);
				Assert.IsTrue(foundKey);

				var result = table.ToArray();
			}
		}

		[Test]
		public async Task BasicOperationsAsync([IncludeDataSources(false, ProviderName.SQLiteMS)] string context, [Values("Value1", "Value2")] string columnName)
		{
			var ms = CreateMappingSchema(columnName);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<SampleClass>())
			using (db.CreateLocalTable<SampleClassWithIdentity>())
			{
				await db.InsertAsync(new SampleClass() { Id = 1, StrKey = "K1", Value = "V1" });
				await db.InsertAsync(new SampleClass() { Id = 2, StrKey = "K2", Value = "V2" });
				await db.InsertOrReplaceAsync(new SampleClass() { Id = 3, StrKey = "K3", Value = "V3" });
				await db.InsertWithIdentityAsync(new SampleClassWithIdentity() { Value = "V4" });

				await db.DeleteAsync(new SampleClass() { Id = 1, StrKey = "K1" });
				await db.UpdateAsync(new SampleClass() { Id = 2, StrKey = "K2", Value = "VU" });

				var found = null != QueryVisitor.Find(table.GetSelectQuery(),
					            e => e is SqlField f && f.PhysicalName == columnName);

				var foundKey = null != QueryVisitor.Find(table.GetSelectQuery(),
					            e => e is SqlField f && f.PhysicalName == columnName);

				Assert.IsTrue(found);
				Assert.IsTrue(foundKey);

				var result = await table.ToArrayAsync();
			}
		}

		[Test]
		public void TestSchema([IncludeDataSources(false, ProviderName.SQLiteMS)] string context)
		{
			void TestMethod(string columnName, string schemaName = null)
			{
				var ms = CreateMappingSchema(columnName, schemaName);
				using (var db = GetDataContext(context, ms))
				using (db.CreateLocalTable<SampleClass>())
				{
					db.Insert(new SampleClass() { Id = 1, StrKey = "K1", Value = "V1" });
				}
			}

			TestMethod("Value1");
			TestMethod("Value2");
			
			TestMethod("ValueF1", "FAIL");
			Assert.Throws(Is.AssignableTo(typeof(Exception)), () => TestMethod("ValueF2", "FAIL"));
		}

		private static MappingSchema CreateMappingSchema(string columnName, string schemaName = null)
		{
			var ms = new MappingSchema(schemaName);
			var builder = ms.GetFluentMappingBuilder();

			builder.Entity<SampleClass>()
				.Property(e => e.Id).IsPrimaryKey()
				.Property(e => e.StrKey).IsPrimaryKey().HasColumnName("Key" + columnName).HasLength(50)
				.Property(e => e.Value).HasColumnName(columnName).HasLength(50);

			builder.Entity<SampleClassWithIdentity>()
				.Property(e => e.Id).IsPrimaryKey()
				.Property(e => e.Value).HasColumnName(columnName).HasLength(50);

			return ms;
		}

	}
}
