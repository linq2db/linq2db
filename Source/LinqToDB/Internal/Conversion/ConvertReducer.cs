using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Expressions.ExpressionVisitors;

namespace LinqToDB.Internal.Conversion
{
	// moved to non-generic class to avoid instance-per-generic
	internal sealed class ConvertReducer
	{
		public static Expression Reducer(Expression e)
		{
			return e is DefaultValueExpression
				? e.Reduce()
				: e;
		}
	}
}
