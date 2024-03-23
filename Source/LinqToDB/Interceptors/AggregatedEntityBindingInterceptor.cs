using System.Collections.Generic;

namespace LinqToDB.Interceptors.Internal
{
	using LinqToDB.Expressions;
	using LinqToDB.Reflection;

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
