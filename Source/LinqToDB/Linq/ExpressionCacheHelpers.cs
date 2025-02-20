using System;
using System.Linq.Expressions;

using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Linq
{
	static class ExpressionCacheHelpers
	{
		public static bool ShouldRemoveConstantFromCache(ConstantExpression node)
		{
			if (!node.Type.IsScalar() && node.Value != null)
			{
				var valueType = node.Value.GetType();

				// Handling UseTableDescriptor case.
				if (valueType == typeof(EntityDescriptor))
					return false;

				if (!node.Value.GetType().IsScalar())
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
