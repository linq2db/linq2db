using System;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue447Tests : TestBase
	{
		[Test]
		public void TestLinqToDBComplexQuery2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Child.Where(c => c.ChildID > 1 || c.ChildID > 0);

				var array = Enumerable.Range(0, 3000).ToArray();

				// Build "where" conditions
				var param = Expression.Parameter(typeof(Model.Child));
				Expression<Func<Model.Child, bool>>? predicate = null;

				for (var i = 0; i < array.Length; i++)
				{
					var id = array[i];

					var filterExpression = Expression.Lambda<Func<Model.Child, bool>>
					(Expression.Equal(
						Expression.Convert(Expression.PropertyOrField(param, "ChildID"), typeof(int)),
						Expression.Constant(id)
					), param);

					predicate = predicate != null ? Or(predicate, filterExpression) : filterExpression;
				}

				result = result.Where(predicate!);

				// StackOverflowException cannot be handled and will terminate process
				_ = result.ToSqlQuery().Sql;
			}
		}

		[Test]
		public void TestLinqToDBComplexQueryCache([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Child.Where(c => c.ChildID > 1 || c.ChildID > 0);

				var array = Enumerable.Range(0, 3000).ToArray();

				// Build "where" conditions
				var param = Expression.Parameter(typeof(Model.Child));
				Expression<Func<Model.Child, bool>>? predicate1 = null;
				Expression<Func<Model.Child, bool>>? predicate2 = null;

				for (var i = 0; i < array.Length; i++)
				{
					var id = array[i];

					var filterExpression = Expression.Lambda<Func<Model.Child, bool>>
					(Expression.Equal(
						Expression.Convert(Expression.PropertyOrField(param, "ChildID"), typeof(int)),
						Expression.Constant(id)
					), param);

					predicate1 = predicate1 != null ? Or(predicate1, filterExpression) : filterExpression;
					predicate2 = predicate2 != null ? Or(predicate2, filterExpression) : filterExpression;
				}

				var result1 = result.Where(predicate1!);
				var result2 = result.Where(predicate2!);

				// StackOverflowException cannot be handled and will terminate process
				_ = result1.ToSqlQuery().Sql;

				var cacheMiss = result1.GetCacheMissCount();

				// from cache
				var sql = result2.ToSqlQuery().Sql;

				result1.GetCacheMissCount().Should().Be(cacheMiss);
			}
		}

		[Test]
		public void TestLinqToDBComplexQueryCacheWithExposing([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Child.Where(c => c.ChildID > 1 || c.ChildID > 0);

				var array = Enumerable.Range(0, 3000).ToArray();

				// Build "where" conditions
				var                                  param      = Expression.Parameter(typeof(Model.Child));
				Expression<Func<Model.Child, bool>>? predicate1 = null;
				Expression<Func<Model.Child, bool>>? predicate2 = null;

				for (var i = 0; i < array.Length; i++)
				{
					var id = array[i];

					var filterExpression = Expression.Lambda<Func<Model.Child, bool>>
					(Expression.Equal(
						Expression.Convert(Expression.PropertyOrField(param, "ChildID"), typeof(int)),
						Expression.Constant(id)
					), param);

					predicate1 = predicate1 != null ? Or(predicate1, filterExpression) : filterExpression;
					predicate2 = predicate2 != null ? Or(predicate2, filterExpression) : filterExpression;
				}

				var result1 = result.Where(predicate1!);

				
				var combined1 =
					from r in result1
					from r1 in result1
					select r1;

				var result2 = result.Where(predicate2!);

				var combined2 =
					from r in result2
					from r1 in result2
					select r1;

				// StackOverflowException cannot be handled and will terminate process
				_ = combined1.ToSqlQuery().Sql;

				var cacheMiss = combined1.GetCacheMissCount();

				// from cache
				var sql = combined2.ToSqlQuery().Sql;

				combined2.GetCacheMissCount().Should().Be(cacheMiss);
			}
		}

		[Test]
		public void TestLinqToDBComplexQueryWithParameters([DataSources] string context)
		{
			var value = true;

			using (var db = GetDataContext(context))
			{
				var query = from p in db.Parent where p.ParentID > 2 && value && true && !false select p;

				var res = query.ToList();
			}
		}

		private static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
		{
			var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
			return Expression.Lambda<Func<T, bool>>(Expression.And(expr1.Body, invokedExpr), expr1.Parameters);
		}

		private static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
		{
			var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
			return Expression.Lambda<Func<T, bool>>(Expression.Or(expr1.Body, invokedExpr), expr1.Parameters);
		}
	}
}
