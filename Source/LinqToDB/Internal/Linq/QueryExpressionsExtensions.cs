using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using LinqToDB.Async;

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
