﻿using System;
using System.Runtime.CompilerServices;
using LinqToDB.Extensions;

namespace LinqToDB.Common.Internal
{
	public static class TypeExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Type UnwrapNullableType(this Type type)
			=> Nullable.GetUnderlyingType(type) ?? type;

		/// <summary>
		/// Returns <c>true</c> if type is reference type or <see cref="Nullable{T}"/>.
		/// </summary>
		/// <param name="type">Type to test.</param>
		/// <returns><c>true</c> if type is reference type or <see cref="Nullable{T}"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullableType(this Type type)
			=> !type.IsValueType || type.IsNullable();

		public static bool IsInteger(this Type type)
		{
			type = type.UnwrapNullableType();

			return type    == typeof(int)
			       || type == typeof(long)
			       || type == typeof(short)
			       || type == typeof(byte)
			       || type == typeof(uint)
			       || type == typeof(ulong)
			       || type == typeof(ushort)
			       || type == typeof(sbyte)
			       || type == typeof(char);
		}

		public static bool IsNumericType(this Type? type)
		{
			if (type == null)
				return false;

			type = type.UnwrapNullableType();

			return type.IsInteger()
			       || type == typeof(decimal)
			       || type == typeof(float)
			       || type == typeof(double);
		}

		public static bool IsSignedInteger(this Type type)
		{
			return type    == typeof(int)
			       || type == typeof(long)
			       || type == typeof(short)
			       || type == typeof(sbyte);
		}

		public static bool IsSignedType(this Type? type)
		{
			return type != null &&
			       (IsSignedInteger(type)
			        || type == typeof(decimal)
			        || type == typeof(double)
			        || type == typeof(float)
			       );
		}
	}
}
