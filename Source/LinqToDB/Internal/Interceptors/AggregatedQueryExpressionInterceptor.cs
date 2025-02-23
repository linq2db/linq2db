using System.Linq.Expressions;

using LinqToDB.Interceptors;

namespace LinqToDB.Internal.Interceptors
{
	class AggregatedQueryExpressionInterceptor : AggregatedInterceptor<IQueryExpressionInterceptor>, IQueryExpressionInterceptor
	{
		public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					expression = interceptor.ProcessExpression(expression, args);
				return expression;
			});
		}
	}
}
