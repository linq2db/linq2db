using System;
using System.Linq.Expressions;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq
{
	static class ExpressionCacheHelpers
	{
		public static bool ShouldRemoveConstantFromCache(ConstantExpression node, MappingSchema mappingSchema)
		{
			if (!mappingSchema.IsScalarType(node.Type) && node.Value != null)
			{
				var valueType = node.Value.GetType();

				// Handling UseTableDescriptor case.
				if (valueType == typeof(EntityDescriptor))
					return false;

				if (!mappingSchema.IsScalarType(valueType))
				{
					if (node.Value is Array or FormattableString or RawSqlString)
						return false;

					return true;
				}
			}

			return false;
		}
	}
}
