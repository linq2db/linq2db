using System.Linq.Expressions;

using LinqToDB.Linq.Internal;

namespace LinqToDB.Linq
{
	public static class QueryExpressionsExtensions
	{
		public static IQueryExpressions WithMainExpressions(this IQueryExpressions expressions, Expression mainExpression)
		{
			var container = new RuntimeExpressionsContainer(mainExpression);
			return container;
		}
	}
}
