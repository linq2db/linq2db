using System;
using System.Collections;
using System.IO;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	/// <summary>
	/// Various general-purpose helpers.
	/// </summary>
	public static class Tools
	{
		/// <summary>
		/// Shortcut extension method for <see cref="string.Format(string, object)"/> method.
		/// </summary>
		/// <param name="format">Format string.</param>
		/// <param name="args">Format parameters.</param>
		/// <returns>String, generated from <paramref name="format"/> format string using <paramref name="args"/> parameters.</returns>
		[StringFormatMethod("format")]
		public static string Args(this string format, params object[] args)
		{
			return string.Format(format, args);
		}

		/// <summary>
		/// Checks that collection is not null and have at least one element.
		/// </summary>
		/// <param name="array">Collection to check.</param>
		/// <returns><c>true</c> if collection is null or contains no elements, <c>false</c> otherwise.</returns>
		public static bool IsNullOrEmpty(this ICollection array)
		{
			return array == null || array.Count == 0;
		}

		/// <summary>
		/// Shortcut extension method for <see cref="string.IsNullOrEmpty(string)"/> method.
		/// </summary>
		/// <param name="str">String value to check.</param>
		/// <returns><c>true</c> if string is null or empty, <c>false</c> otherwise.</returns>
		public static bool IsNullOrEmpty(this string str)
		{
			return string.IsNullOrEmpty(str);
		}

#if !NETFX_CORE

		/// <summary>
		/// Returns path to original directory with provided assembly.
		/// </summary>
		/// <param name="assembly">Assembly.</param>
		/// <returns>Assembly directory path.</returns>
		public static string GetPath(this Assembly assembly)
		{
			return Path.GetDirectoryName(assembly.GetFileName());
		}

		/// <summary>
		/// Returns original path to assembly file.
		/// </summary>
		/// <param name="assembly">Assembly.</param>
		/// <returns>Assembly file path.</returns>
		public static string GetFileName(this Assembly assembly)
		{
			return assembly.CodeBase.GetPathFromUri();
		}

		/// <summary>
		/// Converts file path in URI format to absolute path.
		/// </summary>
		/// <param name="uriString">File path in URI format.</param>
		/// <returns>Absolute file path.</returns>
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
