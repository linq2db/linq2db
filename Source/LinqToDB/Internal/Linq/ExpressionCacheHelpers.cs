using System;
using System.Linq.Expressions;

using LinqToDB.Extensions;

namespace LinqToDB.Internal.Linq
{
	static class ExpressionCacheHelpers
	{
		public static bool ShouldRemoveConstantFromCache(ConstantExpression node)
		{
			if (!node.Type.IsScalar() && node.Value != null)
			{
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
