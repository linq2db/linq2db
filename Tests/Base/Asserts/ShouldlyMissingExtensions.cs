using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using NUnit.Framework;

namespace Shouldly
{
	public static class ShouldlyMissingExtensions
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void ShouldContain(this string actual, string expected, ITimesConstraint times)
		{
			if (times.Type == TimesType.Exactly)
			{
				Assert.That(CountInString(actual, expected), Is.EqualTo(times.Times));
			}
			else if (times.Type == TimesType.AtLeast)
			{
				Assert.That(CountInString(actual, expected), Is.GreaterThanOrEqualTo(times.Times));
			}
			else
			{
				Assert.Fail($"Unknown times constraint: {times.Type}");
			}

			static int CountInString(string str, string fragment)
			{
				var cnt = 0;
				var idx = 0;
				while ((idx = str.IndexOf(fragment, idx)) != -1)
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
				Assert.That(actual, Does.Not.Contain(str));
		}

		public static void ShouldAllSatisfy<T>(this IEnumerable<T> collection, Action<T> asserts)
		{
			foreach (var item in collection)
				asserts(item);
		}
	}
}
