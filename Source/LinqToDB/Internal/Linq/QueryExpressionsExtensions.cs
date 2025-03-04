using System.Linq.Expressions;

namespace LinqToDB.Internal.Linq
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
