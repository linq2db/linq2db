using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

namespace Tests.Extensions
{
	[TestFixture]
	public partial class SqlServerTests : TestBase
	{
		[Test]
		public void TableHintTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.With(SqlServerHints.Table.NoLock).IndexHint(SqlServerHints.Table.NoWait)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock, NoWait)"));
		}

		[Test]
		public void TableHint2005PlusTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(
				SqlServerHints.Table.HoldLock,
				SqlServerHints.Table.NoLock,
				SqlServerHints.Table.NoWait,
				SqlServerHints.Table.PagLock,
				SqlServerHints.Table.ReadCommitted,
				SqlServerHints.Table.ReadCommittedLock,
				SqlServerHints.Table.ReadPast,
				SqlServerHints.Table.ReadUncommitted,
				SqlServerHints.Table.RepeatableRead,
				SqlServerHints.Table.RowLock,
				SqlServerHints.Table.Serializable,
				SqlServerHints.Table.TabLock,
				SqlServerHints.Table.TabLockX,
				SqlServerHints.Table.UpdLock,
				SqlServerHints.Table.XLock
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
			[Values(SqlServerHints.Table.ForceScan)] string hint)
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
				from p in db.Parent.With(SqlServerHints.Table.SpatialWindowMaxCells(10))
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (SPATIAL_WINDOW_MAX_CELLS=10)"));
		}

		[Test]
		public void TableHintSpatialWindowMaxCellsTest2([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithSpatialWindowMaxCells(10)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (SPATIAL_WINDOW_MAX_CELLS=10)"));
		}

		[Test]
		public void TableHintIndexTest2([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child.IndexHint(SqlServerHints.Table.Index, "IX_ChildIndex")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (Index(IX_ChildIndex))"));
		}

		[Test]
		public void JoinHintTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(SqlServerHints.Join.Loop, SqlServerHints.Join.Hash, SqlServerHints.Join.Merge, SqlServerHints.Join.Remote)] string hint)
		{
			if (hint == SqlServerHints.Join.Remote && context.IsAnyOf(TestProvName.AllSqlAzure))
				Assert.Inconclusive("REMOTE hint not supported by Azure SQL");

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
			[Values(SqlServerHints.Join.Loop, SqlServerHints.Join.Hash, SqlServerHints.Join.Merge, SqlServerHints.Join.Remote)] string hint)
		{
			if (hint == SqlServerHints.Join.Remote && context.IsAnyOf(TestProvName.AllSqlAzure))
				Assert.Inconclusive("REMOTE hint not supported by Azure SQL");

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
			[Values(SqlServerHints.Join.Loop, SqlServerHints.Join.Hash, SqlServerHints.Join.Merge)] string hint,
			[Values(SqlJoinType.Left, SqlJoinType.Full)] SqlJoinType joinType)
		{
			using var db = GetDataContext(context);

			var q = db.Child.Join(db.Parent.JoinHint(hint), joinType, (c, p) => c.ParentID == p.ParentID, (c, p) => p);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{joinType.ToString().ToUpperInvariant()} {hint} JOIN"));
		}

		[Test]
		public void JoinHintInnerJoinMethodTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(SqlServerHints.Join.Loop, SqlServerHints.Join.Hash, SqlServerHints.Join.Merge, SqlServerHints.Join.Remote)] string hint)
		{
			if (hint == SqlServerHints.Join.Remote && context.IsAnyOf(TestProvName.AllSqlAzure))
				Assert.Inconclusive("REMOTE hint not supported by Azure SQL");

			using var db = GetDataContext(context);

			var q = db.Child.InnerJoin(db.Parent.JoinHint(hint), (c, p) => c.ParentID == p.ParentID, (c, p) => p);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"INNER {hint} JOIN"));
		}

		[Test]
		public void JoinHintRightJoinMethodTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(SqlServerHints.Join.Hash, SqlServerHints.Join.Merge)] string hint)
		{
			using var db = GetDataContext(context);

			var q = db.Child.RightJoin(db.Parent.JoinHint(hint), (c, p) => c.ParentID == p.ParentID, (c, p) => p);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"RIGHT {hint} JOIN"));
		}

		[Test]
		public void JoinLoopHintTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsSqlServer().JoinLoopHint() on c.ParentID equals p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("INNER LOOP JOIN"));
		}

		[Test]
		public void JoinHashHintTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
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
					.AsSqlServer()
					.JoinHashHint() on c.ParentID equals p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("INNER HASH JOIN"));
		}

		[Test]
		public void JoinMergeHintTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(SqlJoinType.Inner, SqlJoinType.Left, SqlJoinType.Full)] SqlJoinType joinType)
		{
			using var db = GetDataContext(context);

			var q = db.Child.Join(db.Parent.AsSqlServer().JoinMergeHint(), joinType, (c, p) => c.ParentID == p.ParentID, (c, p) => p);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{joinType.ToString().ToUpperInvariant()} MERGE JOIN"));
		}

		[Test]
		public void TableHintDeleteMethodTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child.With(SqlServerHints.Table.NoLock)
				where p.ParentID < -10000
				select p;

			q.Delete();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
		}

		[Test]
		public void QueryHintTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(
				SqlServerHints.Query.HashGroup,
				SqlServerHints.Query.OrderGroup,
				SqlServerHints.Query.ConcatUnion,
				SqlServerHints.Query.HashUnion,
				SqlServerHints.Query.MergeUnion,
				SqlServerHints.Query.LoopJoin,
				SqlServerHints.Query.HashJoin,
				SqlServerHints.Query.MergeJoin,
				SqlServerHints.Query.ExpandViews,
				SqlServerHints.Query.KeepPlan,
				SqlServerHints.Query.KeepFixedPlan,
				SqlServerHints.Query.Recompile,
				SqlServerHints.Query.RobustPlan
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
				SqlServerHints.Query.IgnoreNonClusteredColumnStoreIndex,
				SqlServerHints.Query.OptimizeForUnknown
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
			.QueryHint(SqlServerHints.Query.HashJoin)
			.QueryHint(SqlServerHints.Query.Fast(10));

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
			.QueryHint(SqlServerHints.Query.MaxGrantPercent(25));

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
			.QueryHint(SqlServerHints.Query.MinGrantPercent(25));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTION (MIN_GRANT_PERCENT=25)"));
		}

		[Test]
		public void QueryHintMaxDopTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.QueryHint(SqlServerHints.Query.MaxDop(25));

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
			.QueryHint(SqlServerHints.Query.MaxRecursion(25));

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
			.QueryHint(SqlServerHints.Query.OptimizeFor("@id=1"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTIMIZE FOR (@id=1)"));
		}

		[Test]
		public void QueryHintQueryTraceOnTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer2012PlusNoAzure)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				select p
			)
			.QueryHint(SqlServerHints.Query.QueryTraceOn(10));

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
				from c1 in db.Child.AsSqlServer().WithIndex("IX_ChildIndex")
				where c.ParentID == p.ParentID && c1.ParentID == p.ParentID
				select p
			)
			.TablesInScopeHint(SqlServerHints.Table.NoLock);

			q =
			(
				from p in q
				from c in db.Child
				from p1 in db.Parent.TablesInScopeHint(SqlServerHints.Table.HoldLock)
				where c.ParentID == p.ParentID && c.Parent!.ParentID > 0 && p1.ParentID == p.ParentID
				select p
			)
			.TablesInScopeHint(SqlServerHints.Table.NoWait);

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
				from c in db.Child.TableHint(SqlServerHints.Table.NoLock)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(SqlServerHints.Query.Recompile)
			.QueryHint(SqlServerHints.Query.Fast(10))
			.Delete();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("OPTION (RECOMPILE, FAST 10)"));
		}

		[Test]
		public void InsertTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			(
				from c in db.Child.TableHint(SqlServerHints.Table.NoLock)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(SqlServerHints.Query.Recompile)
			.QueryHint(SqlServerHints.Query.Fast(10))
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
				from c in db.Child.TableHint(SqlServerHints.Table.NoLock)
				where c.ParentID < -1111
				select c
			)
			.QueryHint(SqlServerHints.Query.Recompile)
			.QueryHint(SqlServerHints.Query.Fast(10))
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
						from c in db.Parent1.TableHint(SqlServerHints.Table.NoLock)
						where c.ParentID < -1111
						select c
					)
					.QueryHint(SqlServerHints.Query.Recompile)
					.QueryHint(SqlServerHints.Query.Fast(10))
				)
				.OnTargetKey()
				.UpdateWhenMatched()
				.Merge();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("OPTION (RECOMPILE, FAST 10)"));
		}

		[Test]
		public void CteTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var cte =
				(
					from c in db.Child.TableHint(SqlServerHints.Table.NoLock)
					where c.ParentID < -1111
					select c
				)
				.QueryHint(SqlServerHints.Query.Fast(10))
				.TablesInScopeHint(SqlServerHints.Table.NoWait)
				.AsCte();

			var q =
				(
					from p in cte
					from c in db.Child
					select p
				)
				.QueryHint(SqlServerHints.Query.Recompile)
				.TablesInScopeHint(SqlServerHints.Table.HoldLock);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock, NoWait)"));
			Assert.That(LastQuery, Contains.Substring("WITH (HoldLock)"));
			Assert.That(LastQuery, Contains.Substring("OPTION (RECOMPILE, FAST 10)"));
		}

		[Test]
		public void UnionTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q1 =
				from c in db.Child
				join p in db.Parent.TableHint(SqlServerHints.Table.NoLock) on c.ParentID equals p.ParentID
				select p;

			var q =
				q1.QueryName("qb_1").Union(q1.QueryName("qb_2"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("[p_1] WITH (NoLock)"));
		}

		[Test]
		public void WithIndexTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsSqlServer()
					.WithIndex("IX_ChildIndex")
					.With(SqlServerHints.Table.NoLock)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (Index(IX_ChildIndex), NoLock)"));
		}

		[Test]
		public void WithIndexTest2([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsSqlServer()
					.WithIndex("IX_ChildIndex", "IX_ChildIndex")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (Index(IX_ChildIndex, IX_ChildIndex))"));
		}

		[Test]
		public void WithForceSeekTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsSqlServer()
					.WithForceSeek()
				select p;

			try
			{
				_ = q.ToList();
			}
#pragma warning disable CS0618 // Type or member is obsolete
			catch (System.Data.SqlClient.SqlException    ex) when (ex.Number == 8622) {}
#pragma warning restore CS0618 // Type or member is obsolete
			catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 8622) {}
