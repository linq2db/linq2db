using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
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

	/// <summary>
	/// Converts <see cref="Task{T}"/> or <see cref="ValueTask{TResult}"/> to <see cref="Task"/>.
	/// </summary>
	internal class GenericTaskToTaskMapper : ICustomMapper
	{
		Expression ICustomMapper.Map(Expression expression)
		{
			if (expression.Type.IsGenericType)
			{
				if (expression.Type.GetGenericTypeDefinition().FullName == "System.Threading.Tasks.ValueTask`1")
				{
					return Expression.Convert(Expression.Call(expression, "AsTask", Array<Type>.Empty), typeof(Task));
				}

				if (expression.Type.GetGenericTypeDefinition() == typeof(Task<>))
				{
					return Expression.Convert(expression, typeof(Task));
				}
			}

			throw new LinqToDBException($"{nameof(GenericTaskToTaskMapper)} mapper applied to not-Task<T>/ValueTask<T> type: {expression.Type}");
		}
	}
}
