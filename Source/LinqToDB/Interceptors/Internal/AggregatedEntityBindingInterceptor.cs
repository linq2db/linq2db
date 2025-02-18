using LinqToDB.Internal.Expressions;

namespace LinqToDB.Interceptors.Internal
{
	sealed class AggregatedEntityBindingInterceptor : AggregatedInterceptor<IEntityBindingInterceptor>, IEntityBindingInterceptor
	{
		public SqlGenericConstructorExpression ConvertConstructorExpression(SqlGenericConstructorExpression expression)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
				{
					expression = interceptor.ConvertConstructorExpression(expression);
				}

				return expression;
			});
		}
	}
}