#if NETFRAMEWORK
			catch (System.ServiceModel.FaultException    ex) when (ex.Message.Contains("8622")) {}
#else
			catch (Grpc.Core.RpcException                ex) when (ex.Message.Contains("8622")) {}
#endif

			Assert.That(LastQuery, Contains.Substring("WITH (ForceSeek)"));
		}

		[Test]
		public void WithForceSeekTest2([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsSqlServer()
					.WithForceSeek()
					.WithIndex("IX_ChildIndex")
				select p;

			try
			{
				_ = q.ToList();
			}
#pragma warning disable CS0618 // Type or member is obsolete
			catch (System.Data.SqlClient.SqlException    ex) when (ex.Number == 8622) {}
#pragma warning restore CS0618 // Type or member is obsolete
			catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 8622) {}
#if NETFRAMEWORK
			catch (System.ServiceModel.FaultException    ex) when (ex.Message.Contains("8622")) { }
#else
			catch (Grpc.Core.RpcException                ex) when (ex.Message.Contains("8622")) {}
#endif

			Assert.That(LastQuery, Contains.Substring("WITH (ForceSeek, Index(IX_ChildIndex))"));
		}

		[Test]
		public void WithForceSeekTest3([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsSqlServer()
					.WithForceSeek("IX_ChildIndex")
				select p;

			try
			{
				_ = q.ToList();
			}
#pragma warning disable CS0618 // Type or member is obsolete
			catch (System.Data.SqlClient.SqlException    ex) when (ex.Number == 8622) {}
#pragma warning restore CS0618 // Type or member is obsolete
			catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 8622) {}
#if NETFRAMEWORK
			catch (System.ServiceModel.FaultException    ex) when (ex.Message.Contains("8622")) { }
#else
			catch (Grpc.Core.RpcException                ex) when (ex.Message.Contains("8622")) {}
#endif

			Assert.That(LastQuery, Contains.Substring("WITH (ForceSeek, Index(IX_ChildIndex))"));
		}

		[Test]
		public void WithForceSeekTest4([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child
					.AsSqlServer()
					.WithForceSeek("IX_ChildIndex", c => c.ParentID)
				select p;

#pragma warning disable CS0618 // Type or member is obsolete
			try
			{
				_ = q.ToList();
			}
			catch (System.Data.SqlClient.SqlException    ex) when (ex.Number == 8622) {}
#pragma warning restore CS0618 // Type or member is obsolete
			catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 8622) {}
#if NETFRAMEWORK
			catch (System.ServiceModel.FaultException    ex) when (ex.Message.Contains("8622")) { }
#else
			catch (Grpc.Core.RpcException                ex) when (ex.Message.Contains("8622")) {}
#endif

			Assert.That(LastQuery, Contains.Substring("WITH (ForceSeek(IX_ChildIndex([ParentID])))"));
		}

		[Test]
		public void OptionOptimizeForTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var n  = 10;
			var id = 10;

			var q =
				(
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					where p.Value1 == n && p.ParentID == id
					select p
				)
				.AsSqlServer()
				.OptionOptimizeFor("@n=10", "@id=10");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTION (OPTIMIZE FOR(@n=10, @id=10))"));
		}

		[Test]
		public void OptionUseHintTest([IncludeDataSources(true, TestProvName.AllSqlServer2017Plus)] string context)
		{
			using var db = GetDataContext(context);

			var n  = 10;
			var id = 10;

			var q =
				(
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					where p.Value1 == n && p.ParentID == id
					select p
				)
				.AsSqlServer()
				.OptionUseHint("'ASSUME_JOIN_PREDICATE_DEPENDS_ON_FILTERS'");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTION (USE HINT('ASSUME_JOIN_PREDICATE_DEPENDS_ON_FILTERS'))"));
		}

		[Test]
		public void OptionUseTableTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			var n  = 10;
			var id = 10;

			var q =
				(
					from c in db.Child.TableID("pp")
						.AsSqlServer()
						.WithNoLock()
						.WithForceSeek()
					join p in db.Parent on c.ParentID equals p.ParentID
					where p.Value1 == n && p.ParentID == id
					select p
				)
				.AsSqlServer()
				.OptionTableHint(Sql.TableAlias("pp"), SqlServerHints.Table.NoLock);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("OPTION (TABLE HINT(c_1, NoLock))"));
		}

		[Test]
		public void SqlServerUnionAllTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from p in db.Parent
					select p
				)
				.Concat
				(
					from p in db.Child
					select p.Parent
				)
				.AsSqlServer()
				.OptionRecompile()
			;

			_ = q.ToList();

			Assert.That(LastQuery, Should.Contain(
				"UNION ALL",
				"OPTION (RECOMPILE)"));
		}

		private void SubQueryTest1([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				select new
				{
					cc =
					(
						from c1 in db.Child
						.AsSqlServer().WithNoLock()
						where c1.ParentID == c.ParentID select c1
					).Count()
				};

			_ = q.ToList();
		}

		[Test]
		public void SubQueryTest2([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				select new
				{
					cc =
					(
						from c1 in db.Child
						.AsSqlServer().WithNoLock()
						where c1.ParentID == c.ParentID select c1.ChildID
					).FirstOrDefault()
				};

			_ = q.ToList();
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4321")]
		public void TablesInScopeHintWithTReferenceTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				select new
				{
					c.ChildID,
					c.Parent!.ParentID
				}
			)
			.TablesInScopeHint(SqlServerHints.Table.NoLock);

			_ = q.ToList();

			var test = LastQuery?.Replace("\r", "");

			Assert.That(test, Contains.Substring("[Child] [c_1] WITH (NoLock)"));
			Assert.That(test, Contains.Substring("[Parent] [a_Parent] WITH (NoLock)"));
		}
	}
}
