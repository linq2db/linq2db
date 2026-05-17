using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq
{
	static class ExpressionCacheHelpers
	{
		public static bool ShouldRemoveConstantFromCache(ConstantExpression node, MappingSchema mappingSchema)
		{
			// EntityDescriptor: Handling UseTableDescriptor case.
			// IExpressionCacheKey: values that opt in to participate in the cache key via their own equality.
			if (node.Value is null or EntityDescriptor or FormattableString or RawSqlString or IExpressionCacheKey)
				return false;

			if (!mappingSchema.IsScalarType(node.Type) &&
				!mappingSchema.IsScalarType(node.Value.GetType()))
			{
				return node.Value is not Array;
			}

			return false;
		}
	}
}
