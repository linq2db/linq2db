using System;
using System.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

using LinqToDB;

namespace Tests.UserTests
{
	public class SkipValuesTestCache : TestBase
	{
		[Table("PR_1598_Insert_Table_Cache")]
		public class TestTable
		{
			[Column("Id"), PrimaryKey]
			public Int32 Id { get; set; }
			[Column("Name"), SkipValuesOnInsert("John", "Max"), SkipValuesOnUpdate("Manuel")]
			public String Name { get; set; }
			[Column("Age"), SkipValuesOnInsert(2, 5)]
			public Int32? Age { get; set; }
		}


		[Test]
		public void TestSkipInsert([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{
					var count = db.Insert(new TestTable() { Id = 1, Name = "John", Age = 14 });

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.IsNull(r.Name);

					count = db.Insert(new TestTable() { Id = 2, Name = "Franki", Age = 15 });

					Assert.Greater(count, 0);

					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 2);

					Assert.IsNotNull(r);
					Assert.AreEqual(r.Name, "Franki");
				}
			}
		}

		[Test]
		public void TestSkipUpdate([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{
					var count = db.Insert(new TestTable() { Id = 1, Name = "Manuel", Age = 14 });

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.AreEqual(r.Name, "Manuel");

					r.Name = "Jacob";
					r.Age = 15;
					count = db.Update(r);
					Assert.Greater(count, 0);
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);
					Assert.IsNotNull(r);
					Assert.AreEqual(r.Age, 15);
					Assert.AreEqual(r.Name, "Jacob");

					r.Name = "Manuel";
					r.Age = 22;
					count = db.Update(r);
					Assert.Greater(count, 0);
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);
					Assert.IsNotNull(r);
					Assert.AreEqual(r.Age, 22);
					Assert.AreEqual(r.Name, "Jacob");
				}
			}
		}

		[Test]
		public void TestSkipInsertOrReplace([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{
					var count = db.InsertOrReplace(new TestTable() { Id = 1, Name = "Manuel", Age = 14 });

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					Assert.AreEqual(r.Name, "Manuel");

					r.Name = "Jacob";
					r.Age = 15;
					count = db.InsertOrReplace(r);
					Assert.Greater(count, 0);
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);
					Assert.IsNotNull(r);
					Assert.AreEqual(r.Age, 15);
					Assert.AreEqual(r.Name, "Jacob");

					r.Name = "Manuel";
					r.Age = 22;
					count = db.InsertOrReplace(r);
					Assert.Greater(count, 0);
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);
					Assert.IsNotNull(r);
					Assert.AreEqual(r.Age, 22);
					Assert.AreEqual(r.Name, "Jacob");
				}
			}
		}
	}
}
