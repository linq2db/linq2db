using System;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	static class Tools
	{
		[StringFormatMethod("format")]
		public static string Args(this string format, params object[] args)
		{
			return string.Format(format, args);
		}
	}
}
