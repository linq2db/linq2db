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

		///// <summary>
		///// Evaluates expression and returns wrapper if it is needed.
		///// </summary>
		///// <typeparam name="T">Instance type.</typeparam>
		///// <typeparam name="TR">Result type.</typeparam>
		///// <param name="instance">Wrapper value</param>
		///// <param name="func">Expression for evaluation and wrapping.</param>
		///// <returns>Wrapped value, if type mapping for <typeparamref name="TR"/> is defined. Otherwise returns unchanged value.</returns>
		//public static TR Wrap<T, TR>(this T instance, Expression<Func<T, TR>> func)
		//	where T: TypeWrapper
		//{
		//	var result = instance.mapper_.Wrap(instance, func);
		//	return result;
		//}

		//public static void WrapAction<T>(this T instance, Expression<Action<T>> action)
		//	where T: TypeWrapper
		//{
		//	instance.mapper_.WrapAction(instance, action);
		//}

		//public static void WrapAction<T, T1>(this T instance, Expression<Action<T, T1>> action)
		//	where T: TypeWrapper
		//{
		//	instance.mapper_.WrapAction(instance, action);
		//}

		//public static void WrapAction<T, T1, T2>(this T instance, Expression<Action<T, T1, T2>> action)
		//	where T: TypeWrapper
		//{
		//	instance.mapper_.WrapAction(instance, action);
		//}

		//public static void SetPropValue<T, TV>(this T instance, Expression<Func<T, TV>> propExpression,
		//	TV value)
		//	where T: TypeWrapper
		//{
		//	instance.mapper_.SetValue(instance.instance_, propExpression, value);
		//}

		public static void WrapEvent<T, TDelegate>(this T instance, string eventName)
			where T         : TypeWrapper
			where TDelegate : Delegate
		{
			instance.mapper_.MapEvent<T, TDelegate>(instance.Events, instance.instance_, eventName);
		}
	}
}
