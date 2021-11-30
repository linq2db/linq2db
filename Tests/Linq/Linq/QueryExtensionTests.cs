using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
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
		public void SelfJoinWithDifferentHint([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var query =
					from p in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("NOLOCK")
					join a in db.GetTable<JoinOptimizeTests.AdressEntity>()//.TableHint("READUNCOMMITTED")
						on p.Id equals a.Id //PK column
					select p;

				Debug.WriteLine(query);

				Assert.AreEqual(1, query.GetTableSource().Joins.Count);
			}
		}

		[Test]
		public void SelfJoinWithDifferentHint2([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var query =
					from p in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("NOLOCK")
					join a in db.GetTable<JoinOptimizeTests.AdressEntity>().TableHint("READUNCOMMITTED")
						on p.Id equals a.Id //PK column
					select p;

				Debug.WriteLine(query);

				Assert.AreEqual(1, query.GetTableSource().Joins.Count);
			}
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
