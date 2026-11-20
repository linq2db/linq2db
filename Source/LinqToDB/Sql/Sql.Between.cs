using System;

using LinqToDB.Mapping;

namespace LinqToDB
{
	partial class Sql
	{
		// Translated by SqlFunctionsMemberTranslatorBase. Server-side only: a call whose operands are all client values still
		// becomes SQL BETWEEN, with SQL's NULL semantics.
		[ServerSideOnly]
		public static bool Between<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && value.CompareTo(low) >= 0 && value.CompareTo(high) <= 0;
		}

		[ServerSideOnly]
		public static bool Between<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && value.Value.CompareTo(low) >= 0 && value.Value.CompareTo(high) <= 0;
		}

		[ServerSideOnly]
		public static bool NotBetween<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && (value.CompareTo(low) < 0 || value.CompareTo(high) > 0);
		}

		[ServerSideOnly]
		public static bool NotBetween<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && (value.Value.CompareTo(low) < 0 || value.Value.CompareTo(high) > 0);
		}
	}
}
