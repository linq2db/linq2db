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
			if (node.Value is null or EntityDescriptor or FormattableString or RawSqlString)
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
