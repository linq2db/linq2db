using System;
using System.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

using LinqToDB;
using LinqToDB.Common;

namespace Tests.UserTests
{
	public class SkipValuesOnInsertTest: TestBase
	{
		[Table("PR_1598_Table")]
		public class TestTable
		{
			[Column("Id"), PrimaryKey]
			public Int32 Id { get; set; }
			[Column("Name"), SkipValuesOnInsert("John", "Max")]
			public String Name { get; set; }
			[Column("Age"), SkipValuesOnInsert(2, 5)]
			public Int32? Age { get; set; }
		}

		[Table("PR_1598_Null_Table")]
		public class TestTableNull
		{
			[Column("Id"), PrimaryKey]
			public Int32 Id { get; set; }
			[Column("Name")]
			public String Name { get; set; }
			[Column("Age"), SkipValuesOnInsert(null)]
			public Int32? Age { get; set; }
		}

		[Table("PR_1598_Fluent_Table")]
		public class TestTableFluent
		{
			[Column("Id"), PrimaryKey]
			public Int32 Id { get; set; }
			[Column("Name")]
			public String Name { get; set; }
			[Column("Age")]
			public Int32? Age { get; set; }
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
	}
}
