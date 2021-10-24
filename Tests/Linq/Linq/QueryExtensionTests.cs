using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Expressions;
using LinqToDB.Linq;

using NUnit.Framework;
using Tests.Model;

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
		public void SqlServerWithTableHintTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			_ =
			(
				from p in db.Parent.With(TableHint.NoLock).With(TableHint.NoWait)
				select p
			)
			.ToList();
		}

		[Test]
		public void SqlServerWith2005TableHintTest([IncludeDataSources(
			TestProvName.AllSqlServer2005Plus)] string context,
			[Values(
				TableHint.HoldLock,
				TableHint.NoLock,
				TableHint.NoWait,
				TableHint.PagLock,
				TableHint.ReadCommitted,
				TableHint.ReadCommittedLock,
				TableHint.ReadPast,
				TableHint.ReadUncommitted,
				TableHint.RepeatableRead,
				TableHint.RowLock,
				TableHint.Serializable,
				TableHint.TabLock,
				TableHint.TabLockX,
				TableHint.UpdLock,
				TableHint.XLock
				)] TableHint hint)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			var trace = string.Empty;

			db.OnTraceConnection += ti =>
			{
				if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
					trace = ti.SqlText;
			};

			_ =
			(
				from p in db.Parent.With(hint)
				select p
			)
			.ToList();

			Assert.True(trace.Contains($"WITH ({hint})"));
		}

		[Test]
		public void SqlServerWith2012TableHintTest([IncludeDataSources(
			TestProvName.AllSqlServer2012Plus)] string context,
			[Values(
				TableHint.ForceScan
//				TableHint.ForceSeek,
//				TableHint.Snapshot
				)] TableHint hint)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			var trace = string.Empty;

			db.OnTraceConnection += ti =>
			{
				if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
					trace = ti.SqlText;
			};

			_ =
			(
				from p in db.Parent.With(hint)
				select p
			)
			.ToList();

			Assert.True(trace.Contains($"WITH ({hint})"));
		}

		[Test]
		public void SqlServerWithSpatialWindowMaxCellsTableHintTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			_ =
				(
					from p in db.Parent.With(TableHint.SpatialWindowMaxCells, 10)
					select p
				)
				.ToList();
		}

		[Test]
		public void SqlServerWithIndexTableHintTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = (TestDataConnection)GetDataContext(context);

			_ =
				(
					from p in db.Child.WithIndex("IX_ChildIndex").With(TableHint.NoLock)
					select p
				)
				.ToList();
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
