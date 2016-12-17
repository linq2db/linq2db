using System;
using System.Collections;
using System.IO;
using System.Reflection;
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

		public static bool IsNullOrEmpty(this ICollection array)
		{
			return array == null || array.Count == 0;
		}

		public static bool IsNullOrEmpty(this string str)
		{
			return string.IsNullOrEmpty(str);
		}

		public static string GetLocation(this Assembly assembly)
		{
			var path = new Uri(assembly.EscapedCodeBase).LocalPath;
			return Path.GetDirectoryName(path);
		}
	}
}
