using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Linq;

namespace LinqToDB.Internal.Linq
{
	public static class Internals
	{
		public static IQueryable<T> CreateExpressionQueryInstance<T>(IDataContext dataContext, Expression expression)
		{
			return new ExpressionQueryImpl<T>(dataContext, expression);
		}

		public static IDataContext? GetDataContext<T>(IQueryable<T> queryable)
		{
			return queryable switch
			{
				IExpressionQuery query => query.DataContext,
				_                      => null,
			};
		}

		public static IDataContext? GetDataContext<T>(IUpdatable<T> updatable)
		{
			return GetDataContext(((LinqExtensions.Updatable<T>)updatable).Query);
		}

		public static IDataContext? GetDataContext<T>(IValueInsertable<T> insertable)
		{
			return GetDataContext(((LinqExtensions.ValueInsertable<T>)insertable).Query);
		}

		public static IDataContext? GetDataContext<TSource, TTarget>(ISelectInsertable<TSource, TTarget> insertable)
		{
			return GetDataContext(((LinqExtensions.SelectInsertable<TSource, TTarget>)insertable).Query);
		}

		/// <summary>
		/// This method makes all needed executions to fully expose expression.
		/// </summary>
		/// <param name="dataContext"></param>
		/// <param name="expression"></param>
		/// <returns>Fully exposed expression which used by LINQ Translator.</returns>
		public static Expression ExposeQueryExpression(IDataContext dataContext, Expression expression)
		{
			return Builder.ExpressionBuilder.ExposeExpression(expression, dataContext, new Builder.ExpressionTreeOptimizationContext(dataContext), null, false, false);
		}
	}
}
