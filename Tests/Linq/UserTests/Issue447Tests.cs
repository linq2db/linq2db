namespace Tests.UserTests
{
	using System;
	using System.Linq;
	using System.Linq.Expressions;

	using NUnit.Framework;
	using LinqToDB.Common;

	[TestFixture]
	public class Issue447Tests : TestBase
	{
		[Explicit("https://github.com/linq2db/linq2db/issues/447")]
		[Test, DataContextSource()]
		public void TestLinq2DbComplexQuery2(string context)
		{
			var old = Configuration.Linq.UseBinaryAggregateExpression;
			try
			{
				Configuration.Linq.UseBinaryAggregateExpression = false;
				using (var db = GetDataContext(context))
				{
					var result = db.Child.Where(c => c.ChildID > 1 || c.ChildID > 0);

					var array = Enumerable.Range(0, 3000).ToArray();

					// Build "where" conditions
					var param = Expression.Parameter(typeof(Model.Child));
					Expression<Func<Model.Child, bool>> predicate = null;

					for (int i = 0; i < array.Length; i++)
					{
						var id = array[i];

						var filterExpression = Expression.Lambda<Func<Model.Child, bool>>
						(Expression.Equal(
							Expression.Convert(Expression.Field(param, "ChildID"), typeof(int)),
							Expression.Constant(id)
						), param);

						predicate = predicate != null ? Or(predicate, filterExpression) : filterExpression;
					}

					result = result.Where(predicate);

					// StackOverflowException cannot be handled and will terminate process
					result.ToString();

				}
			}
			finally
			{
				Configuration.Linq.UseBinaryAggregateExpression = old;
			}
		}

		[Test, DataContextSource()]
		public void TestLinq2DbComplexQuery3(string context)
		{
			var old = Configuration.Linq.UseBinaryAggregateExpression;
			try
			{
				Configuration.Linq.UseBinaryAggregateExpression = true;
				using (var db = GetDataContext(context))
				{
					var result = db.Child.Where(c => c.ChildID > 1 || c.ChildID > 0);

					var array = Enumerable.Range(0, 3000).ToArray();

					// Build "where" conditions
					var param = Expression.Parameter(typeof(Model.Child));
					Expression<Func<Model.Child, bool>> predicate = null;

					for (int i = 0; i < array.Length; i++)
					{
						var id = array[i];

						var filterExpression = Expression.Lambda<Func<Model.Child, bool>>
						(Expression.Equal(
							Expression.Convert(Expression.Field(param, "ChildID"), typeof(int)),
							Expression.Constant(id)
						), param);

						predicate = predicate != null ? Or(predicate, filterExpression) : filterExpression;
					}

					result = result.Where(predicate);

					// StackOverflowException cannot be handled and will terminate process
					result.ToString();
				}
			}
			finally
			{
				Configuration.Linq.UseBinaryAggregateExpression = old;
			}
		}

		public static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
		{
			var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
			return Expression.Lambda<Func<T, bool>>(Expression.And(expr1.Body, invokedExpr), expr1.Parameters);
		}

		public static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
		{
			var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
			return Expression.Lambda<Func<T, bool>>(Expression.Or(expr1.Body, invokedExpr), expr1.Parameters);
		}
	}
}