using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Expressions.Types
{
	/// <summary>
	/// Converts <see cref="Task{T}"/> or ValueTask&lt;TResult&gt; to <see cref="Task"/>.
	/// </summary>
	internal sealed class GenericTaskToTaskMapper : ICustomMapper
	{
		bool ICustomMapper.CanMap(Expression expression)
		{
			return expression.Type.IsGenericType
				&& (expression.Type.GetGenericTypeDefinition().FullName == "System.Threading.Tasks.ValueTask`1"
					|| expression.Type.GetGenericTypeDefinition() == typeof(Task<>));
		}

		Expression ICustomMapper.Map(Expression expression)
		{
			if (expression.Type.GetGenericTypeDefinition() == typeof(Task<>))
			{
				return Expression.Convert(expression, typeof(Task));
			}

			return Expression.Convert(Expression.Call(expression, "AsTask", []), typeof(Task));
		}
	}
}
