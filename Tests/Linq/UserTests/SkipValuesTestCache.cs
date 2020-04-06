using System;
using System.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

using LinqToDB;
using LinqToDB.Linq;

namespace Tests.UserTests
{
	public class SkipValuesTestCache : TestBase
	{
		[Table("PR_1598_Insert_Table_Cache")]
		public class TestTable
		{
			[Column("Id"), PrimaryKey]
			public int Id { get; set; }
			[Column("Name")]
			public string? Name { get; set; }
			[Column("Age"), SkipValuesOnInsert(1), SkipValuesOnUpdate(1)]
			public int? Age { get; set; }
		}
		
		[Test]
		public void TestSkipInsertUpdate([DataSources] string context, [Values(1, 2, 1)] int value)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{
					var count = db.Insert(new TestTable() { Id = 1, Name = "John", Age = value });

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					if (value == 2)
					{
						Assert.AreEqual(r.Age, 2);
					}
					else
					{
						Assert.IsNull(r.Age);
					}

					r.Age = value;
					count = db.Update(r);

					Assert.Greater(count, 0);
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);
					Assert.IsNotNull(r);
					if (value == 2)
					{
						Assert.AreEqual(r.Age, 2);
					}
					else
					{
						Assert.IsNull(r.Age);
					}
				}
			}
		}

		[Test]
		public void TestSkipInsertOrReplace(
			[DataSources(TestProvName.AllOracleNative)] string context,
			[Values(1, 2, 1)] int value)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{
					var count = db.InsertOrReplace(new TestTable() { Id = 1, Name = "John", Age = value });

					Assert.Greater(count, 0);

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);

					Assert.IsNotNull(r);
					if (value == 2)
					{
						Assert.AreEqual(r.Age, 2);
					}
					else
					{
						Assert.IsNull(r.Age);
					}

					r.Age = value;
					count = db.InsertOrReplace(r);

					Assert.Greater(count, 0);
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1);
					Assert.IsNotNull(r);
					if (value == 2)
					{
						Assert.AreEqual(r.Age, 2);
					}
					else
					{
						Assert.IsNull(r.Age);
					}
				}
			}
		}
	}
}
