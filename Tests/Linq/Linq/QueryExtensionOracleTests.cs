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
						from p in db.Parent.TableHint(Hints.TableHint.Full).With(Hints.TableHint.Cache)
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
								from p in db.Parent.TableHint(Hints.TableHint.Full)
								from c in db.Child.TableHint(Hints.TableHint.Full)
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
				Hints.TableHint.Cache,
				Hints.TableHint.Cluster,
				Hints.TableHint.DrivingSite,
				Hints.TableHint.Full
				)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.With(hint)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {hint}(p) */"));
		}

		/*
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
		*/

		[Test]
		public void QueryHintTest([IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values(
				Hints.QueryHint.AllRows,
				Hints.QueryHint.Append,
				Hints.QueryHint.CursorSharingExact
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
		public void QueryHintFirstRowsTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					select p
				)
				.QueryHint(Hints.QueryHint.FirstRows(25));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /*+ FIRST_ROWS(25) */"));
		}

		/*
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

			Assert.That(LastQuery, Contains.Substring("[Parent] [p] WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("[Child] [c_1] WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("[Child] [c_2] WITH (NoWait)"));
			Assert.That(LastQuery, Contains.Substring("[Parent] [a_Parent] WITH (NoWait)"));
			Assert.That(LastQuery, Contains.Substring("[Child] [c_3]\r\n"));
			Assert.That(LastQuery, Contains.Substring("[Child] [c1] WITH (Index(IX_ChildIndex), NoLock)"));
			Assert.That(LastQuery, Contains.Substring("[Parent] [p1] WITH (HoldLock)"));
		}
		*/
	}
}
