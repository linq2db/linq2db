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
				from p in db.Parent.With(Hints.TableHint.NoLock).With(Hints.TableHint.NoWait)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock, NoWait)"));
		}

		[Test]
		public void TableHint2005PlusTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2005Plus)] string context,
			[Values(
				Hints.TableHint.HoldLock,
				Hints.TableHint.NoLock,
				Hints.TableHint.NoWait,
				Hints.TableHint.PagLock,
				Hints.TableHint.ReadCommitted,
				Hints.TableHint.ReadCommittedLock,
				Hints.TableHint.ReadPast,
				Hints.TableHint.ReadUncommitted,
				Hints.TableHint.RepeatableRead,
				Hints.TableHint.RowLock,
				Hints.TableHint.Serializable,
				Hints.TableHint.TabLock,
				Hints.TableHint.TabLockX,
				Hints.TableHint.UpdLock,
				Hints.TableHint.XLock
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
				Hints.TableHint.ForceScan
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
				from p in db.Parent.With(Hints.TableHint.SpatialWindowMaxCells(10))
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
					.With(Hints.TableHint.Index("IX_ChildIndex"))
					.With(Hints.TableHint.NoLock)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (Index(IX_ChildIndex), NoLock)"));
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
			[Values(Hints.JoinHint.Loop, Hints.JoinHint.Hash, Hints.JoinHint.Merge, Hints.JoinHint.Remote)] string hint)
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
			[Values(Hints.JoinHint.Loop, Hints.JoinHint.Hash, Hints.JoinHint.Merge, Hints.JoinHint.Remote)] string hint)
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
			[Values(Hints.JoinHint.Loop, Hints.JoinHint.Hash, Hints.JoinHint.Merge)] string hint,
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
			[Values(Hints.JoinHint.Loop, Hints.JoinHint.Hash, Hints.JoinHint.Merge, Hints.JoinHint.Remote)] string hint)
		{
			using var db = GetDataContext(context);

			var q = db.Child.InnerJoin(db.Parent.JoinHint(hint), (c, p) => c.ParentID == p.ParentID, (c, p) => p);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"INNER {hint} JOIN"));
		}

		[Test]
		public void JoinHintRightJoinMethodTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(Hints.JoinHint.Hash, Hints.JoinHint.Merge)] string hint)
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
				from p in db.Child.With(Hints.TableHint.NoLock)
				where p.ParentID < -10000
				select p;

			q.Delete();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
		}

		[Test]
		public void QueryHintTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(
				Hints.Option.HashGroup,
				Hints.Option.OrderGroup,
				Hints.Option.ConcatUnion,
				Hints.Option.HashUnion,
				Hints.Option.MergeUnion,
				Hints.Option.LoopJoin,
				Hints.Option.HashJoin,
				Hints.Option.MergeJoin,
				Hints.Option.ExpandViews,
				Hints.Option.KeepPlan,
				Hints.Option.KeepFixedPlan,
				Hints.Option.Recompile,
				Hints.Option.RobustPlan
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
				Hints.Option.IgnoreNonClusteredColumnStoreIndex,
				Hints.Option.OptimizeForUnknown
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
			.QueryHint(Hints.Option.HashJoin)
			.QueryHint(Hints.Option.Fast(10));

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
			.QueryHint(Hints.Option.MaxGrantPercent(25));

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
			.QueryHint(Hints.Option.MinGrantPercent(25));

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
			.QueryHint(Hints.Option.MaxDop(25));

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
			.QueryHint(Hints.Option.MaxRecursion(25));

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
			.QueryHint(Hints.Option.OptimizeFor("@id=1"));

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
			.QueryHint(Hints.Option.QueryTraceOn(10));

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
				from c1 in db.Child.With(Hints.TableHint.Index("IX_ChildIndex"))
				where c.ParentID == p.ParentID && c1.ParentID == p.ParentID
				select p
			)
			.TablesInScopeHint(Hints.TableHint.NoLock);

			q =
			(
				from p in q
				from c in db.Child
				from p1 in db.Parent.TablesInScopeHint(Hints.TableHint.HoldLock)
				where c.ParentID == p.ParentID && c.Parent!.ParentID > 0 && p1.ParentID == p.ParentID
				select p
			)
			.TablesInScopeHint(Hints.TableHint.NoWait);

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
				from c in db.Child.TableHint(Hints.TableHint.NoLock)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(Hints.Option.Recompile)
			.QueryHint(Hints.Option.Fast(10))
			.Delete();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("OPTION (RECOMPILE, FAST 10)"));
		}

		[Test]
		public void InsertTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child.TableHint(Hints.TableHint.NoLock)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(Hints.Option.Recompile)
			.QueryHint(Hints.Option.Fast(10))
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
				from c in db.Child.TableHint(Hints.TableHint.NoLock)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(Hints.Option.Recompile)
			.QueryHint(Hints.Option.Fast(10))
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
						from c in db.Parent1.TableHint(Hints.TableHint.NoLock)
						where c.ParentID < -1111
						select c
					)
					.QueryHint(Hints.Option.Recompile)
					.QueryHint(Hints.Option.Fast(10))
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
					from c in db.Child.TableHint(Hints.TableHint.NoLock)
					where c.ParentID < -1111
					select c
				)
				.QueryHint(Hints.Option.Fast(10))
				.TablesInScopeHint(Hints.TableHint.NoWait)
				.AsCte();

			var q =
				(
					from p in cte
					from c in db.Child
					select p
				)
				.QueryHint(Hints.Option.Recompile)
				.TablesInScopeHint(Hints.TableHint.HoldLock);

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
				join p in db.Parent.TableHint(Hints.TableHint.NoLock) on c.ParentID equals p.ParentID
				select p;

			var q =
				q1.QueryName("qb_1").Union(q1.QueryName("qb_2"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("[p_1] WITH (NoLock)"));
		}
	}
}
