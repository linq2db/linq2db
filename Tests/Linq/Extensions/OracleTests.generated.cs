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
		public void TableHintFullTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracle()
					.FullHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Full}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Full}(p) {OracleHints.Table.Full}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Cluster}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Cluster}(p) {OracleHints.Table.Cluster}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Hash}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Hash}(p) {OracleHints.Table.Hash}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Index}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexAsc}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexCombine}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexJoin}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexDesc}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexFFS}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexFastFullScan}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexSS}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexSkipScan}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexSSAsc}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexSkipScanAsc}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexSSDesc}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.IndexSkipScanDesc}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoIndex}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoIndexFFS}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoIndexFastFullScan}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoIndexSS}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoIndexSkipScan}(p parent_ix parent2_ix) */"));
		}

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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.AllRows} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.FirstRows(10)} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoQueryTransformation} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.UseConcat} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.UseConcat}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoExpand} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.NoExpand}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.Rewrite} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.Rewrite}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoRewrite} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.NoRewrite}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.Merge} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.Merge}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Merge}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Merge}(p) {OracleHints.Table.Merge}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoMerge} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.NoMerge}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoMerge}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoMerge}(p) {OracleHints.Table.NoMerge}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.StarTransformation} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.StarTransformation}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoStarTransformation} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.NoStarTransformation}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Fact}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Fact}(p) {OracleHints.Table.Fact}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoFact}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoFact}(p) {OracleHints.Table.NoFact}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.Unnest} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.Unnest}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoUnnest} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.NoUnnest}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.Leading}(c_1 p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.Ordered} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.UseNL}(c_1 p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.UseNestedLoop}(c_1 p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoUseNL}(c_1 p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoUseNestedLoop}(c_1 p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.UseMerge}(c_1 p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoUseMerge}(c_1 p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.UseHash}(c_1 p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoUseHash}(c_1 p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.UseNestedLoopWithIndex}(p parent_ix parent2_ix) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.Parallel} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoParallel}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoParallel}(p) {OracleHints.Table.NoParallel}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.Append} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoAppend} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Cache}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Cache}(p) {OracleHints.Table.Cache}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoCache}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoCache}(p) {OracleHints.Table.NoCache}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.PushPredicate} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.PushPredicate}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.PushPredicate}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.PushPredicate}(p) {OracleHints.Table.PushPredicate}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.PushSubQueries}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ QB_NAME(qb) {OracleHints.Query.NoPushSubQueries}(@qb) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.CursorSharingExact} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.DrivingSite}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.DrivingSite}(p) {OracleHints.Table.DrivingSite}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.ModelMinAnalysis} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.PxJoinFilter}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.PxJoinFilter}(p) {OracleHints.Table.PxJoinFilter}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoPxJoinFilter}(p) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.NoPxJoinFilter}(p) {OracleHints.Table.NoPxJoinFilter}(c_1) */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoXmlQueryRewrite} */"));
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

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.NoXmlIndexRewrite} */"));
		}

	}
}
