using System;
using System.Text;
using System.Reflection;

namespace LinqToDB.Extensions
{
	using System.Runtime.CompilerServices;

	using Common;
	using Mapping;
	using SqlQuery;

	static class StringExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetHashCodeEx(this string str)
		{
#if NETSTANDARD2_1PLUS
			return str.GetHashCode(StringComparison.Ordinal);
#else
			return str.GetHashCode();
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ReplaceEx(this string str, string oldValue, string? newValue)
		{
#if NETSTANDARD2_1PLUS
			return str.Replace(oldValue, newValue, StringComparison.Ordinal);
#else
			return str.Replace(oldValue, newValue);
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsEx(this string str, string value)
		{
#if NETSTANDARD2_1PLUS
			return str.Contains(value, StringComparison.Ordinal);
#else
			return str.Contains(value);
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsEx(this string str, char value)
		{
#if NETSTANDARD2_1PLUS
			return str.Contains(value, StringComparison.Ordinal);
#else
			return str.IndexOf(value) != -1;
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOfEx(this string str, char value)
		{
#if NETSTANDARD2_1PLUS
			return str.IndexOf(value, StringComparison.Ordinal);
#else
			return str.IndexOf(value);
#endif
		}
	}
}
