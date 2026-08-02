using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
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
		public void TestInsertOrUpdate([InsertOrUpdateDataSources(false, TestProvName.AllPostgreSQL, TestProvName.AllSQLite, TestProvName.AllDuckDB)] string context)
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

		// PostgreSQL, SQLite and DuckDB disabled because they need real unique constrain on database side
		[Test]
		public void InsertOrReplaceTest([InsertOrUpdateDataSources(false, TestProvName.AllPostgreSQL, TestProvName.AllSQLite, TestProvName.AllDuckDB)] string context)
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
		public void TestMerge([MergeDataContextSource(false)] string context)
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
		public void TestMergeOnExplicit([MergeDataContextSource(false)] string context)
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

		[Table("SUG001NonPkKey")]
		sealed class NonPkKeyUpsert
		{
			[PrimaryKey] public int Id   { get; set; }
			[Column]     public int Code { get; set; }
			[Column]     public int Val  { get; set; }
		}

		// #5482 / SUG001 regression. The PR rewired SAP HANA's legacy 3-arg
		// InsertOrUpdate(insert, update, keySelector) onto the native `UPSERT … WITH PRIMARY KEY`, which
		// keys on the table PRIMARY KEY and ignores a caller-supplied non-PK key selector — whereas the
		// old 2-statement emulation honored the arbitrary key. Here the match key is the non-PK `Code`
		// column: since Code=100 already exists, the second call must UPDATE that row (count stays 1),
		// not INSERT a new row keyed on the PK `Id=2` (which would make 2 rows).
		// Insert and Update branches are deliberately aligned on their non-key columns (Id, Val) so the
		// native single-statement path is actually exercised — divergent branches fall back to the
		// UPDATE→INSERT emulation (HasDivergentInsertOrUpdateBranches) and would mask the regression.
		[Test]
		public void InsertOrUpdate_NonPkKey_MatchesKeyNotPrimaryKey([IncludeDataSources(TestProvName.AllSapHana)] string context)
		{
			using var db = GetDataConnection(context);
			using var _  = db.CreateLocalTable<NonPkKeyUpsert>();

			db.Insert(new NonPkKeyUpsert { Id = 1, Code = 100, Val = 10 });

			db.GetTable<NonPkKeyUpsert>().InsertOrUpdate(
				() => new NonPkKeyUpsert { Id = 2, Code = 100, Val = 20 },
				p  => new NonPkKeyUpsert { Id = 2, Val = 20 },
				() => new NonPkKeyUpsert { Code = 100 });

			var rows = db.GetTable<NonPkKeyUpsert>().ToList();
			Assert.That(rows, Has.Count.EqualTo(1));
			Assert.That(rows[0].Val, Is.EqualTo(20));
		}
	}
}
