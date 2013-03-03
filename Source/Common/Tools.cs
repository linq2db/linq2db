using System;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	public static class Tools
	{
		[StringFormatMethod("format")]
		public static string Args(this string format, params object[] args)
		{
			return string.Format(format, args);
		}

		public static bool IsNullOrEmpty<T>(this T[] array)
		{
			return array == null || array.Length == 0;
		}

		public static bool IsNullOrEmpty(this string str)
		{
			return string.IsNullOrEmpty(str);
		}
	}
}
