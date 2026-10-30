using System;
using System.Collections;
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

		/// <summary>
		/// Whether the cache key compares <paramref name="constant"/> by value: kept in the key and not a collection,
		/// which the key compares with <see cref="object.Equals(object)"/> (by reference for an array).
		/// </summary>
		public static bool IsComparedByValueInKey(ConstantExpression constant, MappingSchema mappingSchema)
		{
			return !ShouldRemoveConstantFromCache(constant, mappingSchema) && constant.Value is string or not IEnumerable;
		}
	}
}
