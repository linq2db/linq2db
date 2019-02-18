using System;
using System.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

using LinqToDB;

namespace Tests.UserTests
{
	public class SkipValuesOnInsertTest: TestBase
	{
		[Table("PR_1598_Insert_Table")]
		public class TestTable
		{
			[Column("Id"), PrimaryKey]
			public Int32 Id { get; set; }
			[Column("Name"), SkipValuesOnInsert("John", "Max")]
			public String Name { get; set; }
			[Column("Age"), SkipValuesOnInsert(2, 5)]
			public Int32? Age { get; set; }
		}

		[Table("PR_1598_Mixed_Table")]
		public class TestTableMixed
		{
			[Column("Id"), PrimaryKey]
			public Int32 Id { get; set; }
			[Column("Name"), SkipValuesOnInsert("John"), SkipValuesOnUpdate("Max")]
			public String Name { get; set; }
			[Column("Age")]
			public Int32? Age { get; set; }
		}

		[Table("PR_1598_Insert_Null_Table")]
		public class TestTableNull
		{
			[Column("Id"), PrimaryKey]
			public Int32 Id { get; set; }
			[Column("Name")]
			public String Name { get; set; }
			[Column("Age"), SkipValuesOnInsert(null)]
			public Int32? Age { get; set; }
		}

		[Table("PR_1598_Insert_Fluent_Table")]
		public class TestTableFluent
		{
			[Column("Id"), PrimaryKey]
			public Int32 Id { get; set; }
			[Column("Name")]
			public String Name { get; set; }
			[Column("Age")]
			public Int32? Age { get; set; }
		}

		[Table("PR_1598_Insert_Enum_Table")]
		public class TestTableEnum
		{
			public enum GenderType
			{
				[MapValue(null)]
				Undefined,
				[MapValue("Male")]
				Male,
				[MapValue("Female")]
				Female
			}

			[Column("Id"), PrimaryKey]
			public Int32 Id { get; set; }
			[Column("Name")]
			public String Name { get; set; }
			[Column("Age")]
			public Int32? Age { get; set; }
			[Column("Gender"), SkipValuesOnInsert(GenderType.Female)]
			public GenderType Gender { get; set; }
		}

		[Test]
		public void TestSkipString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{

					var count = db.Insert(new TestTable() {Id = 1, Name = "John", Age = 14});

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.IsNull(r.Name);

					count = db.Insert(new TestTable() {Id = 2, Name = "Max", Age = 15});

					Assert.Greater(count, 0);

					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 2);

					Assert.IsNotNull(r);
					Assert.IsNull(r.Name);
				}
			}
		}

		[Test]
		public void TestNotSkipString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{

					var count = db.Insert(new TestTable() {Id = 1, Name = "Paul", Age = 14});

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.AreEqual(r.Name, "Paul");

					count = db.Insert(new TestTable() {Id = 2, Name = "Mary", Age = 15});

					Assert.Greater(count, 0);

					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 2);

					Assert.IsNotNull(r);
					Assert.AreEqual(r.Name, "Mary");
				}
			}
		}

		[Test]
		public void TestSkipInt32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{

					var count = db.Insert(new TestTable() {Id = 1, Name = "Smith", Age = 2});

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.IsNull(r.Age);

					count = db.Insert(new TestTable() {Id = 2, Name = "Tommy", Age = 5});

					Assert.Greater(count, 0);

					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 2);

					Assert.IsNotNull(r);
					Assert.IsNull(r.Age);
				}
			}
		}

		[Test]
		public void TestNotSkipInt32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{

					var count = db.Insert(new TestTable() {Id = 1, Name = "Smith", Age = 55});

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.AreEqual(r.Age, 55);

					count = db.Insert(new TestTable() {Id = 2, Name = "Tommy", Age = 50});

					Assert.Greater(count, 0);

					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 2);

					Assert.IsNotNull(r);
					Assert.AreEqual(r.Age, 50);
				}
			}
		}

		[Test]
		public void TestSkipNull([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				// Change default value, so that null is not inserted as default.
				db.MappingSchema.SetDefaultValue(typeof(Int32?), 0);
				using (db.CreateLocalTable<TestTableNull>())
				{
					
					var count = db.Insert(new TestTableNull() { Id = 1, Name = "Tommy", Age = null });

					Assert.Greater(count, 0);
					var r = db.GetTable<TestTableNull>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.IsNotNull(r.Age);
				}
			}
		}

		[Test]
		public void TestSkipWithFluentBuilder([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var mapping = db.MappingSchema.GetFluentMappingBuilder();
				mapping.Entity<TestTableFluent>().HasSkipValuesOnInsert(t => t.Age, 2, 5);
				using (db.CreateLocalTable<TestTableFluent>())
				{
					var count = db.Insert(new TestTableFluent() { Id = 1, Name = null, Age = 2 });

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTableFluent>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.IsNull(r.Age);
				}
			}
		}

		[Test]
		public void TestSkipWithEnum([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTableEnum>())
				{
					var count = db.Insert(new TestTableEnum() { Id = 1, Name = "Max", Age = 20, Gender = TestTableEnum.GenderType.Male});

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTableEnum>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.AreEqual(r.Gender, TestTableEnum.GenderType.Male);

					count = db.Insert(new TestTableEnum() { Id = 2, Name = "Jenny", Age = 25, Gender = TestTableEnum.GenderType.Female });

					Assert.Greater(count, 0);

					r = db.GetTable<TestTableEnum>().FirstOrDefault(t => t.Id == 2);

					Assert.IsNotNull(r);
					Assert.AreEqual(r.Gender, TestTableEnum.GenderType.Undefined);
				}
			}
		}

		[Test]
		public void TestSkipMixed([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTableMixed>())
				{
					var count = db.Insert(new TestTableMixed() { Id = 1, Name = "Jason", Age = 20 });

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTableMixed>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.AreEqual(r.Name, "Jason");

					r.Name = "Max";
					count = db.Update(r);
					Assert.Greater(count, 0);
					r = db.GetTable<TestTableMixed>().FirstOrDefault(t => t.Id == 1);
					Assert.IsNotNull(r);
					Assert.AreEqual(r.Name, "Jason");

					count = db.Insert(new TestTableMixed() { Id = 2, Name = "John", Age = 25 });

					Assert.Greater(count, 0);

					r = db.GetTable<TestTableMixed>().FirstOrDefault(t => t.Id == 2);

					Assert.IsNotNull(r);
					Assert.IsNull(r.Name);

					r.Name = "Jessy";
					count = db.Update(r);
					Assert.Greater(count, 0);
					r = db.GetTable<TestTableMixed>().FirstOrDefault(t => t.Id == 2);
					Assert.IsNotNull(r);
					Assert.AreEqual(r.Name, "Jessy");
				}
			}
		}
	}
}
