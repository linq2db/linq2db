using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions.Types
{
	public sealed class ValueTaskToTaskMapper : ICustomMapper
	{
		bool ICustomMapper.CanMap(Expression expression)
		{
			return (!expression.Type.IsGenericType && string.Equals(expression.Type.FullName, "System.Threading.Tasks.ValueTask", System.StringComparison.Ordinal))
				|| (expression.Type.IsGenericType && string.Equals(expression.Type.GetGenericTypeDefinition().FullName, "System.Threading.Tasks.ValueTask`1", System.StringComparison.Ordinal));
		}

		Expression ICustomMapper.Map(TypeMapper mapper, Expression expression)
		{
			return Expression.Call(expression, "AsTask", []);
		}
	}
}
