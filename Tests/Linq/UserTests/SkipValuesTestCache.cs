using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

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
		public void TestSkipInsertUpdate([DataSources] string context, [Values(1, 2)] int value)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{
					var count = db.Insert(new TestTable() { Id = 1, Name = "John", Age = value });

					if (context.SupportsRowcount())
						Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					if (value == 2)
					{
						Assert.That(r.Age, Is.EqualTo(2));
					}
					else
					{
						Assert.That(r.Age, Is.Null);
					}

					r.Age = value;
					count = db.Update(r);

					if (context.SupportsRowcount())
						Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					if (value == 2)
					{
						Assert.That(r.Age, Is.EqualTo(2));
					}
					else
					{
						Assert.That(r.Age, Is.Null);
					}
				}
			}
		}

		[Test]
		public void TestSkipInsertOrReplace(
			[InsertOrUpdateDataSources(TestProvName.AllOracleNative)] string context,
			[Values(1, 2)] int value)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<TestTable>())
				{
					var count = db.InsertOrReplace(new TestTable() { Id = 1, Name = "John", Age = value });

					Assert.That(count, Is.GreaterThan(0));

					var r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;

					Assert.That(r, Is.Not.Null);
					if (value == 2)
					{
						Assert.That(r.Age, Is.EqualTo(2));
					}
					else
					{
						Assert.That(r.Age, Is.Null);
					}

					r.Age = value;
					count = db.InsertOrReplace(r);

					Assert.That(count, Is.GreaterThan(0));
					r = db.GetTable<TestTable>().FirstOrDefault(t => t.Id == 1)!;
					Assert.That(r, Is.Not.Null);
					if (value == 2)
					{
						Assert.That(r.Age, Is.EqualTo(2));
					}
					else
					{
						Assert.That(r.Age, Is.Null);
					}
				}
			}
		}
	}
}
