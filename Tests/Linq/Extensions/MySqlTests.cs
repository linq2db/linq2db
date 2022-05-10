using LinqToDB;
using LinqToDB.DataProvider.MySql;

using NUnit.Framework;

namespace Tests.Extensions;

[TestFixture]
public partial class MySqlTests : TestBase
{
	[Test]
	public void QueryHintTest([IncludeDataSources(true, TestProvName.AllMySql)] string context,
		[Values(
			MySqlHints.Query.Bka,                      MySqlHints.Query.NoBka,
			MySqlHints.Query.Bnl,                      MySqlHints.Query.NoBnl,
			MySqlHints.Query.DerivedConditionPushDown, MySqlHints.Query.NoDerivedConditionPushDown,
			MySqlHints.Query.HashJoin,                 MySqlHints.Query.NoHashJoin,
			MySqlHints.Query.Merge,                    MySqlHints.Query.NoMerge
		)] string hint)
	{
		using var db = GetDataContext(context);

		var q =
			(
				from p in db.Parent.TableHint(MySqlHints.Table.NoBka).TableID("Pr")
				from c in db.Child.TableID("Ch")
				select p
			)
			.QueryHint(hint, Sql.TableSpec("Pr"), Sql.TableSpec("Ch"));

		_ = q.ToList();

		Assert.That(LastQuery, Contains.Substring($"SELECT /*+ NO_BKA(p) {hint}(p, c_1) */"));
	}

	[Test]
	public void IndexHintTest([IncludeDataSources(true, TestProvName.AllMySql)] string context,
		[Values(
			MySqlHints.Table.GroupIndex, MySqlHints.Table.NoGroupIndex,
			MySqlHints.Table.Index,      MySqlHints.Table.NoIndex,
			MySqlHints.Table.IndexMerge, MySqlHints.Table.NoIndexMerge,
			MySqlHints.Table.JoinIndex,  MySqlHints.Table.NoJoinIndex,
			MySqlHints.Table.Mrr,        MySqlHints.Table.NoMrr,
			MySqlHints.Table.NoIcp,
			MySqlHints.Table.NoRangeOptimization,
			MySqlHints.Table.OrderIndex, MySqlHints.Table.NoOrderIndex,
			MySqlHints.Table.SkipScan,   MySqlHints.Table.NoSkipScan
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
			MySqlHints.Query.SemiJoin, MySqlHints.Query.NoSemiJoin
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
		.AsMySql()
		.QueryBlockHint(hint, "@qq", "FIRSTMATCH", "LOOSESCAN");

		_ = q.ToList();

		Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {hint}(@qq FIRSTMATCH, LOOSESCAN)"));
	}

	[Test]
	public void TableHintTest([IncludeDataSources(true, TestProvName.AllMySql)] string context,
		[Values(
			MySqlHints.Table.Bka,                      MySqlHints.Table.NoBka,
			MySqlHints.Table.Bnl,                      MySqlHints.Table.NoBnl,
			MySqlHints.Table.DerivedConditionPushDown, MySqlHints.Table.NoDerivedConditionPushDown,
			MySqlHints.Table.HashJoin,                 MySqlHints.Table.NoHashJoin,
			MySqlHints.Table.Merge,                    MySqlHints.Table.NoMerge
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
		)
		.QueryHint(MySqlHints.Query.SetVar, "sort_buffer_size=16M");

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
		).QueryHint(MySqlHints.Query.ResourceGroup, "USR_default");

		_ = q.ToList();

		Assert.That(LastQuery, Contains.Substring("SELECT /*+ RESOURCE_GROUP(USR_default) */"));
	}

	[Test]
	public void IndexHintSingleTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
	{
		using var db = GetDataContext(context);

		var q =
			from p in db.Parent.TableHint(MySqlHints.Table.Index, "parent_ix")
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
		.QueryHint(MySqlHints.Query.MaxExecutionTime(1000));

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
			from p in db.Child.IndexHint(hint, "IX_ChildIndex", "IX_ChildIndex2").With(MySqlHints.Table.Bka)
			select p
		)
		.QueryHint(MySqlHints.Query.MaxExecutionTime(1000));

		_ = q.ToList();

		Assert.That(LastQuery, Contains.Substring("SELECT /*+ BKA(p) MAX_EXECUTION_TIME(1000) */"));
		Assert.That(LastQuery, Contains.Substring($"`Child` `p` {hint}(IX_ChildIndex, IX_ChildIndex2)"));
	}

	[Test]
	public void QueryHintSemiJoinWithQueryBlockTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
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
			.AsMySql()
			.SemiJoinHintWithQueryBlock("@qq", "FIRSTMATCH", "LOOSESCAN");

		_ = q.ToList();

		Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.SemiJoin}(@qq FIRSTMATCH, LOOSESCAN)"));
	}

	[Test]
	public void QueryHintNoSemiJoinWithQueryBlockTest([IncludeDataSources(true, TestProvName.AllMySql)] string context)
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
			.AsMySql()
			.NoSemiJoinHintWithQueryBlock("@qq", "FIRSTMATCH", "LOOSESCAN");

		_ = q.ToList();

		Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {MySqlHints.Query.NoSemiJoin}(@qq FIRSTMATCH, LOOSESCAN)"));
		Assert.That(LastQuery, Contains.Substring("\tSELECT /*+ QB_NAME(qq) */").Using(StringComparison.Ordinal));
	}
}
