using System;
using System.Linq.Expressions;

using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq
{
	static class ExpressionCacheHelpers
	{
		public static bool ShouldRemoveConstantFromCache(ConstantExpression node, MappingSchema mappingSchema)
		{
			if (!mappingSchema.IsScalarType(node.Type) && node.Value != null)
			{
				if (!mappingSchema.IsScalarType(node.Value.GetType()))
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
