using System;
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

		// PostgreSQL and SQLite disabled because they need real unique constrain on database side
		// DB2 needs merge api + arraycontext features from 3.0
		[ActiveIssue(1239, Configuration = ProviderName.DB2)]
		[Test]
		public void TestInsertOrUpdate([InsertOrUpdateDataSources(false, TestProvName.AllPostgreSQL, TestProvName.AllSQLite)] string context)
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

				Assert.That(db.GetTable<TestTable>().Count(), Is.EqualTo(1));

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

				Assert.That(db.GetTable<TestTable>().Count(), Is.EqualTo(1));
			}
		}

		// PostgreSQL and SQLite disabled because they need real unique constrain on database side
		[Test]
		public void InsertOrReplaceTest([InsertOrUpdateDataSources(false, TestProvName.AllPostgreSQL, TestProvName.AllSQLite)] string context)
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

				Assert.That(db.GetTable<TestTable>().Count(), Is.EqualTo(1));

				db.InsertOrReplace(record);

				Assert.That(db.GetTable<TestTable>().Count(), Is.EqualTo(1));
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

				Assert.That(db.GetTable<TestTable>().Count(), Is.EqualTo(1));

				db.GetTable<TestTable>()
					.Merge()
					.Using(new[] { record })
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();

				Assert.That(db.GetTable<TestTable>().Count(), Is.EqualTo(1));
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

				Assert.That(db.GetTable<TestTable>().Count(), Is.EqualTo(1));

				db.GetTable<TestTable>()
					.Merge()
					.Using(new[] { record })
					.On((t, s) => t.Key1 == s.Key1 && t.Key2 == s.Key2)
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();

				Assert.That(db.GetTable<TestTable>().Count(), Is.EqualTo(1));
			}
		}
	}
}
