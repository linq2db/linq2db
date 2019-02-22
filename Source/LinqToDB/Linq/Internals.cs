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
	}
}
