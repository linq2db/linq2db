using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Expressions;
using LinqToDB.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryExtensionTests : TestBase
	{
		[Test]
		public void EmptyTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			_ = db.Parent.Empty().ToList();
		}

		[Test]
		public void EmptyTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			_ =
			(
				from p in db.Parent.Empty()
				from c in db.Child.John()
				where p.ParentID == c.ParentID
				select new { p, c }
			)
			.ToList();
		}

		[Test]
		public void TableTest([DataSources] string context)
		{
			using var db = GetDataContext(context);

			_ = db.Parent.Comment(t => t.ParentID, "oh yeah").ToList();
		}

		[Test]
		public void TableTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			_ =
			(
				from p in db.Parent.Comment(t => t.ParentID, "oh yeah")
				join c in db.Child.Empty() on p.ParentID equals c.ParentID
				select new { p, c }
			)
			.ToList();
		}

		[Test]
		public void SqlServerTableHintTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.With(Hints.TableHint.NoLock).With(Hints.TableHint.NoWait)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock, NoWait)"));
		}

		[Test]
		public void SqlServerTableHint2005PlusTest(
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
		public void SqlServerTableHint2012PlusTest(
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
		public void SqlServerTableHintSpatialWindowMaxCellsTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.With(Hints.TableHint.SpatialWindowMaxCells(10))
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (SPATIAL_WINDOW_MAX_CELLS=10)"));
		}

		[Test]
		public void SqlServerTableHintIndexTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
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
		public void SqlServerTableHintForceSeekTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child.WithForceSeek("IX_ChildIndex", c => c.ParentID)
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (ForceSeek (IX_ChildIndex (ParentID)))"));
		}

		[Test, Explicit]
		public void SqlServerTableHintForceSeekTest2([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Child.WithForceSeek("IX_ChildIndex")
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (ForceSeek (IX_ChildIndex))"));
		}

		[Test]
		public void SqlServerJoinHintTest(
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
		public void SqlServerJoinHintSubQueryTest(
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
		public void SqlServerJoinHintMethodTest(
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
		public void SqlServerJoinHintInnerJoinMethodTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(Hints.JoinHint.Loop, Hints.JoinHint.Hash, Hints.JoinHint.Merge, Hints.JoinHint.Remote)] string hint)
		{
			using var db = GetDataContext(context);

			var q = db.Child.InnerJoin(db.Parent.JoinHint(hint), (c, p) => c.ParentID == p.ParentID, (c, p) => p);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"INNER {hint} JOIN"));
		}

		[Test]
		public void SqlServerJoinHintRightJoinMethodTest(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context,
			[Values(Hints.JoinHint.Hash, Hints.JoinHint.Merge)] string hint)
		{
			using var db = GetDataContext(context);

			var q = db.Child.RightJoin(db.Parent.JoinHint(hint), (c, p) => c.ParentID == p.ParentID, (c, p) => p);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"RIGHT {hint} JOIN"));
		}

		[Test]
		public void SqlServerTableHintDeleteMethodTest([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
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
		public void SqlServerQueryHintTest(
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
		public void SqlServerQueryHint2008PlusTest(
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
		public void SqlServerQueryHintFastTest(
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
		public void SqlServerQueryHintMaxGrantPercentTest(
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
		public void SqlServerQueryHintMinGrantPercentTest(
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
		public void SqlServerQueryHintMaxDopTest(
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
		public void SqlServerQueryHintMaxRecursionTest(
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
		public void SqlServerQueryHintOptimizeForTest(
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
		public void SqlServerQueryHintQueryTraceOnTest(
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
	}

	public static class QueryExtensions
	{
		[Sql.QueryExtension(Sql.QueryExtensionScope.Table)]
		public static ITable<T> Empty<T>(this ITable<T> table)
			where T : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Empty, table),
				table.Expression);

			return table;
		}

		[Sql.QueryExtension(Sql.QueryExtensionScope.Join)]
		public static IQueryable<TSource> John<TSource>(this IQueryable<TSource> source)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(John, source),
					currentSource.Expression));
		}

		[Sql.QueryExtension(Sql.QueryExtensionScope.Table)]
		public static ITable<TSource> Comment<TSource,TValue>(
			this ITable<TSource>             table,
			Expression<Func<TSource,TValue>> expr,
			[SqlQueryDependent] string       comment)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Comment, table, expr, comment),
				table.Expression, Expression.Quote(expr), Expression.Constant(comment));

			return table;
		}
	}
}
