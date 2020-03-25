using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	public static class Internals
	{
		public static IQueryable<T> CreateExpressionQueryInstance<T>(IDataContext dataContext, Expression expression)
		{
			return new ExpressionQueryImpl<T>(dataContext, expression);
		}

		public static IDataContext GetDataContext<T>(IQueryable<T> queryable)
		{
			switch (queryable)
			{
				case ExpressionQuery<T> query:
					return query.DataContext;
				case ITable<T> table:
					return table.DataContext;
				default:
					return default!;
			}
		}
	}
}
