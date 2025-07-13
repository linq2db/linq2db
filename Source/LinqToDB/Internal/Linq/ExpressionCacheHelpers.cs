using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;
using LinqToDB.Model;

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
					if (node.Value is Array or FormattableString)
						return false;

					return true;
				}
			}

			return false;
		}
	}
}
