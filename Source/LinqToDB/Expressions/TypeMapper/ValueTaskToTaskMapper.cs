using System;
using System.Linq.Expressions;
using LinqToDB.Common;

namespace LinqToDB.Expressions
{
	internal class ValueTaskToTaskMapper : ICustomMapper
	{
		Expression ICustomMapper.Map(Expression expression)
		{
			if ((!expression.Type.IsGenericType && expression.Type.FullName == "System.Threading.Tasks.ValueTask")
				|| (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition().FullName == "System.Threading.Tasks.ValueTask`1"))
			{
				return Expression.Call(expression, "AsTask", Array<Type>.Empty);
			}

			throw new LinqToDBException($"{nameof(ValueTaskToTaskMapper)} mapper applied to non-ValueTask type: {expression.Type}");
		}
	}
}
