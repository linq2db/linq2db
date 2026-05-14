using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shouldly
{
	public static class ShouldlyMissingExtensions
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void ShouldContain(this string actual, string expected, ITimesConstraint times)
		{
			var count = CountInString(actual, expected);

			switch (times.Type)
			{
				case TimesType.Exactly: count.ShouldBe(times.Times);                   break;
				case TimesType.AtLeast: count.ShouldBeGreaterThanOrEqualTo(times.Times); break;
				default: throw new ShouldAssertException($"Unknown times constraint: {times.Type}");
			}

			static int CountInString(string str, string fragment)
			{
				var cnt = 0;
				var idx = 0;
				while ((idx = str.IndexOf(fragment, idx, StringComparison.Ordinal)) != -1)
				{
					cnt++;
					idx++;
				}

				return cnt;
			}
		}

		public static void ShouldNotContainAny(this string actual, params string[] notFound)
		{
			foreach (var str in notFound)
				actual.ShouldNotContain(str);
		}

		public static void ShouldAllSatisfy<T>(this IEnumerable<T> collection, Action<T> asserts)
		{
			foreach (var item in collection)
				asserts(item);
		}
	}
}
