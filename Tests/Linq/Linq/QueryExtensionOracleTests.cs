using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.Oracle;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryExtensionOracleTests : TestBase
	{
		[Test]
		public void TableSubqueryHintTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in
					(
						from p in db.Parent.TableHint(OracleHints.TableHint.Full).With(OracleHints.TableHint.Cache)
						select p
					)
					.AsSubQuery()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FULL(p_1.p) CACHE(p_1.p) */"));
		}

		[Test]
		public void TableSubqueryHintTest2([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in
					(
						from p in
							(
								from p in db.Parent.TableHint(OracleHints.TableHint.Full)
								from c in db.Child.TableHint(OracleHints.TableHint.Full)
								select p
							)
							.AsSubQuery()
						select p
					)
					.AsSubQuery()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FULL(p_2.p_1.p) FULL(p_2.p_1.c_1)"));
		}

		[Test]
		public void TableHintTest([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.TableHint.Cache,
				OracleHints.TableHint.Cluster,
				OracleHints.TableHint.DrivingSite,
				OracleHints.TableHint.Fact,
				OracleHints.TableHint.Full,
				OracleHints.TableHint.Hash,
				OracleHints.TableHint.NoCache,
				OracleHints.TableHint.NoFact,
				OracleHints.TableHint.NoParallel,
				OracleHints.TableHint.NoPxJoinFilter,
				OracleHints.TableHint.NoUseHash,
				OracleHints.TableHint.PxJoinFilter
				)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.With(hint)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {hint}(p) */"));
		}

		[Test]
		public void TableHintDynamicSamplingTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(OracleHints.TableHint.DynamicSampling, 1)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ DYNAMIC_SAMPLING(p 1)"));
		}

		[Test]
		public void TableHintParallelTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(OracleHints.TableHint.Parallel, (object)",", 5)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(p , 5)"));
		}

		[Test]
		public void TableHintParallelDefaultTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(OracleHints.TableHint.Parallel, "DEFAULT")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(p DEFAULT)"));
		}

		[Test]
		public void IndexHintSingleTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(OracleHints.IndexHint.Index, "parent_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ INDEX(p parent_ix)"));
		}

		[Test]
		public void IndexHintTest([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.IndexHint.Index,
				OracleHints.IndexHint.IndexAsc,
				OracleHints.IndexHint.IndexCombine,
				OracleHints.IndexHint.IndexDesc,
				OracleHints.IndexHint.IndexFastFullScan,
				OracleHints.IndexHint.IndexJoin,
				OracleHints.IndexHint.IndexSkipScan,
				OracleHints.IndexHint.IndexSkipScanAsc,
				OracleHints.IndexHint.IndexSkipScanDesc,
				OracleHints.IndexHint.NoIndex,
				OracleHints.IndexHint.NoIndexFastFullScan,
				OracleHints.IndexHint.NoIndexSkipScan,
				OracleHints.IndexHint.NoParallelIndex,
				OracleHints.IndexHint.ParallelIndex,
				OracleHints.IndexHint.UseNlWithIndex
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(hint, "parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {hint}(p parent_ix parent2_ix)"));
		}

		[Test]
		public void QueryHintTest([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.QueryHint.AllRows,
				OracleHints.QueryHint.Append,
				OracleHints.QueryHint.CursorSharingExact,
				OracleHints.QueryHint.ModelMinAnalysis,
				OracleHints.QueryHint.NoAppend,
				OracleHints.QueryHint.NoExpand,
				OracleHints.QueryHint.NoPushSubQuery,
				OracleHints.QueryHint.NoRewrite,
				OracleHints.QueryHint.NoQueryTransformation,
				OracleHints.QueryHint.NoStarTransformation,
				OracleHints.QueryHint.NoUnnest,
				OracleHints.QueryHint.NoXmlQueryRewrite,
				OracleHints.QueryHint.Ordered,
				OracleHints.QueryHint.PushSubQueries,
				OracleHints.QueryHint.Rule,
				OracleHints.QueryHint.StarTransformation,
				OracleHints.QueryHint.Unnest,
				OracleHints.QueryHint.UseConcat
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(hint);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {hint} */"));
		}

		[Test]
		public void QueryHintWithQueryBlockTest([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.QueryHint.NoUnnest
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in
					db.Parent.AsSubQuery("Parent")
					on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(hint, "@Parent");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {hint}(@Parent) */"));
			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(Parent) */"));
		}

		[Test]
		public void QueryHintFirstRowsTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(OracleHints.QueryHint.FirstRows(25));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FIRST_ROWS(25) */"));
		}

		[Test]
		public void UnionTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q1 =
				from c in db.Child
				join p in db.Parent.TableHint(OracleHints.TableHint.Full) on c.ParentID equals p.ParentID
				select p;

			var q =
				q1.QueryName("qb_1").Union(q1.QueryName("qb_2"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ QB_NAME(qb_1) FULL(p@qb_1) FULL(p_1@qb_2) */"));
		}

		[Test]
		public void UnionTest2([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q1 =
				from c in db.Child
				join p in db.Parent.TableHint(OracleHints.TableHint.Full) on c.ParentID equals p.ParentID
				select p;

			var q =
				from p in q1.QueryName("qb_1").Union(q1.QueryName("qb_2"))
				where p.ParentID > 0
				select p.ParentID;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FULL(p@qb_1) FULL(p_1@qb_2) */"));
			Assert.That(LastQuery, Contains.Substring("SELECT /*+ QB_NAME(qb_1) */"));
			Assert.That(LastQuery, Contains.Substring("SELECT /*+ QB_NAME(qb_2) */"));
		}

		[Test]
		public void UnionTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q1 =
				from c in db.Child
				join p in db.Parent.TableHint(OracleHints.TableHint.Full) on c.ParentID equals p.ParentID
				select p;

			var q =
				from p in q1.Union(q1)
				where p.ParentID > 0
				select p.ParentID;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FULL(p_2.p) FULL(p_2.p_1) */"));
		}

		[Test]
		public void TableIDTest([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.QueryHint.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableHint(OracleHints.TableHint.Full).TableID("Pr")
				from c in db.Child.TableID("Ch")
				select p
			)
			.QueryHint(hint, Sql.TableSpec("Pr"), Sql.TableSpec("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ FULL(p) {hint}(p c_1) */"));
		}

		[Test]
		public void TableIDTest2([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.QueryHint.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in
							(
								from p in db.Parent.TableHint(OracleHints.TableHint.Full).TableID("Pr")
								from c in db.Child.TableID("Ch")
								select p
							)
							.AsSubQuery()
						select p
					)
					.AsSubQuery()
				select p
			)
			.QueryHint(hint, Sql.TableSpec("Pr"), Sql.TableSpec("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ FULL(p_2.p_1.p) {hint}(p_2.p_1.p p_2.p_1.c_1) */"));
		}

		[Test]
		public void TableIDTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.QueryHint.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in
							(
								from p in db.Parent.TableHint(OracleHints.TableHint.Full).TableID("Pr")
								where p.ParentID < 0
								select p
							)
							.AsSubQuery()
						select p
					)
					.AsSubQuery("qn")
				from c in db.Child.TableID("Ch")
				select p
			)
			.QueryHint(hint, Sql.TableSpec("Pr"), Sql.TableSpec("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ FULL(p_1.p@qn) {hint}(p_1.p@qn c_1) */"));
		}

		[Test]
		public void TableIDTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.QueryHint.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in
							(
								from p in db.Parent.TableHint(OracleHints.TableHint.Full).TableID("Pr")
								where p.ParentID < 0
								select p
							)
							.AsSubQuery("qn")
						from c in db.Child.TableID("Ch")
						select p
					)
					.AsSubQuery()
				select p
			)
			.QueryHint(hint, Sql.TableSpec("Pr"), Sql.TableSpec("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ FULL(p@qn) {hint}(p@qn p_2.c_1) */"));
		}

		[Test]
		public void TableInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from p in db.Parent
					from c in db.Child
					from c1 in db.Child.TableHint(OracleHints.TableHint.Full)
					where c.ParentID == p.ParentID && c1.ParentID == p.ParentID
					select p
				)
				.TablesInScopeHint(OracleHints.TableHint.NoCache);

			q =
				(
					from p in q
					from c in db.Child
					from p1 in db.Parent.TablesInScopeHint(OracleHints.TableHint.Parallel)
					where c.ParentID == p.ParentID && c.Parent!.ParentID > 0 && p1.ParentID == p.ParentID
					select p
				)
				.TablesInScopeHint(OracleHints.TableHint.Cluster);

			q =
				from p in q
				from c in db.Child
				where c.ParentID == p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ NOCACHE(p_2.p_1.t1.p) NOCACHE(p_2.p_1.t1.c_1) FULL(p_2.p_1.c1) NOCACHE(p_2.p_1.c1) PARALLEL(p_2.p1) CLUSTER(p_2.c_2) CLUSTER(p_2.a_Parent) */"));
		}

		[Test]
		public void DeleteTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child.TableHint(OracleHints.TableHint.Full)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(OracleHints.QueryHint.AllRows)
			.QueryHint(OracleHints.QueryHint.FirstRows(10))
			.Delete();

			Assert.That(LastQuery, Contains.Substring("DELETE /*+ FULL(c_1) ALL_ROWS FIRST_ROWS(10) */"));
		}

		[Test]
		public void InsertTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child.TableHint(OracleHints.TableHint.Full)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(OracleHints.QueryHint.AllRows)
			.QueryHint(OracleHints.QueryHint.FirstRows(10))
			.Insert(db.Child, c => new()
			{
				ChildID = c.ChildID * 2
			});

			Assert.That(LastQuery, Contains.Substring("INSERT /*+ FULL(c_1) ALL_ROWS FIRST_ROWS(10) */ INTO "));
		}

		[Test]

		public void UpdateTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child//.TableHint(Hints.TableHint.Full)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(OracleHints.QueryHint.AllRows)
			.QueryHint(OracleHints.QueryHint.FirstRows(10))
			.Update(db.Child, c => new()
			{
				ChildID = c.ChildID * 2
			});

			Assert.That(LastQuery, Contains.Substring("UPDATE /*+ ALL_ROWS FIRST_ROWS(10) */"));
		}

		[Test]
		public void MergeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			db.Parent1
				.Merge()
				.Using
				(
					(
						from c in db.Parent1.TableHint(OracleHints.TableHint.Full)
						where c.ParentID < -1111
						select c
					)
					.QueryHint(OracleHints.QueryHint.AllRows)
					.QueryHint(OracleHints.QueryHint.FirstRows(10))
				)
				.OnTargetKey()
				.UpdateWhenMatched()
				.Merge();

			Assert.That(LastQuery, Contains.Substring("MERGE /*+ FULL(c_1) ALL_ROWS FIRST_ROWS(10) */ INTO"));
		}

		[Test]
		public void CteTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var cte =
				(
					from c in db.Child.TableHint(OracleHints.TableHint.Full)
					where c.ParentID < -1111
					select c
				)
				.QueryHint(OracleHints.QueryHint.FirstRows(10))
				.TablesInScopeHint(OracleHints.TableHint.NoCache)
				.AsCte();

			var q =
				(
					from p in cte
					from c in db.Child
					select p
				)
				.QueryHint(OracleHints.QueryHint.AllRows)
				.TablesInScopeHint(OracleHints.TableHint.Fact);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("\tSELECT /*+ FULL(c_1) NOCACHE(c_1) */"));
			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FACT(c_2) FIRST_ROWS(10) ALL_ROWS */"));
		}
	}
}
