using System;
using System.Collections.Generic;
using System.Reflection;

using LinqToDB.Reflection;

namespace LinqToDB.Interceptors.Internal
{
	sealed class AggregatedExpressionInterceptor : AggregatedInterceptor<IExpressionInterceptor>, IExpressionInterceptor
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
	}
}
