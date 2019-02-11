using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;
using NUnit.Framework;

using LinqToDB;

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
	}
}
