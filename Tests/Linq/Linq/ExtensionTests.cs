﻿using System;
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
