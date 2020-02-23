using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public static class TypeWrapperExtensions
	{
		/// <summary>
		/// Evaluates real value without wrapping it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="instance">Wrapper instance.</param>
		/// <param name="func">Expression for evaluation</param>
		/// <returns>Real value.</returns>
		public static object? Evaluate<T>(this T instance, Expression<Func<T, object?>> func)
			where T: TypeWrapper
		{
			var result = instance.mapper_.Evaluate(instance, func);
			return result;
		}
	}
}
