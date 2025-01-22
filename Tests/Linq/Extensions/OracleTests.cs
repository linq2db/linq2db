using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Mapping;

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
						from p in db.Parent.TableHint(OracleHints.Hint.Full).With(OracleHints.Hint.Cache)
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
								from p in db.Parent.TableHint(OracleHints.Hint.Full)
								from c in db.Child.TableHint(OracleHints.Hint.Full)
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
				OracleHints.Hint.Cache,
				OracleHints.Hint.Cluster,
				OracleHints.Hint.DrivingSite,
				OracleHints.Hint.Fact,
				OracleHints.Hint.Full,
				OracleHints.Hint.Hash,
				OracleHints.Hint.NoCache,
				OracleHints.Hint.NoFact,
				OracleHints.Hint.NoParallel,
				OracleHints.Hint.NoPxJoinFilter,
				OracleHints.Hint.PxJoinFilter
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
				from p in db.Parent.TableHint(OracleHints.Hint.DynamicSampling, 1)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ DYNAMIC_SAMPLING(p 1)"));
		}

		[Test]
		public void TableHintParallelTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(OracleHints.Hint.Parallel, (object)",", 5)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(p , 5)"));
		}

		[Test]
		public void TableHintParallelDefaultTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(OracleHints.Hint.Parallel, "DEFAULT")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ PARALLEL(p DEFAULT)"));
		}

		[Test]
		public void IndexHintSingleTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(OracleHints.Hint.Index, "parent_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ INDEX(p parent_ix)"));
		}

		[Test]
		public void IndexHintTest([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.Hint.Index,
				OracleHints.Hint.IndexAsc,
				OracleHints.Hint.IndexCombine,
				OracleHints.Hint.IndexDesc,
				OracleHints.Hint.IndexFastFullScan,
				OracleHints.Hint.IndexJoin,
				OracleHints.Hint.IndexSkipScan,
				OracleHints.Hint.IndexSkipScanAsc,
				OracleHints.Hint.IndexSkipScanDesc,
				OracleHints.Hint.NoIndex,
				OracleHints.Hint.NoIndexFastFullScan,
				OracleHints.Hint.NoIndexSkipScan,
				OracleHints.Hint.NoParallelIndex,
				OracleHints.Hint.ParallelIndex,
				OracleHints.Hint.UseNLWithIndex
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
				OracleHints.Hint.AllRows,
				OracleHints.Hint.Append,
				OracleHints.Hint.CursorSharingExact,
				OracleHints.Hint.ModelMinAnalysis,
				OracleHints.Hint.NoAppend,
				OracleHints.Hint.NoExpand,
				OracleHints.Hint.NoRewrite,
				OracleHints.Hint.NoQueryTransformation,
				OracleHints.Hint.NoStarTransformation,
				OracleHints.Hint.NoUnnest,
				OracleHints.Hint.NoXmlQueryRewrite,
				OracleHints.Hint.Ordered,
				OracleHints.Hint.PushSubQueries,
				OracleHints.Hint.StarTransformation,
				OracleHints.Hint.Unnest,
				OracleHints.Hint.UseConcat
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
				OracleHints.Hint.NoUnnest
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
			.QueryHint(OracleHints.Hint.FirstRows(25));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FIRST_ROWS(25) */"));
		}

		[Test]
		public void UnionTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q1 =
				from c in db.Child
				join p in db.Parent.TableHint(OracleHints.Hint.Full) on c.ParentID equals p.ParentID
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
				join p in db.Parent.TableHint(OracleHints.Hint.Full) on c.ParentID equals p.ParentID
				select p;

			var q =
				from p in q1.QueryName("qb_1").Union(q1.QueryName("qb_2"))
				where p.ParentID > 0
				select p.ParentID;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FULL(p@qb_1) FULL(p_2@qb_2) */"));
			Assert.That(LastQuery, Contains.Substring("SELECT /*+ QB_NAME(qb_1) */"));
			Assert.That(LastQuery, Contains.Substring("SELECT /*+ QB_NAME(qb_2) */"));
		}

		[Test]
		public void UnionTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q1 =
				from c in db.Child
				join p in db.Parent.TableHint(OracleHints.Hint.Full) on c.ParentID equals p.ParentID
				select p;

			var q =
				from p in q1.Union(q1)
				where p.ParentID > 0
				select p.ParentID;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FULL(p_3.p) FULL(p_3.p_2) */"));
		}

		[Test]
		public void TableIDTest([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				OracleHints.Hint.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableHint(OracleHints.Hint.Full).TableID("Pr")
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
				OracleHints.Hint.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in
							(
								from p in db.Parent.TableHint(OracleHints.Hint.Full).TableID("Pr")
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
				OracleHints.Hint.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in
							(
								from p in db.Parent.TableHint(OracleHints.Hint.Full).TableID("Pr")
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
				OracleHints.Hint.Leading
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in
							(
								from p in db.Parent.TableHint(OracleHints.Hint.Full).TableID("Pr")
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
					from c1 in db.Child.TableHint(OracleHints.Hint.Full)
					where c.ParentID == p.ParentID && c1.ParentID == p.ParentID
					select p
				)
				.TablesInScopeHint(OracleHints.Hint.NoCache);

			q =
				(
					from p in q
					from c in db.Child
					from p1 in db.Parent.TablesInScopeHint(OracleHints.Hint.Parallel)
					where c.ParentID == p.ParentID && c.Parent!.ParentID > 0 && p1.ParentID == p.ParentID
					select p
				)
				.TablesInScopeHint(OracleHints.Hint.Cluster);

			q =
				from p in q
				from c in db.Child
				where c.ParentID == p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ NOCACHE(p) NOCACHE(c_1) FULL(c1) NOCACHE(c1) CLUSTER(c_2) CLUSTER(a_Parent) PARALLEL(p1) */"));
		}

		[Test]
		public void DeleteTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child.TableHint(OracleHints.Hint.Full)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(OracleHints.Hint.AllRows)
			.QueryHint(OracleHints.Hint.FirstRows(10))
			.Delete();

			Assert.That(LastQuery, Contains.Substring("DELETE /*+ FULL(c_1) ALL_ROWS FIRST_ROWS(10) */"));
		}

		[Test]
		public void InsertTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child.TableHint(OracleHints.Hint.Full)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(OracleHints.Hint.AllRows)
			.QueryHint(OracleHints.Hint.FirstRows(10))
			.Insert(db.Child, c => new()
			{
				ChildID = c.ChildID * 2
			});

			Assert.That(LastQuery, Contains.Substring("INSERT /*+ FULL(c_1) ALL_ROWS FIRST_ROWS(10) */ INTO "));
		}

		[Obsolete("Remove test after API removed")]
		[Test]
		public void UpdateTestOld([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child//.TableHint(Hints.TableHint.Full)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(OracleHints.Hint.AllRows)
			.QueryHint(OracleHints.Hint.FirstRows(10))
			.Update(db.Child, c => new()
			{
				ChildID = c.ChildID * 2
			});

			Assert.That(LastQuery, Contains.Substring("UPDATE /*+ ALL_ROWS FIRST_ROWS(10) */"));
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
			.QueryHint(OracleHints.Hint.AllRows)
			.QueryHint(OracleHints.Hint.FirstRows(10))
			.Update(q => q, c => new()
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
						from c in db.Parent1.TableHint(OracleHints.Hint.Full)
						where c.ParentID < -1111
						select c
					)
					.QueryHint(OracleHints.Hint.AllRows)
					.QueryHint(OracleHints.Hint.FirstRows(10))
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
					from c in db.Child.TableHint(OracleHints.Hint.Full)
					where c.ParentID < -1111
					select c
				)
				.QueryHint(OracleHints.Hint.FirstRows(10))
				.TablesInScopeHint(OracleHints.Hint.NoCache)
				.AsCte();

			var q =
				(
					from p in cte
					from c in db.Child
					select p
				)
				.QueryHint(OracleHints.Hint.AllRows)
				.TablesInScopeHint(OracleHints.Hint.Fact);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("\tSELECT /*+ FULL(c_1) NOCACHE(c_1) */").Using(StringComparison.Ordinal));
			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FACT(c_2) ALL_ROWS FIRST_ROWS(10) */"));
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
					from p in db.Parent.AsOracle().PQDistributeHint("PARTITION", "NONE")
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.DynamicSampling}(p 1) */"));
		}

		[Test]
		public void QueryHintContainersTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					select p
				)
				.AsOracle()
				.ContainersHint(OracleHints.Hint.NoParallel);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ CONTAINERS(DEFAULT_PDB_HINT='NO_PARALLEL') */"));
		}

		[Test]
		public void QueryHintOptParamTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					select p
				)
				.AsOracle()
				.OptParamHint("'star_transformation_enabled'", "'true'");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ OPT_PARAM('star_transformation_enabled' 'true') */"));
		}

		[Test]
		public void OracleUnionTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from p in db.Parent.TableID("cc")
					select p
				)
				.AsOracle()
				.Union
				(
					from p in db.Child
					select p.Parent
				)
				.Union
				(
					from p in db.Parent
					from c in db.Child.TableID("pp")
						.AsSubQuery()
						.AsOracle()
					select p
				)
				.AsOracle()
				.ContainersHint(OracleHints.Hint.NoParallel);

			_ = q.ToList();


			Assert.That(LastQuery, Should.Contain(
				"SELECT /*+ CONTAINERS(DEFAULT_PDB_HINT='NO_PARALLEL')",
				"UNION"));
		}

		#region Issue 4163

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4163")]
		public void Issue4163Test1([IncludeDataSources(true, TestProvName.AllOracle)] string context, [Values] CompareNulls compareNulls)
		{
			using var db = GetDataContext(context, o => o.UseCompareNulls(compareNulls));
			using var tb = db.CreateLocalTable(Issue4163TableExplicitNullability.Data);

			var cnt = db.GetTable<Issue4163TableExplicitNullability>().Where(r => r.Method != PaymentMethod.Unknown).Count();

			Assert.That(cnt, Is.EqualTo(2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4163")]
		public void Issue4163Test2([IncludeDataSources(true, TestProvName.AllOracle)] string context, [Values] CompareNulls compareNulls)
		{
			using var db = GetDataContext(context, o => o.UseCompareNulls(compareNulls));
			using var tb = db.CreateLocalTable(Issue4163TableExplicitNullability.Data);

			var cnt = db.GetTable<Issue4163TableUnknownNullability>().Where(r => r.Method != PaymentMethod.Unknown).Count();

			Assert.That(cnt, Is.EqualTo(2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4163")]
		public void Issue4163Test3([IncludeDataSources(true, TestProvName.AllOracle)] string context, [Values] CompareNulls compareNulls)
		{
			using var db = GetDataContext(context, o => o.UseCompareNulls(compareNulls));
			using var tb = db.CreateLocalTable(Issue4163TableExplicitNullability.Data);

			var cnt = db.GetTable<Issue4163TableInferredNullability>().Where(r => r.Method != PaymentMethodWithNull.Unknown).Count();

			Assert.That(cnt, Is.EqualTo(2));
		}

		[Table("Issue4163Table")]
		sealed class Issue4163TableExplicitNullability
		{
			[Column] public int Id { get; set; }
			[Column(CanBeNull = true)] public PaymentMethod Method { get; set; }

			public static readonly Issue4163TableExplicitNullability[] Data = new[]
			{
				new Issue4163TableExplicitNullability() { Id = 1, Method = PaymentMethod.Unknown },
				new Issue4163TableExplicitNullability() { Id = 2, Method = PaymentMethod.Cheque },
				new Issue4163TableExplicitNullability() { Id = 3, Method = PaymentMethod.EFT },
			};
		}

		[Table("Issue4163Table")]
		sealed class Issue4163TableUnknownNullability
		{
			[Column] public int Id { get; set; }
			[Column] public PaymentMethod Method { get; set; }
		}

		[Table("Issue4163Table")]
		sealed class Issue4163TableInferredNullability
		{
			[Column] public int Id { get; set; }
			[Column] public PaymentMethodWithNull Method { get; set; }
		}

		enum PaymentMethod
		{
			[MapValue("")]
			Unknown,

			[MapValue("C")]
			Cheque,

			[MapValue("E")]
			EFT
		}

		enum PaymentMethodWithNull
		{
			[MapValue("")]
			Unknown,

			[MapValue(null)]
			Null = Unknown,

			[MapValue("C")]
			Cheque,

			[MapValue("E")]
			EFT
		}

		#endregion
	}
}
