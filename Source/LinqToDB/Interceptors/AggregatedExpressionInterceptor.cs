using System.Linq.Expressions;

namespace LinqToDB.Interceptors
{
	class AggregatedExpressionInterceptor : AggregatedInterceptor<IExpressionInterceptor>, IExpressionInterceptor
	{
		protected override AggregatedInterceptor<IExpressionInterceptor> Create()
		{
			return new AggregatedExpressionInterceptor();
		}

		public Expression ProcessExpression(Expression expression)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					expression = interceptor.ProcessExpression(expression);
				return expression;
			});
		}
	}
}
