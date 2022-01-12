using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.Oracle;

using NUnit.Framework;

namespace Tests.Extensions
{
	[TestFixture]
	public partial class OracleTests : TestBase
	{
		[Test]
		public void TableSubqueryHintTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in
					(
						from p in db.Parent.TableHint(OracleHints.Table.Full).With(OracleHints.Table.Cache)
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
								from p in db.Parent.TableHint(OracleHints.Table.Full)
								from c in db.Child.TableHint(OracleHints.Table.Full)
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
				OracleHints.Table.Cache,
				OracleHints.Table.Cluster,
				OracleHints.Table.DrivingSite,
				OracleHints.Table.Fact,
				OracleHints.Table.Full,
				OracleHints.Table.Hash,
				OracleHints.Table.NoCache,
				OracleHints.Table.NoFact,
				OracleHints.Table.NoParallel,
				OracleHints.Table.NoPxJoinFilter,
				OracleHints.Table.PxJoinFilter
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
				from p in db.Parent.TableHint(OracleHints.Table.DynamicSampling, 1)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ DYNAMIC_SAMPLING(p 1)"));
		}

		[Test]
		public void TableHintParallelTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(OracleHints.Table.Parallel, (object)",", 5)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(p , 5)"));
		}

		[Test]
		public void TableHintParallelDefaultTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(OracleHints.Table.Parallel, "DEFAULT")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(p DEFAULT)"));
		}

		[Test]
		public void IndexHintSingleTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(OracleHints.Table.Index, "parent_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ INDEX(p parent_ix)"));
		}

		[Test]
		public void IndexHintTest([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.Table.Index,
				OracleHints.Table.IndexAsc,
				OracleHints.Table.IndexCombine,
				OracleHints.Table.IndexDesc,
				OracleHints.Table.IndexFastFullScan,
				OracleHints.Table.IndexJoin,
				OracleHints.Table.IndexSkipScan,
				OracleHints.Table.IndexSkipScanAsc,
				OracleHints.Table.IndexSkipScanDesc,
				OracleHints.Table.NoIndex,
				OracleHints.Table.NoIndexFastFullScan,
				OracleHints.Table.NoIndexSkipScan,
				OracleHints.Table.NoParallelIndex,
				OracleHints.Table.ParallelIndex,
				OracleHints.Table.UseNLWithIndex
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
				OracleHints.Query.AllRows,
				OracleHints.Query.Append,
				OracleHints.Query.CursorSharingExact,
				OracleHints.Query.ModelMinAnalysis,
				OracleHints.Query.NoAppend,
				OracleHints.Query.NoExpand,
				OracleHints.Query.NoRewrite,
				OracleHints.Query.NoQueryTransformation,
				OracleHints.Query.NoStarTransformation,
				OracleHints.Query.NoUnnest,
				OracleHints.Query.NoXmlQueryRewrite,
				OracleHints.Query.Ordered,
				OracleHints.Query.PushSubQueries,
				OracleHints.Query.StarTransformation,
				OracleHints.Query.Unnest,
				OracleHints.Query.UseConcat
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
				OracleHints.Query.NoUnnest
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
		public void QueryHintFirstRowsTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(OracleHints.Query.FirstRows(25));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FIRST_ROWS(25) */"));
		}

		[Test]
		public void UnionTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q1 =
				from c in db.Child
				join p in db.Parent.TableHint(OracleHints.Table.Full) on c.ParentID equals p.ParentID
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
				join p in db.Parent.TableHint(OracleHints.Table.Full) on c.ParentID equals p.ParentID
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
				join p in db.Parent.TableHint(OracleHints.Table.Full) on c.ParentID equals p.ParentID
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
				OracleHints.Query.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableHint(OracleHints.Table.Full).TableID("Pr")
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
				OracleHints.Query.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in
							(
								from p in db.Parent.TableHint(OracleHints.Table.Full).TableID("Pr")
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
				OracleHints.Query.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in
							(
								from p in db.Parent.TableHint(OracleHints.Table.Full).TableID("Pr")
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
				OracleHints.Query.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in
							(
								from p in db.Parent.TableHint(OracleHints.Table.Full).TableID("Pr")
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
					from c1 in db.Child.TableHint(OracleHints.Table.Full)
					where c.ParentID == p.ParentID && c1.ParentID == p.ParentID
					select p
				)
				.TablesInScopeHint(OracleHints.Table.NoCache);

			q =
				(
					from p in q
					from c in db.Child
					from p1 in db.Parent.TablesInScopeHint(OracleHints.Table.Parallel)
					where c.ParentID == p.ParentID && c.Parent!.ParentID > 0 && p1.ParentID == p.ParentID
					select p
				)
				.TablesInScopeHint(OracleHints.Table.Cluster);

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
				from c in db.Child.TableHint(OracleHints.Table.Full)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(OracleHints.Query.AllRows)
			.QueryHint(OracleHints.Query.FirstRows(10))
			.Delete();

			Assert.That(LastQuery, Contains.Substring("DELETE /*+ FULL(c_1) ALL_ROWS FIRST_ROWS(10) */"));
		}

		[Test]
		public void InsertTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child.TableHint(OracleHints.Table.Full)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(OracleHints.Query.AllRows)
			.QueryHint(OracleHints.Query.FirstRows(10))
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
			.QueryHint(OracleHints.Query.AllRows)
			.QueryHint(OracleHints.Query.FirstRows(10))
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
						from c in db.Parent1.TableHint(OracleHints.Table.Full)
						where c.ParentID < -1111
						select c
					)
					.QueryHint(OracleHints.Query.AllRows)
					.QueryHint(OracleHints.Query.FirstRows(10))
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
					from c in db.Child.TableHint(OracleHints.Table.Full)
					where c.ParentID < -1111
					select c
				)
				.QueryHint(OracleHints.Query.FirstRows(10))
				.TablesInScopeHint(OracleHints.Table.NoCache)
				.AsCte();

			var q =
				(
					from p in cte
					from c in db.Child
					select p
				)
				.QueryHint(OracleHints.Query.AllRows)
				.TablesInScopeHint(OracleHints.Table.Fact);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("\tSELECT /*+ FULL(c_1) NOCACHE(c_1) */"));
			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FACT(c_2) FIRST_ROWS(10) ALL_ROWS */"));
		}

		[Test]
		public void QueryHintParallelDefaultTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					select p
				)
				.AsOracle()
				.ParallelDefaultHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(DEFAULT) */"));
		}

		[Test]
		public void QueryHintParallelAutoTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					select p
				)
				.AsOracle()
				.ParallelAutoHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(AUTO) */"));
		}

		[Test]
		public void QueryHintParallelManualTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					select p
				)
				.AsOracle()
				.ParallelManualHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(MANUAL) */"));
		}

		[Test]
		public void QueryHintParallelTest2([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					select p
				)
				.AsOracle()
				.ParallelHint(10);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(10) */"));
		}

		[Test]
		public void QueryHintParallelTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child.AsOracle().ParallelHint(5)
					join p in db.Parent on c.ParentID equals p.ParentID
					select p
				);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(c_1, 5) */"));
		}

		[Test]
		public void QueryHintParallelDefaultTest2([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child
					from p in db.Parent.AsOracle().PqDistributeHint("PARTITION", "NONE")
					where c.ParentID == p.ParentID
					select p
				);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PQ_DISTRIBUTE(p PARTITION, NONE) */"));
		}

		[Test]
		public void QueryHintParallelIndexTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child
					from p in db.Parent.AsOracle().ParallelIndexHint("index1", 3)
					where c.ParentID == p.ParentID
					select p
				);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL_INDEX(p, index1, 3) */"));
		}

		[Test]
		public void QueryHintParallelIndexDefaultTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child
					from p in db.Parent.AsOracle().ParallelIndexHint("index1", "DEFAULT")
					where c.ParentID == p.ParentID
					select p
				);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL_INDEX(p, index1, DEFAULT) */"));
		}

		[Test]
		public void QueryHintNoParallelIndexTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				from p in db.Parent.AsOracle().NoParallelIndexHint("index1")
				where c.ParentID == p.ParentID
				select p
			);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ NO_PARALLEL_INDEX(p, index1) */"));
		}

		[Test]
		public void TableHintDynamicSamplingTest2([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.DynamicSamplingHint(1)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.DynamicSampling}(p 1) */"));
		}
	}
}
