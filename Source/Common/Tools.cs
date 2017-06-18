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

#if !NETFX_CORE

		public static string GetPath(this Assembly assembly)
		{
			return Path.GetDirectoryName(assembly.GetFileName());
		}

		public static string GetFileName(this Assembly assembly)
		{
			return assembly.CodeBase.GetPathFromUri();
		}

		public static string GetPathFromUri(this string uriString)
		{
			try
			{
				var uri = new Uri(Uri.EscapeUriString(uriString));
				var path = 
					  Uri.UnescapeDataString(uri.AbsolutePath)
					+ Uri.UnescapeDataString(uri.Query)
					+ Uri.UnescapeDataString(uri.Fragment);

				return Path.GetFullPath(path);
			}
			catch (Exception ex)
			{
				throw new LinqToDBException("Error while trying to extract path from " + uriString + " " + ex.Message, ex);
			}
		}

#endif
	}
}
