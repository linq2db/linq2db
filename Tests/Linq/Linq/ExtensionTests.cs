using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ExtensionTests : TestBase
	{
		public class ParenTable
		{
			public int  ParentID;
			public int? Value1;
		}

		[Test]
		public void TableName([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<ParenTable>().TableName("Parent").ToList();
		}

		[Test]
		public void DatabaseName([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<Parent>().DatabaseName(TestUtils.GetDatabaseName(db)).ToList();
		}

		[Test]
		public void SchemaName([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<Parent>().SchemaName("dbo").ToList();
		}

		[Test]
		public void AllNames([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<ParenTable>()
					.DatabaseName(TestUtils.GetDatabaseName(db))
					.SchemaName("dbo")
					.TableName("Parent")
					.ToList();
		}

		[Test]
		public void TableNameImmutable([SQLiteDataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var table1 = db.GetTable<ParenTable>();
				var table2 = table1.TableName("Parent2");
				var table3 = table2.TableName("Parent3");

				Assert.AreEqual(table1.TableName, "ParenTable");
				Assert.AreEqual(table2.TableName, "Parent2");
				Assert.AreEqual(table3.TableName, "Parent3");
			}
		}

		[Test]
		public void DatabaseNameImmutable([SQLiteDataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var table1 = db.GetTable<ParenTable>();
				var table2 = table1.DatabaseName("db2");
				var table3 = table2.DatabaseName("db3");

				Assert.AreEqual(table1.DatabaseName, null);
				Assert.AreEqual(table2.DatabaseName, "db2");
				Assert.AreEqual(table3.DatabaseName, "db3");
			}
		}

		[Test]
		public void SchemaNameImmutable([SQLiteDataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var table1 = db.GetTable<ParenTable>();
				var table2 = table1.SchemaName("schema2");
				var table3 = table2.SchemaName("schema3");

				Assert.AreEqual(table1.SchemaName, null);
				Assert.AreEqual(table2.SchemaName, "schema2");
				Assert.AreEqual(table3.SchemaName, "schema3");
			}
		}

		[Test]
		public void GetTableNameTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var tableName = db.GetTable<ParenTable>().TableName;

				Assert.That(tableName, Is.Not.Null);
			}
		}
	}
}
