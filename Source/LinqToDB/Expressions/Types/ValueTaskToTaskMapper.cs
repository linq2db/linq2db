using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqToDB.Expressions.Types
{
	public class ValueTaskToTaskMapper : ICustomMapper
	{
		bool ICustomMapper.CanMap(Expression expression)
		{
			return (!expression.Type.IsGenericType && expression.Type.FullName == "System.Threading.Tasks.ValueTask")
				|| (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition().FullName == "System.Threading.Tasks.ValueTask`1");
		}

		Expression ICustomMapper.Map(Expression expression)
		{
			return Expression.Call(expression, "AsTask", []);
		}
	}

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
