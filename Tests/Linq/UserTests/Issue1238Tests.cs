﻿using System;
using System.Linq;

using NUnit.Framework;

using Tests.Model;
using Tests.xUpdate;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;

	[TestFixture]
	public class Issue1238Tests : TestBase
	{
		[Table("InheritanceParent")]
		public class TestTable
		{
			[Column("InheritanceParentId"), PrimaryKey]           public int     Key1;
			[Column("Name"),                PrimaryKey, Nullable] public string? Key2;
			[Column("TypeDiscriminator")]                         public int?    Data;
		}

		// PostgreSQL disabled because it needs real primary key on database side
		// DB2 needs merge api + arraycontext features from 3.0
		[ActiveIssue(1239, Configuration = ProviderName.DB2)]
		[Test]
		public void TestInsertOrUpdate([InsertOrUpdateDataSources(false, TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<TestTable>().Delete();

				db.GetTable<TestTable>()
					.InsertOrUpdate(
						() => new TestTable()
						{
							Key1 = 143,
							Key2 = null,
							Data = 1
						},
						p => new TestTable()
						{
							Data = 1
						});

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());

				db.GetTable<TestTable>()
					.InsertOrUpdate(
						() => new TestTable()
						{
							Key1 = 143,
							Key2 = null,
							Data = 1
						},
						p => new TestTable()
						{
							Data = 1
						});

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());
			}
		}

		// PostgreSQL disabled because it needs real primary key on database side
		[ActiveIssue(
			Configuration = ProviderName.DB2,
			Details       = "ERROR [42610] [IBM][DB2/NT64] SQL0418N  The statement was not processed because the statement contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword, or a null value.")]
		[Test]
		public void InsertOrReplaceTest([InsertOrUpdateDataSources(false, TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<TestTable>().Delete();

				var record = new TestTable()
				{
					Key1 = 143,
					Key2 = null,
					Data = 1
				};

				db.InsertOrReplace(record);

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());

				db.InsertOrReplace(record);

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());
			}
		}

		[Test]
		public void TestMerge([MergeTests.MergeDataContextSource(false)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<TestTable>().Delete();

				var record = new TestTable()
				{
					Key1 = 143,
					Key2 = null,
					Data = 1
				};

				db.GetTable<TestTable>()
					.Merge()
					.Using(new[] { record })
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());

				db.GetTable<TestTable>()
					.Merge()
					.Using(new[] { record })
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());
			}
		}

		[Test]
		public void TestMergeOnExplicit([MergeTests.MergeDataContextSource(false)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<TestTable>().Delete();

				var record = new TestTable()
				{
					Key1 = 143,
					Key2 = null,
					Data = 1
				};

				db.GetTable<TestTable>()
					.Merge()
					.Using(new[] { record })
					.On((t, s) => t.Key1 == s.Key1 && t.Key2 == s.Key2)
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());

				db.GetTable<TestTable>()
					.Merge()
					.Using(new[] { record })
					.On((t, s) => t.Key1 == s.Key1 && t.Key2 == s.Key2)
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());
			}
		}
	}
}
