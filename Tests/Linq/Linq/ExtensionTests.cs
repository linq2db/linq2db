using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ExtensionTests : TestBase
	{
		public class ParenTable
		{
			public int  ParentID;
			public int? Value1;
		}

		[Test]
		public void TableName([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<ParenTable>().TableName("Parent").ToList();
		}

		[Test]
		public void DatabaseName([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<Parent>().DatabaseName(TestUtils.GetDatabaseName(db, context)).ToList();
		}

		[Test]
		public void SchemaName([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<Parent>().SchemaName("dbo").ToList();
		}

		[Test]
		public void AllNames([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<ParenTable>()
				.DatabaseName(TestUtils.GetDatabaseName(db, context))
				.SchemaName("dbo")
				.TableName("Parent")
				.ToList();
		}

		[Test]
		public void TableNameImmutable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var table1 = db.GetTable<ParenTable>();
			var table2 = table1.TableName("Parent2");
			var table3 = table2.TableName("Parent3");
			using (Assert.EnterMultipleScope())
			{
				Assert.That(table1.TableName, Is.EqualTo("ParenTable"));
				Assert.That(table2.TableName, Is.EqualTo("Parent2"));
				Assert.That(table3.TableName, Is.EqualTo("Parent3"));
			}
		}

		[Test]
		public void DatabaseNameImmutable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var table1 = db.GetTable<ParenTable>();
			var table2 = table1.DatabaseName("db2");
			var table3 = table2.DatabaseName("db3");
			using (Assert.EnterMultipleScope())
			{
				Assert.That(table1.DatabaseName, Is.Null);
				Assert.That(table2.DatabaseName, Is.EqualTo("db2"));
				Assert.That(table3.DatabaseName, Is.EqualTo("db3"));
			}
		}

		[Test]
		public void SchemaNameImmutable([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var table1 = db.GetTable<ParenTable>();
			var table2 = table1.SchemaName("schema2");
			var table3 = table2.SchemaName("schema3");
			using (Assert.EnterMultipleScope())
			{
				Assert.That(table1.SchemaName, Is.Null);
				Assert.That(table2.SchemaName, Is.EqualTo("schema2"));
				Assert.That(table3.SchemaName, Is.EqualTo("schema3"));
			}
		}

		[Test]
		public void GetTableNameTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var tableName = db.GetTable<ParenTable>().TableName;

			Assert.That(tableName, Is.Not.Null);
		}
	}
}
