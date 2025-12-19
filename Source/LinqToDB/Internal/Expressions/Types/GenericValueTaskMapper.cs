using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using LinqToDB.Expressions;

namespace LinqToDB.Internal.Expressions.Types
{
	internal sealed class GenericValueTaskMapper<ToType> : ICustomMapper
		where ToType : TypeWrapper
	{
		bool ICustomMapper.CanMap(Expression expression)
		{
			return expression.Type.IsGenericType
				&& (string.Equals(expression.Type.GetGenericTypeDefinition().FullName, "System.Threading.Tasks.ValueTask`1", StringComparison.Ordinal));
		}

		Expression ICustomMapper.Map(TypeMapper mapper, Expression expression)
		{
			var fromType = expression.Type.GenericTypeArguments[0];

			return Expression.Call(_convert.MakeGenericMethod(fromType, typeof(ToType)), expression, Expression.Constant((Func<object, ToType>)mapper.Wrap<ToType>));
		}

#pragma warning disable CA2012 // Use ValueTasks correctly
		static readonly MethodInfo _convert = MemberHelper.MethodOfGeneric<ValueTask<string>, Func<object, int>>((f, c) => Convert(f, c));
#pragma warning restore CA2012 // Use ValueTasks correctly

		static async ValueTask<TTo> Convert<TFrom, TTo>(ValueTask<TFrom> task, Func<object, TTo> factory)
		{
			var result = await task.ConfigureAwait(false);
			return factory(result!);
		}
	}
}
