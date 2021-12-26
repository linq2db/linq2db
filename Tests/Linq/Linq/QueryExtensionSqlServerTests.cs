using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryExtensionSqlServerTests : TestBase
	{
		[Test]
		public void TableHintTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.With(Hints.Table.NoLock).With(Hints.Table.NoWait)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock, NoWait)"));
		}

		[Test]
		public void TableHint2005PlusTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2005Plus)] string context,
			[Values(
				Hints.Table.HoldLock,
				Hints.Table.NoLock,
				Hints.Table.NoWait,
				Hints.Table.PagLock,
				Hints.Table.ReadCommitted,
				Hints.Table.ReadCommittedLock,
				Hints.Table.ReadPast,
				Hints.Table.ReadUncommitted,
				Hints.Table.RepeatableRead,
				Hints.Table.RowLock,
				Hints.Table.Serializable,
				Hints.Table.TabLock,
				Hints.Table.TabLockX,
				Hints.Table.UpdLock,
				Hints.Table.XLock
				)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.With(hint)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"WITH ({hint})"));
		}

		[Test]
		public void TableHint2012PlusTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context,
			[Values(
				Hints.Table.ForceScan
//				TableHint.ForceSeek,
//				TableHint.Snapshot
				)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.TableHint(hint)
				select p;

			_ = q.ToList();

			Assert.That(q.ToString(), Contains.Substring($"WITH ({hint})"));
		}

		[Test]
		public void TableHintSpatialWindowMaxCellsTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.With(Hints.Table.SpatialWindowMaxCells(10))
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (SPATIAL_WINDOW_MAX_CELLS=10)"));
		}

		[Test]
		public void TableHintIndexTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.WithIndex("IX_ChildIndex")
					.With(Hints.Table.NoLock)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (Index(IX_ChildIndex), NoLock)"));
		}

		[Test]
		public void TableHintIndexTest2([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child.TableHint(Hints.Table.Index, "IX_ChildIndex")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (Index(IX_ChildIndex))"));
		}

		[Test]
		public void TableHintIndexTest3([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child.WithIndex("IX_ChildIndex", "IX_ChildIndex")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (Index(IX_ChildIndex, IX_ChildIndex))"));
		}

		[Test, Explicit]
		public void TableHintForceSeekTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child.WithForceSeek("IX_ChildIndex", c => c.ParentID)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (ForceSeek (IX_ChildIndex (ParentID)))"));
		}

		[Test, Explicit]
		public void TableHintForceSeekTest2([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child.WithForceSeek("IX_ChildIndex")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (ForceSeek (IX_ChildIndex))"));
		}

		[Test]
		public void JoinHintTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(Hints.Join.Loop, Hints.Join.Hash, Hints.Join.Merge, Hints.Join.Remote)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.JoinHint(hint) on c.ParentID equals p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"INNER {hint} JOIN"));
		}

		[Test]
		public void JoinHintSubQueryTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(Hints.Join.Loop, Hints.Join.Hash, Hints.Join.Merge, Hints.Join.Remote)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in
				(
					from t in db.Parent
					where t.Children.Any()
					select new { t.ParentID, t.Children.Count }
				)
				.JoinHint(hint) on c.ParentID equals p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"INNER {hint} JOIN"));
		}

		[Test]
		public void JoinHintMethodTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(Hints.Join.Loop, Hints.Join.Hash, Hints.Join.Merge)] string hint,
			[Values(SqlJoinType.Left, SqlJoinType.Full)] SqlJoinType joinType)
		{
			using var db = GetDataContext(context);

			var q = db.Child.Join(db.Parent.JoinHint(hint), joinType, (c, p) => c.ParentID == p.ParentID, (c, p) => p);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{joinType.ToString().ToUpper()} {hint} JOIN"));
		}

		[Test]
		public void JoinHintInnerJoinMethodTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(Hints.Join.Loop, Hints.Join.Hash, Hints.Join.Merge, Hints.Join.Remote)] string hint)
		{
			using var db = GetDataContext(context);

			var q = db.Child.InnerJoin(db.Parent.JoinHint(hint), (c, p) => c.ParentID == p.ParentID, (c, p) => p);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"INNER {hint} JOIN"));
		}

		[Test]
		public void JoinHintRightJoinMethodTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(Hints.Join.Hash, Hints.Join.Merge)] string hint)
		{
			using var db = GetDataContext(context);

			var q = db.Child.RightJoin(db.Parent.JoinHint(hint), (c, p) => c.ParentID == p.ParentID, (c, p) => p);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"RIGHT {hint} JOIN"));
		}

		[Test]
		public void TableHintDeleteMethodTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child.With(Hints.Table.NoLock)
				where p.ParentID < -10000
				select p;

			q.Delete();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
		}

		[Test]
		public void QueryHintTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(
				Hints.Query.HashGroup,
				Hints.Query.OrderGroup,
				Hints.Query.ConcatUnion,
				Hints.Query.HashUnion,
				Hints.Query.MergeUnion,
				Hints.Query.LoopJoin,
				Hints.Query.HashJoin,
				Hints.Query.MergeJoin,
				Hints.Query.ExpandViews,
				Hints.Query.KeepPlan,
				Hints.Query.KeepFixedPlan,
				Hints.Query.Recompile,
				Hints.Query.RobustPlan
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

			Assert.That(LastQuery, Contains.Substring($"OPTION ({hint})"));
		}

		[Test]
		public void QueryHint2008PlusTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context,
			[Values(
				Hints.Query.IgnoreNonClusteredColumnStoreIndex,
				Hints.Query.OptimizeForUnknown
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

			Assert.That(LastQuery, Contains.Substring($"OPTION ({hint})"));
		}

		[Test]
		public void QueryHintFastTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(Hints.Query.HashJoin)
			.QueryHint(Hints.Query.Fast(10));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTION (HASH JOIN, FAST 10)"));
		}

		[Test]
		public void QueryHintMaxGrantPercentTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(Hints.Query.MaxGrantPercent(25));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTION (MAX_GRANT_PERCENT=25)"));
		}

		[Test]
		public void QueryHintMinGrantPercentTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(Hints.Query.MinGrantPercent(25));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTION (MIN_GRANT_PERCENT=25)"));
		}

		[Test]
		public void QueryHintMaxDopTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(Hints.Query.MaxDop(25));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTION (MAXDOP 25)"));
		}

		[Test]
		public void QueryHintMaxRecursionTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(Hints.Query.MaxRecursion(25));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTION (MAXRECURSION 25)"));
		}

		[Test]
		public void QueryHintOptimizeForTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var id = 1;

			var q =
			(
				from p in db.Parent
				where p.ParentID == id
				select p
			)
			.QueryHint(Hints.Query.OptimizeFor("@id=1"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTIMIZE FOR (@id=1)"));
		}

		[Test]
		public void QueryHintQueryTraceOnTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				select p
			)
			.QueryHint(Hints.Query.QueryTraceOn(10));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("QUERYTRACEON 10"));
		}

		[Test]
		public void TablesInScopeHintTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				from c in db.Child
				from c1 in db.Child.WithIndex("IX_ChildIndex")
				where c.ParentID == p.ParentID && c1.ParentID == p.ParentID
				select p
			)
			.TablesInScopeHint(Hints.Table.NoLock);

			q =
			(
				from p in q
				from c in db.Child
				from p1 in db.Parent.TablesInScopeHint(Hints.Table.HoldLock)
				where c.ParentID == p.ParentID && c.Parent!.ParentID > 0 && p1.ParentID == p.ParentID
				select p
			)
			.TablesInScopeHint(Hints.Table.NoWait);

			q =
				from p in q
				from c in db.Child
				where c.ParentID == p.ParentID
				select p;

			_ = q.ToList();

			var test = LastQuery?.Replace("\r", "");

			Assert.That(test, Contains.Substring("[Parent] [p] WITH (NoLock)"));
			Assert.That(test, Contains.Substring("[Child] [c_1] WITH (NoLock)"));
			Assert.That(test, Contains.Substring("[Child] [c_2] WITH (NoWait)"));
			Assert.That(test, Contains.Substring("[Parent] [a_Parent] WITH (NoWait)"));
			Assert.That(test, Contains.Substring("[Child] [c1] WITH (Index(IX_ChildIndex), NoLock)"));
			Assert.That(test, Contains.Substring("[Parent] [p1] WITH (HoldLock)"));
			Assert.That(test, Contains.Substring("[Child] [c_3]\n"));
		}

		[Test]
		public void DeleteTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child.TableHint(Hints.Table.NoLock)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(Hints.Query.Recompile)
			.QueryHint(Hints.Query.Fast(10))
			.Delete();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("OPTION (RECOMPILE, FAST 10)"));
		}

		[Test]
		public void InsertTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child.TableHint(Hints.Table.NoLock)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(Hints.Query.Recompile)
			.QueryHint(Hints.Query.Fast(10))
			.Insert(db.Child, c => new()
			{
				ChildID = c.ChildID * 2
			});

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("OPTION (RECOMPILE, FAST 10)"));
		}

		[Test]
		public void UpdateTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child.TableHint(Hints.Table.NoLock)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(Hints.Query.Recompile)
			.QueryHint(Hints.Query.Fast(10))
			.Update(db.Child, c => new()
			{
				ChildID = c.ChildID * 2
			});

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("OPTION (RECOMPILE, FAST 10)"));
		}

		[Test]
		public void MergeTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			db.Parent1
				.Merge()
				.Using
				(
					(
						from c in db.Parent1.TableHint(Hints.Table.NoLock)
						where c.ParentID < -1111
						select c
					)
					.QueryHint(Hints.Query.Recompile)
					.QueryHint(Hints.Query.Fast(10))
				)
				.OnTargetKey()
				.UpdateWhenMatched()
				.Merge();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("OPTION (RECOMPILE, FAST 10)"));
		}

		[Test]
		public void CteTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			var cte =
				(
					from c in db.Child.TableHint(Hints.Table.NoLock)
					where c.ParentID < -1111
					select c
				)
				.QueryHint(Hints.Query.Fast(10))
				.TablesInScopeHint(Hints.Table.NoWait)
				.AsCte();

			var q =
				(
					from p in cte
					from c in db.Child
					select p
				)
				.QueryHint(Hints.Query.Recompile)
				.TablesInScopeHint(Hints.Table.HoldLock);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock, NoWait)"));
			Assert.That(LastQuery, Contains.Substring("WITH (HoldLock)"));
			Assert.That(LastQuery, Contains.Substring("OPTION (FAST 10, RECOMPILE)"));
		}

		[Test]
		public void UnionTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q1 =
				from c in db.Child
				join p in db.Parent.TableHint(Hints.Table.NoLock) on c.ParentID equals p.ParentID
				select p;

			var q =
				q1.QueryName("qb_1").Union(q1.QueryName("qb_2"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("[p_1] WITH (NoLock)"));
		}
	}
}
