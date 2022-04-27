using System.Linq.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Interceptors
{
	class AggregatedExpressionInterceptor : AggregatedInterceptor<IExpressionInterceptor>, IExpressionInterceptor
	{
		protected override AggregatedInterceptor<IExpressionInterceptor> Create()
		{
			return new AggregatedExpressionInterceptor();
		}

		public Expression ProcessExpression(MappingSchema mappingSchema, Expression expression)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					expression = interceptor.ProcessExpression(mappingSchema, expression);
				return expression;
			});
		}
	}
}
