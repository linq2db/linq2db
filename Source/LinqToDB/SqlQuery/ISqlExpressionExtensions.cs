using System;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.SqlQuery
{
	public static class ISqlExpressionExtensions
	{
		public static bool AreEqual(this ISqlExpression? left, ISqlExpression? right, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (left == null && right == null)
				return true;
			if (left == null || right == null)
				return false;
			return left.Equals(right, comparer);
		}
	}
}
