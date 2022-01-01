using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.MySql;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryExtensionMySqlTests : TestBase
	{
		[Test]
		public void QueryHintTest([IncludeDataSources(true, TestProvName.AllMySql)] string context,
			[Values(
				MySqlHints.QueryHint.Bka,                      MySqlHints.QueryHint.NoBka,
				MySqlHints.QueryHint.Bnl,                      MySqlHints.QueryHint.NoBnl,
				MySqlHints.QueryHint.DerivedConditionPushDown, MySqlHints.QueryHint.NoDerivedConditionPushDown,
				MySqlHints.QueryHint.HashJoin,                 MySqlHints.QueryHint.NoHashJoin,
				MySqlHints.QueryHint.Merge,                    MySqlHints.QueryHint.NoMerge
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from p in db.Parent.TableHint(MySqlHints.TableHint.NoBka).TableID("Pr")
					from c in db.Child.TableID("Ch")
					select p
				)
				.QueryHint(hint, Sql.TableID("Pr"), Sql.TableID("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ NO_BKA(p) {hint}(p, c_1) */"));
		}

		[Test]
		public void IndexHintTest([IncludeDataSources(true, TestProvName.AllMySql)] string context,
			[Values(
				MySqlHints.IndexHint.GroupIndex, MySqlHints.IndexHint.NoGroupIndex,
				MySqlHints.IndexHint.Index,      MySqlHints.IndexHint.NoIndex,
				MySqlHints.IndexHint.IndexMerge, MySqlHints.IndexHint.NoIndexMerge,
				MySqlHints.IndexHint.JoinIndex,  MySqlHints.IndexHint.NoJoinIndex,
				MySqlHints.IndexHint.Mrr,        MySqlHints.IndexHint.NoMrr,
				MySqlHints.IndexHint.NoIcp,
				MySqlHints.IndexHint.NoRangeOptimization,
				MySqlHints.IndexHint.OrderIndex, MySqlHints.IndexHint.NoOrderIndex,
				MySqlHints.IndexHint.SkipScan,   MySqlHints.IndexHint.NoSkipScan
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(hint, "parent_ix", "parent2_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {hint}(p parent_ix, parent2_ix)"));
		}

		[Test]
		public void TableSubQueryHintTest([IncludeDataSources(true, TestProvName.AllMySql)] string context,
			[Values(
				MySqlHints.QueryHint.SemiJoin, MySqlHints.QueryHint.NoSemiJoin
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in
					(
						from p in db.Parent
						from c in db.Child
						select p
					)
					.AsSubQuery("qq")
				select p
			)
			.SubQueryHint(hint, "@qq", "FIRSTMATCH", "LOOSESCAN");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {hint}(@qq FIRSTMATCH, LOOSESCAN)"));
		}

		[Test]
		public void TableHintTest([IncludeDataSources(true, TestProvName.AllMySql)] string context,
			[Values(
				MySqlHints.TableHint.Bka,                      MySqlHints.TableHint.NoBka,
				MySqlHints.TableHint.Bnl,                      MySqlHints.TableHint.NoBnl,
				MySqlHints.TableHint.DerivedConditionPushDown, MySqlHints.TableHint.NoDerivedConditionPushDown,
				MySqlHints.TableHint.HashJoin,                 MySqlHints.TableHint.NoHashJoin,
				MySqlHints.TableHint.Merge,                    MySqlHints.TableHint.NoMerge
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
		public void SetVarHintTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				select p
			).QueryHint(MySqlHints.QueryHint.SetVar, "sort_buffer_size=16M");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ SET_VAR(sort_buffer_size=16M) */"));
		}

		[Test]
		public void  ResourceGroupHintTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				select p
			).QueryHint(MySqlHints.QueryHint.ResourceGroup, "USR_default");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ RESOURCE_GROUP(USR_default) */"));
		}

		[Test]
		public void IndexHintSingleTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(MySqlHints.IndexHint.Index, "parent_ix")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ INDEX(p parent_ix)"));
		}

		[Test]
		public void QueryHintFirstRowsTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(MySqlHints.QueryHint.MaxExecutionTime(1000));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ MAX_EXECUTION_TIME(1000) */"));
		}

		[Test]
		public void TableIndexHintTest([IncludeDataSources(true, TestProvName.AllMySql)] string context,
			[Values(
				"USE INDEX",
				"USE KEY FOR ORDER BY"
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Child.IndexHint(hint, "IX_ChildIndex", "IX_ChildIndex2").With(MySqlHints.TableHint.Bka)
				select p
			)
			.QueryHint(MySqlHints.QueryHint.MaxExecutionTime(1000));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ BKA(p) MAX_EXECUTION_TIME(1000) */"));
			Assert.That(LastQuery, Contains.Substring($"`Child` `p` {hint}(IX_ChildIndex, IX_ChildIndex2)"));
		}
	}
}
