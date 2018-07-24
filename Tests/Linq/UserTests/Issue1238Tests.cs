using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;
	using Tests.Model;

	[TestFixture]
	public class Issue1238Tests : TestBase
	{
		[Table("InheritanceParent")]
		public class TestTable
		{
			[Column("InheritanceParentId"), PrimaryKey]           public int    Key1;
			[Column("Name"),                PrimaryKey, Nullable] public string Key2;
			[Column("TypeDiscriminator")]                         public int?   Data;
		}

		[Test, DataContextSource(false)]
		public void TestInsertOrUpdate(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

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

		[Test, DataContextSource(false)]
		public void TestInsertOrReplace(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

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

		[Test, DataContextSource(false)]
		public void TestMerge(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

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
					.InsertWhenNotMatched()
					.UpdateWhenMatched()
					.Merge();

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());

				db.GetTable<TestTable>()
					.Merge()
					.Using(new[] { record })
					.OnTargetKey()
					.InsertWhenNotMatched()
					.UpdateWhenMatched()
					.Merge();

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());
			}
		}

		[Test, DataContextSource(false)]
		public void TestMergeOnExplicit(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

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
					.InsertWhenNotMatched()
					.UpdateWhenMatched()
					.Merge();

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());

				db.GetTable<TestTable>()
					.Merge()
					.Using(new[] { record })
					.On((t, s) => t.Key1 == s.Key1 && t.Key2 == s.Key2)
					.InsertWhenNotMatched()
					.UpdateWhenMatched()
					.Merge();

				Assert.AreEqual(1, db.GetTable<TestTable>().Count());
			}
		}
	}
}
