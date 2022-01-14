// Generated.
//
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.DataProvider.Oracle;

using NUnit.Framework;

namespace Tests.Extensions
{
	partial class OracleTests
	{
		[Test]
		public void QueryHintAllRowsTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.AllRowsHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.AllRows} */"));
		}

		[Test]
		public void QueryHintFirstRowsTest2([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.FirstRowsHint(10);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.FirstRows(10)} */"));
		}

		[Test]
		public void TableHintClusterTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.ClusterHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Cluster}(p) */"));
		}

		[Test]
		public void TableHintClusterInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.ClusterInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Cluster}(p) {OracleHints.Hint.Cluster}(c_1) */"));
		}

		[Test]
		public void QueryHintClusteringTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.ClusteringHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Clustering} */"));
		}

		[Test]
		public void QueryHintNoClusteringTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoClusteringHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoClustering} */"));
		}

		[Test]
		public void TableHintFullTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.FullHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Full}(p) */"));
		}

		[Test]
		public void TableHintFullInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.FullInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Full}(p) {OracleHints.Hint.Full}(c_1) */"));
		}

		[Test]
		public void TableHintHashTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.HashHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Hash}(p) */"));
		}

		[Test]
		public void TableHintHashInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.HashInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Hash}(p) {OracleHints.Hint.Hash}(c_1) */"));
		}

		[Test]
		public void IndexHintIndexTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Index}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexAscTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexAscHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexAsc}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexCombineTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexCombineHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexCombine}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexJoinTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexJoinHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexJoin}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexDescTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexDescHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexDesc}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexFFSTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexFFSHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexFFS}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexFastFullScanTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexFastFullScanHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexFastFullScan}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexSSTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexSSHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexSS}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexSkipScanTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexSkipScanHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexSkipScan}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexSSAscTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexSSAscHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexSSAsc}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexSkipScanAscTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexSkipScanAscHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexSkipScanAsc}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexSSDescTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexSSDescHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexSSDesc}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexSkipScanDescTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.IndexSkipScanDescHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.IndexSkipScanDesc}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void QueryHintNativeFullOuterJoinTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NativeFullOuterJoinHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NativeFullOuterJoin} */"));
		}

		[Test]
		public void QueryHintNoNativeFullOuterJoinTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoNativeFullOuterJoinHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoNativeFullOuterJoin} */"));
		}

		[Test]
		public void IndexHintNoIndexTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoIndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoIndex}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoIndexFFSTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoIndexFFSHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoIndexFFS}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoIndexFastFullScanTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoIndexFastFullScanHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoIndexFastFullScan}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoIndexSSTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoIndexSSHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoIndexSS}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoIndexSkipScanTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoIndexSkipScanHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoIndexSkipScan}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void TableHintInMemoryTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.InMemoryHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.InMemory}(p) */"));
		}

		[Test]
		public void TableHintInMemoryInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.InMemoryInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.InMemory}(p) {OracleHints.Hint.InMemory}(c_1) */"));
		}

		[Test]
		public void TableHintNoInMemoryTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoInMemoryHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoInMemory}(p) */"));
		}

		[Test]
		public void TableHintNoInMemoryInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.NoInMemoryInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoInMemory}(p) {OracleHints.Hint.NoInMemory}(c_1) */"));
		}

		[Test]
		public void TableHintInMemoryPruningTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.InMemoryPruningHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.InMemoryPruning}(p) */"));
		}

		[Test]
		public void TableHintInMemoryPruningInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.InMemoryPruningInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.InMemoryPruning}(p) {OracleHints.Hint.InMemoryPruning}(c_1) */"));
		}

		[Test]
		public void TableHintNoInMemoryPruningTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoInMemoryPruningHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoInMemoryPruning}(p) */"));
		}

		[Test]
		public void TableHintNoInMemoryPruningInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.NoInMemoryPruningInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoInMemoryPruning}(p) {OracleHints.Hint.NoInMemoryPruning}(c_1) */"));
		}

		[Test]
		public void QueryHintUseBandTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.UseBandHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.UseBand}(c_1 p) */"));
		}

		[Test]
		public void QueryHintNoUseBandTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoUseBandHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoUseBand}(c_1 p) */"));
		}

		[Test]
		public void QueryHintUseCubeTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.UseCubeHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.UseCube}(c_1 p) */"));
		}

		[Test]
		public void QueryHintNoUseCubeTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoUseCubeHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoUseCube}(c_1 p) */"));
		}

		[Test]
		public void QueryHintUseHashTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.UseHashHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.UseHash}(c_1 p) */"));
		}

		[Test]
		public void QueryHintNoUseHashTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoUseHashHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoUseHash}(c_1 p) */"));
		}

		[Test]
		public void QueryHintUseMergeTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.UseMergeHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.UseMerge}(c_1 p) */"));
		}

		[Test]
		public void QueryHintNoUseMergeTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoUseMergeHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoUseMerge}(c_1 p) */"));
		}

		[Test]
		public void QueryHintUseNLTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.UseNLHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.UseNL}(c_1 p) */"));
		}

		[Test]
		public void QueryHintUseNestedLoopTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.UseNestedLoopHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.UseNestedLoop}(c_1 p) */"));
		}

		[Test]
		public void QueryHintNoUseNLTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoUseNLHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoUseNL}(c_1 p) */"));
		}

		[Test]
		public void QueryHintNoUseNestedLoopTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoUseNestedLoopHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoUseNestedLoop}(c_1 p) */"));
		}

		[Test]
		public void IndexHintUseNLWithIndexTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.UseNLWithIndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.UseNLWithIndex}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void IndexHintUseNestedLoopWithIndexTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.UseNestedLoopWithIndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.UseNestedLoopWithIndex}(p parent_ix parent2_ix) */"));
		}

		[Test]
		public void QueryHintEnableParallelDmlTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.EnableParallelDmlHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.EnableParallelDml} */"));
		}

		[Test]
		public void QueryHintDisableParallelDmlTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.DisableParallelDmlHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.DisableParallelDml} */"));
		}

		[Test]
		public void QueryHintPQConcurrentUnionTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.PQConcurrentUnionHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PQConcurrentUnion} */"));
		}

		[Test]
		public void QueryHintPQConcurrentUnionTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.PQConcurrentUnionHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.PQConcurrentUnion}(@qb) */"));
		}

		[Test]
		public void QueryHintNoPQConcurrentUnionTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoPQConcurrentUnionHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoPQConcurrentUnion} */"));
		}

		[Test]
		public void QueryHintNoPQConcurrentUnionTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.NoPQConcurrentUnionHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.NoPQConcurrentUnion}(@qb) */"));
		}

		[Test]
		public void QueryHintPQFilterSerialTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.PQFilterSerialHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PQFilterSerial} */"));
		}

		[Test]
		public void QueryHintPQFilterNoneTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.PQFilterNoneHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PQFilterNone} */"));
		}

		[Test]
		public void QueryHintPQFilterHashTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.PQFilterHashHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PQFilterHash} */"));
		}

		[Test]
		public void QueryHintPQFilterRandomTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.PQFilterRandomHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PQFilterRandom} */"));
		}

		[Test]
		public void TableHintPQSkewTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.PQSkewHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PQSkew}(p) */"));
		}

		[Test]
		public void TableHintPQSkewInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.PQSkewInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PQSkew}(p) {OracleHints.Hint.PQSkew}(c_1) */"));
		}

		[Test]
		public void TableHintNoPQSkewTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoPQSkewHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoPQSkew}(p) */"));
		}

		[Test]
		public void TableHintNoPQSkewInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.NoPQSkewInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoPQSkew}(p) {OracleHints.Hint.NoPQSkew}(c_1) */"));
		}

		[Test]
		public void QueryHintNoQueryTransformationTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoQueryTransformationHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoQueryTransformation} */"));
		}

		[Test]
		public void QueryHintUseConcatTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.UseConcatHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.UseConcat} */"));
		}

		[Test]
		public void QueryHintUseConcatTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.UseConcatHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.UseConcat}(@qb) */"));
		}

		[Test]
		public void QueryHintNoExpandTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoExpandHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoExpand} */"));
		}

		[Test]
		public void QueryHintNoExpandTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.NoExpandHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.NoExpand}(@qb) */"));
		}

		[Test]
		public void QueryHintRewriteTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.RewriteHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Rewrite} */"));
		}

		[Test]
		public void QueryHintRewriteTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.RewriteHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.Rewrite}(@qb) */"));
		}

		[Test]
		public void QueryHintNoRewriteTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoRewriteHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoRewrite} */"));
		}

		[Test]
		public void QueryHintNoRewriteTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.NoRewriteHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.NoRewrite}(@qb) */"));
		}

		[Test]
		public void QueryHintMergeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.MergeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Merge} */"));
		}

		[Test]
		public void QueryHintMergeTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.MergeHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.Merge}(@qb) */"));
		}

		[Test]
		public void TableHintMergeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.MergeHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Merge}(p) */"));
		}

		[Test]
		public void TableHintMergeInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.MergeInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Merge}(p) {OracleHints.Hint.Merge}(c_1) */"));
		}

		[Test]
		public void QueryHintNoMergeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoMergeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoMerge} */"));
		}

		[Test]
		public void QueryHintNoMergeTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.NoMergeHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.NoMerge}(@qb) */"));
		}

		[Test]
		public void TableHintNoMergeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoMergeHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoMerge}(p) */"));
		}

		[Test]
		public void TableHintNoMergeInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.NoMergeInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoMerge}(p) {OracleHints.Hint.NoMerge}(c_1) */"));
		}

		[Test]
		public void QueryHintStarTransformationTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.StarTransformationHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.StarTransformation} */"));
		}

		[Test]
		public void QueryHintStarTransformationTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.StarTransformationHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.StarTransformation}(@qb) */"));
		}

		[Test]
		public void QueryHintNoStarTransformationTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoStarTransformationHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoStarTransformation} */"));
		}

		[Test]
		public void QueryHintNoStarTransformationTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.NoStarTransformationHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.NoStarTransformation}(@qb) */"));
		}

		[Test]
		public void TableHintFactTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.FactHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Fact}(p) */"));
		}

		[Test]
		public void TableHintFactInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.FactInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Fact}(p) {OracleHints.Hint.Fact}(c_1) */"));
		}

		[Test]
		public void TableHintNoFactTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoFactHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoFact}(p) */"));
		}

		[Test]
		public void TableHintNoFactInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.NoFactInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoFact}(p) {OracleHints.Hint.NoFact}(c_1) */"));
		}

		[Test]
		public void QueryHintUnnestTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.UnnestHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Unnest} */"));
		}

		[Test]
		public void QueryHintUnnestTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.UnnestHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.Unnest}(@qb) */"));
		}

		[Test]
		public void QueryHintNoUnnestTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoUnnestHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoUnnest} */"));
		}

		[Test]
		public void QueryHintNoUnnestTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.NoUnnestHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.NoUnnest}(@qb) */"));
		}

		[Test]
		public void QueryHintLeadingTest4([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.LeadingHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Leading}(c_1 p) */"));
		}

		[Test]
		public void QueryHintOrderedTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.OrderedHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Ordered} */"));
		}

		[Test]
		public void QueryHintParallelTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.ParallelHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Parallel} */"));
		}

		[Test]
		public void TableHintNoParallelTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoParallelHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoParallel}(p) */"));
		}

		[Test]
		public void TableHintNoParallelInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.NoParallelInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoParallel}(p) {OracleHints.Hint.NoParallel}(c_1) */"));
		}

		[Test]
		public void QueryHintAppendTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.AppendHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Append} */"));
		}

		[Test]
		public void QueryHintAppendValuesTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.AppendValuesHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.AppendValues} */"));
		}

		[Test]
		public void QueryHintNoAppendTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoAppendHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoAppend} */"));
		}

		[Test]
		public void TableHintCacheTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.CacheHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Cache}(p) */"));
		}

		[Test]
		public void TableHintCacheInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.CacheInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Cache}(p) {OracleHints.Hint.Cache}(c_1) */"));
		}

		[Test]
		public void TableHintNoCacheTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoCacheHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoCache}(p) */"));
		}

		[Test]
		public void TableHintNoCacheInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.NoCacheInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoCache}(p) {OracleHints.Hint.NoCache}(c_1) */"));
		}

		[Test]
		public void QueryHintPushPredicateTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.PushPredicateHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PushPredicate} */"));
		}

		[Test]
		public void QueryHintPushPredicateTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.PushPredicateHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.PushPredicate}(@qb) */"));
		}

		[Test]
		public void TableHintPushPredicateTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.PushPredicateHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PushPredicate}(p) */"));
		}

		[Test]
		public void TableHintPushPredicateInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.PushPredicateInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PushPredicate}(p) {OracleHints.Hint.PushPredicate}(c_1) */"));
		}

		[Test]
		public void QueryHintPushSubQueriesTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.PushSubQueriesHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.PushSubQueries}(@qb) */"));
		}

		[Test]
		public void QueryHintNoPushSubQueriesTest3([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryName("qb")
			.AsOracle()
			.NoPushSubQueriesHint("@qb");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Hint.NoPushSubQueries}(@qb) */"));
		}

		[Test]
		public void QueryHintCursorSharingExactTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.CursorSharingExactHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.CursorSharingExact} */"));
		}

		[Test]
		public void TableHintDrivingSiteTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.DrivingSiteHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.DrivingSite}(p) */"));
		}

		[Test]
		public void TableHintDrivingSiteInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.DrivingSiteInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.DrivingSite}(p) {OracleHints.Hint.DrivingSite}(c_1) */"));
		}

		[Test]
		public void QueryHintModelMinAnalysisTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.ModelMinAnalysisHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.ModelMinAnalysis} */"));
		}

		[Test]
		public void TableHintPxJoinFilterTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.PxJoinFilterHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PxJoinFilter}(p) */"));
		}

		[Test]
		public void TableHintPxJoinFilterInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.PxJoinFilterInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.PxJoinFilter}(p) {OracleHints.Hint.PxJoinFilter}(c_1) */"));
		}

		[Test]
		public void TableHintNoPxJoinFilterTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.NoPxJoinFilterHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoPxJoinFilter}(p) */"));
		}

		[Test]
		public void TableHintNoPxJoinFilterInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracle()
			.NoPxJoinFilterInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoPxJoinFilter}(p) {OracleHints.Hint.NoPxJoinFilter}(c_1) */"));
		}

		[Test]
		public void QueryHintNoXmlQueryRewriteTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoXmlQueryRewriteHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoXmlQueryRewrite} */"));
		}

		[Test]
		public void QueryHintNoXmlIndexRewriteTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoXmlIndexRewriteHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoXmlIndexRewrite} */"));
		}

		[Test]
		public void QueryHintFreshMaterializedViewTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.FreshMaterializedViewHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.FreshMaterializedView} */"));
		}

		[Test]
		public void QueryHintFreshMVTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.FreshMVHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.FreshMV} */"));
		}

		[Test]
		public void QueryHintGroupingTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.GroupingHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Grouping} */"));
		}

		[Test]
		public void QueryHintMonitorTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.MonitorHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.Monitor} */"));
		}

		[Test]
		public void QueryHintNoMonitorTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracle()
			.NoMonitorHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Hint.NoMonitor} */"));
		}

	}
}
