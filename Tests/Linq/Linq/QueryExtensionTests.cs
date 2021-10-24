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
