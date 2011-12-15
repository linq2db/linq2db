using System;
using System.Reflection;
using System.Threading;
using LinqToDB.Extensions;

namespace LinqToDB.Common
{
	using Reflection;

	/// <summary>
	/// Converts a base data type to another base data type.
	/// </summary>
	/// <typeparam name="T">Destination data type.</typeparam>
	/// <typeparam name="P">Source data type.</typeparam>
	public static class Convert<T,P>
	{
		/// <summary>
		/// Represents a method that converts an object from one type to another type.
		/// </summary>
		/// <param name="p">A value to convert to the target type.</param>
		/// <returns>The <typeparamref name="T"/> that represents the converted <paramref name="p"/>.</returns>
		public delegate T ConvertMethod(P p);

		/// <summary>Converts an array of one type to an array of another type.</summary>
		/// <returns>An array of the target type containing the converted elements from the source array.</returns>
		/// <param name="src">The one-dimensional, zero-based <see cref="T:System.Array"></see> to convert to a target type.</param>
		/// <exception cref="T:System.ArgumentNullException">array is null.-or-converter is null.</exception>
		public static T[] FromArray(P[] src)
		{
			var arr = new T[src.Length];

			for (var i = 0; i < arr.Length; i++)
				arr[i] = From(src[i]);

			return arr;
		}

		///<summary>
		/// Converter instance.
		///</summary>
		public static ConvertMethod From = GetConverter();

		///<summary>
		/// Initializes converter instance.
		///</summary>
		///<returns>Converter instance.</returns>
		public static ConvertMethod GetConverter()
		{
			var from = typeof(P);
			var to   = typeof(T);

			// Convert to the same type.
			//
			if (to == from)
				return (ConvertMethod)(object)(Convert<P,P>.ConvertMethod)SameType;

			if (from.IsEnum)
				from = Enum.GetUnderlyingType(from);

			if (to.IsEnum)
				to = Enum.GetUnderlyingType(to);

			if (ReflectionExtensions.IsSameOrParent(to, from))
				return Assignable;

			string methodName;

			if (ReflectionExtensions.IsNullable(to))
				methodName = "ToNullable" + to.GetGenericArguments()[0].Name;
			else if (to.IsArray)
				methodName = "To" + to.GetElementType().Name + "Array";
			else if (to.Name == "Binary")
				methodName = "ToLinq" + to.Name;
			else
				methodName = "To" + to.Name;

			var mi = typeof(Convert).GetMethod(methodName,
				BindingFlags.Public | BindingFlags.Static | BindingFlags.ExactBinding,
				null, new[] { from }, null) ?? FindTypeCastOperator(to) ?? FindTypeCastOperator(from);

			if (mi == null && ReflectionExtensions.IsNullable(to))
			{
				// To-nullable conversion.
				// We have to use reflection to enforce some constraints.
				//
				var toType   = to.GetGenericArguments()[0];
				var fromType = ReflectionExtensions.IsNullable(from)? from.GetGenericArguments()[0]: from;

				methodName = ReflectionExtensions.IsNullable(from) ? "FromNullable" : "From";

				mi = typeof(NullableConvert<,>)
					.MakeGenericType(toType, fromType)
					.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
			}

			if (mi != null)
				return (ConvertMethod)Delegate.CreateDelegate(typeof(ConvertMethod), mi);

			return Default;
		}

		private static MethodInfo FindTypeCastOperator(Type t)
		{
			foreach (var mi in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
			{
				if (mi.IsSpecialName && mi.ReturnType == typeof(T) && (mi.Name == "op_Implicit" || mi.Name == "op_Explicit"))
				{
					var parameters = mi.GetParameters();

					if (1 == parameters.Length && parameters[0].ParameterType == typeof(P))
						return mi;
				}
			}

			return null;
		}

		private static P SameType  (P p) { return p; }
		private static T Assignable(P p) { return (T)(object)p; }
		private static T Default   (P p) { return (T)System.Convert.ChangeType(p, typeof(T), Thread.CurrentThread.CurrentCulture); }
	}

	/// <summary>
	/// Converts a base data type to another base data type.
	/// </summary>
	/// <typeparam name="T">Destination data type.</typeparam>
	public static class ConvertTo<T>
	{
		/// <summary>Returns an <typeparamref name="T"/> whose value is equivalent to the specified value.</summary>
		/// <returns>The <typeparamref name="T"/> that represents the converted <paramref name="p"/>.</returns>
		/// <param name="p">A value to convert to the target type.</param>
		public static T From<P>(P p)
		{
			return Convert<T,P>.From(p);
		}
	}

	internal static class NullableConvert<T,P>
		where T: struct
		where P: struct
	{
		public static T? FromNullable(P? p)
		{
			return p.HasValue? From(p.Value): null;
		}

		public static T? From(P p)
		{
			return Convert<T,P>.From(p);
		}
	}
}
