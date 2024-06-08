// Generated.
//
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.DataProvider.MySql;

using NUnit.Framework;

namespace Tests.Extensions
{
	partial class MySqlTests
	{
		[Test]
		public void TableHintJoinFixedOrderTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.JoinFixedOrderHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.JoinFixedOrder}(p) */"));
		}

		[Test]
		public void TableHintJoinFixedOrderInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.JoinFixedOrderInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.JoinFixedOrder}(p) {MySqlHints.Table.JoinFixedOrder}(c_1) */"));
		}

		[Test]
		public void QueryHintJoinFixedOrderTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.JoinFixedOrderHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.JoinFixedOrder}(c_1, p) */"));
		}

		[Test]
		public void TableHintJoinOrderTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.JoinOrderHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.JoinOrder}(p) */"));
		}

		[Test]
		public void TableHintJoinOrderInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.JoinOrderInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.JoinOrder}(p) {MySqlHints.Table.JoinOrder}(c_1) */"));
		}

		[Test]
		public void QueryHintJoinOrderTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.JoinOrderHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.JoinOrder}(c_1, p) */"));
		}

		[Test]
		public void TableHintJoinPrefixTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.JoinPrefixHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.JoinPrefix}(p) */"));
		}

		[Test]
		public void TableHintJoinPrefixInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.JoinPrefixInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.JoinPrefix}(p) {MySqlHints.Table.JoinPrefix}(c_1) */"));
		}

		[Test]
		public void QueryHintJoinPrefixTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.JoinPrefixHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.JoinPrefix}(c_1, p) */"));
		}

		[Test]
		public void TableHintJoinSuffixTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.JoinSuffixHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.JoinSuffix}(p) */"));
		}

		[Test]
		public void TableHintJoinSuffixInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.JoinSuffixInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.JoinSuffix}(p) {MySqlHints.Table.JoinSuffix}(c_1) */"));
		}

		[Test]
		public void QueryHintJoinSuffixTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.JoinSuffixHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.JoinSuffix}(c_1, p) */"));
		}

		[Test]
		public void TableHintBkaTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.BkaHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.Bka}(p) */"));
		}

		[Test]
		public void TableHintBkaInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.BkaInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.Bka}(p) {MySqlHints.Table.Bka}(c_1) */"));
		}

		[Test]
		public void QueryHintBkaTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.BkaHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.Bka}(c_1, p) */"));
		}

		[Test]
		public void TableHintBatchedKeyAccessTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.BatchedKeyAccessHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.BatchedKeyAccess}(p) */"));
		}

		[Test]
		public void TableHintBatchedKeyAccessInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.BatchedKeyAccessInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.BatchedKeyAccess}(p) {MySqlHints.Table.BatchedKeyAccess}(c_1) */"));
		}

		[Test]
		public void QueryHintBatchedKeyAccessTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.BatchedKeyAccessHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.BatchedKeyAccess}(c_1, p) */"));
		}

		[Test]
		public void TableHintNoBkaTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoBkaHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoBka}(p) */"));
		}

		[Test]
		public void TableHintNoBkaInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.NoBkaInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoBka}(p) {MySqlHints.Table.NoBka}(c_1) */"));
		}

		[Test]
		public void QueryHintNoBkaTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.NoBkaHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.NoBka}(c_1, p) */"));
		}

		[Test]
		public void TableHintNoBatchedKeyAccessTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoBatchedKeyAccessHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoBatchedKeyAccess}(p) */"));
		}

		[Test]
		public void TableHintNoBatchedKeyAccessInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.NoBatchedKeyAccessInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoBatchedKeyAccess}(p) {MySqlHints.Table.NoBatchedKeyAccess}(c_1) */"));
		}

		[Test]
		public void QueryHintNoBatchedKeyAccessTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.NoBatchedKeyAccessHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.NoBatchedKeyAccess}(c_1, p) */"));
		}

		[Test]
		public void TableHintBnlTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.BnlHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.Bnl}(p) */"));
		}

		[Test]
		public void TableHintBnlInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.BnlInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.Bnl}(p) {MySqlHints.Table.Bnl}(c_1) */"));
		}

		[Test]
		public void QueryHintBnlTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.BnlHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.Bnl}(c_1, p) */"));
		}

		[Test]
		public void TableHintBlockNestedLoopTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.BlockNestedLoopHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.BlockNestedLoop}(p) */"));
		}

		[Test]
		public void TableHintBlockNestedLoopInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.BlockNestedLoopInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.BlockNestedLoop}(p) {MySqlHints.Table.BlockNestedLoop}(c_1) */"));
		}

		[Test]
		public void QueryHintBlockNestedLoopTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.BlockNestedLoopHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.BlockNestedLoop}(c_1, p) */"));
		}

		[Test]
		public void TableHintNoBnlTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoBnlHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoBnl}(p) */"));
		}

		[Test]
		public void TableHintNoBnlInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.NoBnlInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoBnl}(p) {MySqlHints.Table.NoBnl}(c_1) */"));
		}

		[Test]
		public void QueryHintNoBnlTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.NoBnlHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.NoBnl}(c_1, p) */"));
		}

		[Test]
		public void TableHintNoBlockNestedLoopTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoBlockNestedLoopHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoBlockNestedLoop}(p) */"));
		}

		[Test]
		public void TableHintNoBlockNestedLoopInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.NoBlockNestedLoopInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoBlockNestedLoop}(p) {MySqlHints.Table.NoBlockNestedLoop}(c_1) */"));
		}

		[Test]
		public void QueryHintNoBlockNestedLoopTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.NoBlockNestedLoopHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.NoBlockNestedLoop}(c_1, p) */"));
		}

		[Test]
		public void TableHintDerivedConditionPushDownTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.DerivedConditionPushDownHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.DerivedConditionPushDown}(p) */"));
		}

		[Test]
		public void TableHintDerivedConditionPushDownInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.DerivedConditionPushDownInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.DerivedConditionPushDown}(p) {MySqlHints.Table.DerivedConditionPushDown}(c_1) */"));
		}

		[Test]
		public void QueryHintDerivedConditionPushDownTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.DerivedConditionPushDownHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.DerivedConditionPushDown}(c_1, p) */"));
		}

		[Test]
		public void TableHintNoDerivedConditionPushDownTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoDerivedConditionPushDownHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoDerivedConditionPushDown}(p) */"));
		}

		[Test]
		public void TableHintNoDerivedConditionPushDownInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.NoDerivedConditionPushDownInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoDerivedConditionPushDown}(p) {MySqlHints.Table.NoDerivedConditionPushDown}(c_1) */"));
		}

		[Test]
		public void QueryHintNoDerivedConditionPushDownTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.NoDerivedConditionPushDownHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.NoDerivedConditionPushDown}(c_1, p) */"));
		}

		[Test]
		public void TableHintHashJoinTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.HashJoinHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.HashJoin}(p) */"));
		}

		[Test]
		public void TableHintHashJoinInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.HashJoinInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.HashJoin}(p) {MySqlHints.Table.HashJoin}(c_1) */"));
		}

		[Test]
		public void QueryHintHashJoinTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.HashJoinHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.HashJoin}(c_1, p) */"));
		}

		[Test]
		public void TableHintNoHashJoinTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoHashJoinHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoHashJoin}(p) */"));
		}

		[Test]
		public void TableHintNoHashJoinInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.NoHashJoinInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoHashJoin}(p) {MySqlHints.Table.NoHashJoin}(c_1) */"));
		}

		[Test]
		public void QueryHintNoHashJoinTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.NoHashJoinHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.NoHashJoin}(c_1, p) */"));
		}

		[Test]
		public void TableHintMergeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.MergeHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.Merge}(p) */"));
		}

		[Test]
		public void TableHintMergeInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.MergeInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.Merge}(p) {MySqlHints.Table.Merge}(c_1) */"));
		}

		[Test]
		public void QueryHintMergeTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.MergeHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.Merge}(c_1, p) */"));
		}

		[Test]
		public void TableHintNoMergeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoMergeHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoMerge}(p) */"));
		}

		[Test]
		public void TableHintNoMergeInScopeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.NoMergeInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoMerge}(p) {MySqlHints.Table.NoMerge}(c_1) */"));
		}

		[Test]
		public void QueryHintNoMergeTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child.TableID("cc")
				join p in db.Parent.TableID("pp") on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.NoMergeHint(Sql.TableSpec("cc"), Sql.TableSpec("pp"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.NoMerge}(c_1, p) */"));
		}

		[Test]
		public void IndexHintGroupIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.GroupIndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.GroupIndex}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoGroupIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoGroupIndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoGroupIndex}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.IndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.Index}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoIndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoIndex}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintIndexMergeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.IndexMergeHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.IndexMerge}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoIndexMergeTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoIndexMergeHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoIndexMerge}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintJoinIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.JoinIndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.JoinIndex}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoJoinIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoJoinIndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoJoinIndex}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintMrrTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.MrrHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.Mrr}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoMrrTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoMrrHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoMrr}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoIcpTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoIcpHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoIcp}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoRangeOptimizationTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoRangeOptimizationHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoRangeOptimization}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintOrderIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.OrderIndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.OrderIndex}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoOrderIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoOrderIndexHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoOrderIndex}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintSkipScanTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.SkipScanHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.SkipScan}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void IndexHintNoSkipScanTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsMySql()
					.NoSkipScanHint("parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Table.NoSkipScan}(p parent_ix, parent2_ix) */"));
		}

		[Test]
		public void QueryHintSemiJoinTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				from c in db.Child
				select p
			)
			.AsMySql()
				.SemiJoinHint("FIRSTMATCH", "LOOSESCAN");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.SemiJoin}(FIRSTMATCH, LOOSESCAN)"));
		}

		[Test]
		public void QueryHintNoSemiJoinTest4([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				from c in db.Child
				select p
			)
			.AsMySql()
				.NoSemiJoinHint("FIRSTMATCH", "LOOSESCAN");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.NoSemiJoin}(FIRSTMATCH, LOOSESCAN)"));
		}

		[Test]
		public void QueryHintMaxExecutionTimeTest2([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.MaxExecutionTimeHint(10);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.MaxExecutionTime(10)} */"));
		}

		[Test]
		public void QueryHintSetVarTest3([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.SetVarHint("aaa");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.SetVar}(aaa) */"));
		}

		[Test]
		public void QueryHintResourceGroupTest3([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsMySql()
			.ResourceGroupHint("aaa");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.ResourceGroup}(aaa) */"));
		}

		[Test]
		public void IndexHintUseIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.UseIndexHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.UseIndex}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintUseIndexForJoinTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.UseIndexForJoinHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.UseIndexForJoin}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintUseIndexForOrderByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.UseIndexForOrderByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.UseIndexForOrderBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintUseIndexForGroupByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.UseIndexForGroupByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.UseIndexForGroupBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintUseKeyTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.UseKeyHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.UseKey}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintUseKeyForJoinTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.UseKeyForJoinHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.UseKeyForJoin}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintUseKeyForOrderByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.UseKeyForOrderByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.UseKeyForOrderBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintUseKeyForGroupByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.UseKeyForGroupByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.UseKeyForGroupBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintIgnoreIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.IgnoreIndexHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.IgnoreIndex}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintIgnoreIndexForJoinTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.IgnoreIndexForJoinHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.IgnoreIndexForJoin}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintIgnoreIndexForOrderByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.IgnoreIndexForOrderByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.IgnoreIndexForOrderBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintIgnoreIndexForGroupByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.IgnoreIndexForGroupByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.IgnoreIndexForGroupBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintIgnoreKeyTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.IgnoreKeyHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.IgnoreKey}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintIgnoreKeyForJoinTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.IgnoreKeyForJoinHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.IgnoreKeyForJoin}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintIgnoreKeyForOrderByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.IgnoreKeyForOrderByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.IgnoreKeyForOrderBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintIgnoreKeyForGroupByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.IgnoreKeyForGroupByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.IgnoreKeyForGroupBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintForceIndexTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.ForceIndexHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.ForceIndex}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintForceIndexForJoinTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.ForceIndexForJoinHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.ForceIndexForJoin}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintForceIndexForOrderByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.ForceIndexForOrderByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.ForceIndexForOrderBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintForceIndexForGroupByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.ForceIndexForGroupByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.ForceIndexForGroupBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintForceKeyTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.ForceKeyHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.ForceKey}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintForceKeyForJoinTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.ForceKeyForJoinHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.ForceKeyForJoin}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintForceKeyForOrderByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.ForceKeyForOrderByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.ForceKeyForOrderBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void IndexHintForceKeyForGroupByTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsMySql()
					.ForceKeyForGroupByHint("IX_ChildIndex", "IX_ChildIndex2")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {MySqlHints.Table.ForceKeyForGroupBy}(IX_ChildIndex, IX_ChildIndex2)"));
		}

		[Test]
		public void SubQueryHintForUpdateTest([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForUpdateHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForUpdate}"));
		}

		[Test]
		public void SubQueryHintForUpdateTest2([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForUpdateHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForUpdate} OF p, c_1"));
		}

		[Test]
		public void SubQueryHintForUpdateNoWaitTest([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForUpdateNoWaitHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForUpdate} {MySqlHints.SubQuery.NoWait}"));
		}

		[Test]
		public void SubQueryHintForUpdateNoWaitTest2([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForUpdateNoWaitHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForUpdate} OF p, c_1 {MySqlHints.SubQuery.NoWait}"));
		}

		[Test]
		public void SubQueryHintForUpdateSkipLockedTest([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForUpdateSkipLockedHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForUpdate} {MySqlHints.SubQuery.SkipLocked}"));
		}

		[Test]
		public void SubQueryHintForUpdateSkipLockedTest2([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForUpdateSkipLockedHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForUpdate} OF p, c_1 {MySqlHints.SubQuery.SkipLocked}"));
		}

		[Test]
		public void SubQueryHintForShareTest([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForShareHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForShare}"));
		}

		[Test]
		public void SubQueryHintForShareTest2([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForShareHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForShare} OF p, c_1"));
		}

		[Test]
		public void SubQueryHintForShareNoWaitTest([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForShareNoWaitHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForShare} {MySqlHints.SubQuery.NoWait}"));
		}

		[Test]
		public void SubQueryHintForShareNoWaitTest2([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForShareNoWaitHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForShare} OF p, c_1 {MySqlHints.SubQuery.NoWait}"));
		}

		[Test]
		public void SubQueryHintForShareSkipLockedTest([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForShareSkipLockedHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForShare} {MySqlHints.SubQuery.SkipLocked}"));
		}

		[Test]
		public void SubQueryHintForShareSkipLockedTest2([IncludeDataSources(true, TestProvName.AllMySql80)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsMySql()
			.ForShareSkipLockedHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{MySqlHints.SubQuery.ForShare} OF p, c_1 {MySqlHints.SubQuery.SkipLocked}"));
		}

	}
}
