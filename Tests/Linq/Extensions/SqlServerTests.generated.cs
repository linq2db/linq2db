﻿// Generated.
//
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

namespace Tests.Extensions
{
	partial class SqlServerTests
	{
		[Test]
		public void WithForceScanTableTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithForceScan()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (ForceScan)"));
		}

		[Test]
		public void WithForceScanInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithForceScanInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (ForceScan)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (ForceScan)"));
		}

		[Test]
		public void WithHoldLockTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithHoldLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (HoldLock)"));
		}

		[Test]
		public void WithHoldLockInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithHoldLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (HoldLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (HoldLock)"));
		}

		[Test]
		public void WithNoLockTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithNoLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
		}

		[Test]
		public void WithNoLockInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithNoLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (NoLock)"));
		}

		[Test]
		public void WithNoWaitTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithNoWait()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (NoWait)"));
		}

		[Test]
		public void WithNoWaitInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithNoWaitInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (NoWait)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (NoWait)"));
		}

		[Test]
		public void WithPagLockTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithPagLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (PagLock)"));
		}

		[Test]
		public void WithPagLockInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithPagLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (PagLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (PagLock)"));
		}

		[Test]
		public void WithReadCommittedTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithReadCommitted()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (ReadCommitted)"));
		}

		[Test]
		public void WithReadCommittedInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithReadCommittedInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (ReadCommitted)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (ReadCommitted)"));
		}

		[Test]
		public void WithReadCommittedLockTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithReadCommittedLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (ReadCommittedLock)"));
		}

		[Test]
		public void WithReadCommittedLockInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithReadCommittedLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (ReadCommittedLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (ReadCommittedLock)"));
		}

		[Test]
		public void WithReadPastTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithReadPast()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (ReadPast)"));
		}

		[Test]
		public void WithReadPastInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithReadPastInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (ReadPast)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (ReadPast)"));
		}

		[Test]
		public void WithReadUncommittedTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithReadUncommitted()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (ReadUncommitted)"));
		}

		[Test]
		public void WithReadUncommittedInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithReadUncommittedInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (ReadUncommitted)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (ReadUncommitted)"));
		}

		[Test]
		public void WithRepeatableReadTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithRepeatableRead()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (RepeatableRead)"));
		}

		[Test]
		public void WithRepeatableReadInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithRepeatableReadInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (RepeatableRead)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (RepeatableRead)"));
		}

		[Test]
		public void WithRowLockTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithRowLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (RowLock)"));
		}

		[Test]
		public void WithRowLockInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithRowLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (RowLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (RowLock)"));
		}

		[Test]
		public void WithSerializableTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithSerializable()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (Serializable)"));
		}

		[Test]
		public void WithSerializableInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithSerializableInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (Serializable)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (Serializable)"));
		}

		[Test]
		public void WithTabLockTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithTabLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (TabLock)"));
		}

		[Test]
		public void WithTabLockInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithTabLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (TabLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (TabLock)"));
		}

		[Test]
		public void WithTabLockXTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithTabLockX()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (TabLockX)"));
		}

		[Test]
		public void WithTabLockXInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithTabLockXInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (TabLockX)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (TabLockX)"));
		}

		[Test]
		public void WithUpdLockTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithUpdLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (UpdLock)"));
		}

		[Test]
		public void WithUpdLockInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithUpdLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (UpdLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (UpdLock)"));
		}

		[Test]
		public void WithXLockTableTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlServer()
					.WithXLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (XLock)"));
		}

		[Test]
		public void WithXLockInScopeTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlServer()
			.WithXLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (XLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (XLock)"));
		}

		[Test]
		public void OptionHashGroupTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionHashGroup();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.HashGroup})"));
		}

		[Test]
		public void OptionOrderGroupTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionOrderGroup();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.OrderGroup})"));
		}

		[Test]
		public void OptionConcatUnionTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionConcatUnion();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.ConcatUnion})"));
		}

		[Test]
		public void OptionHashUnionTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionHashUnion();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.HashUnion})"));
		}

		[Test]
		public void OptionMergeUnionTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionMergeUnion();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.MergeUnion})"));
		}

		[Test]
		public void OptionLoopJoinTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionLoopJoin();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.LoopJoin})"));
		}

		[Test]
		public void OptionHashJoinTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionHashJoin();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.HashJoin})"));
		}

		[Test]
		public void OptionMergeJoinTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionMergeJoin();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.MergeJoin})"));
		}

		[Test]
		public void OptionExpandViewsTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionExpandViews();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.ExpandViews})"));
		}

		[Test]
		public void OptionFastTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionFast(10);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.Fast(10)})"));
		}

		[Test]
		public void OptionForceOrderTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionForceOrder();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.ForceOrder})"));
		}

		[Test]
		public void OptionIgnoreNonClusteredColumnStoreIndexTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionIgnoreNonClusteredColumnStoreIndex();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.IgnoreNonClusteredColumnStoreIndex})"));
		}

		[Test]
		public void OptionKeepPlanTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionKeepPlan();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.KeepPlan})"));
		}

		[Test]
		public void OptionKeepFixedPlanTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionKeepFixedPlan();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.KeepFixedPlan})"));
		}

		[Test]
		public void OptionMaxGrantPercentTest([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionMaxGrantPercent(10);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.MaxGrantPercent(10)})"));
		}

		[Test]
		public void OptionMinGrantPercentTest([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionMinGrantPercent(10);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.MinGrantPercent(10)})"));
		}

		[Test]
		public void OptionMaxDopTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionMaxDop(10);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.MaxDop(10)})"));
		}

		[Test]
		public void OptionMaxRecursionTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionMaxRecursion(10);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.MaxRecursion(10)})"));
		}

		[Test]
		public void OptionNoPerformanceSpoolTest([IncludeDataSources(true, TestProvName.AllSqlServer2019Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionNoPerformanceSpool();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.NoPerformanceSpool})"));
		}

		[Test]
		public void OptionOptimizeForUnknownTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionOptimizeForUnknown();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.OptimizeForUnknown})"));
		}

		[Test]
		public void OptionQueryTraceOnTest([IncludeDataSources(true, TestProvName.AllSqlServerNoAzure)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionQueryTraceOn(10);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.QueryTraceOn(10)})"));
		}

		[Test]
		public void OptionRecompileTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionRecompile();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.Recompile})"));
		}

		[Test]
		public void OptionRobustPlanTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsSqlServer()
			.OptionRobustPlan();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"OPTION ({SqlServerHints.Query.RobustPlan})"));
		}

	}
}
