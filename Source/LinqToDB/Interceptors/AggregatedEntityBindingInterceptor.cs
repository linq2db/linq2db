using System.Collections.Generic;

namespace LinqToDB.Interceptors.Internal
{
	using LinqToDB.Expressions;
	using LinqToDB.Reflection;

	sealed class AggregatedEntityBindingInterceptor : AggregatedInterceptor<IEntityBindingInterceptor>, IEntityBindingInterceptor
	{
		public IReadOnlyDictionary<int, MemberAccessor>? TryMapMembersToConstructor(TypeAccessor typeAccessor)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
				{
					var mappings = interceptor.TryMapMembersToConstructor(typeAccessor);
					if (mappings != null)
						return mappings;
				}
				return null;
			});
		}

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
