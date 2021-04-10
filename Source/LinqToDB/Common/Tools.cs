using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LinqToDB.Common
{
	using System.Diagnostics.CodeAnalysis;
	using Reflection;

	/// <summary>
	/// Various general-purpose helpers.
	/// </summary>
	public static class Tools
	{
		/// <summary>
		/// Checks that collection is not null and have at least one element.
		/// </summary>
		/// <param name="array">Collection to check.</param>
		/// <returns><c>true</c> if collection is null or contains no elements, <c>false</c> otherwise.</returns>
		public static bool IsNullOrEmpty([NotNullWhen(false)] this ICollection? array)
		{
			return array == null || array.Count == 0;
		}

		/// <summary>
		/// Returns path to original directory with provided assembly.
		/// </summary>
		/// <param name="assembly">Assembly.</param>
		/// <returns>Assembly directory path.</returns>
		public static string GetPath(this Assembly assembly)
		{
			return Path.GetDirectoryName(assembly.GetFileName())!;
		}

		/// <summary>
		/// Returns original path to assembly file.
		/// </summary>
		/// <param name="assembly">Assembly.</param>
		/// <returns>Assembly file path.</returns>
		public static string GetFileName(this Assembly assembly)
		{
			return assembly.CodeBase!.GetPathFromUri();
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

				var path = string.Empty;

				if (uri.Host != string.Empty)
					path = Path.DirectorySeparatorChar + uriString.Substring(uriString.ToLowerInvariant().IndexOf(uri.Host), uri.Host.Length);

				path +=
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

		public static string ToDebugDisplay(string str)
		{
			static string RemoveDuplicates(string pattern, string input)
			{
				var toSearch = pattern + pattern;
				do
				{
					var s = input.Replace(toSearch, pattern);
					if (s == input)
						break;
					input = s;
				} while (true);

				return input;
			}

			str = RemoveDuplicates("\t",   str);
			str = RemoveDuplicates("\r\n", str);
			str = RemoveDuplicates("\n",   str);

			str = str.Replace("\t",   " ");
			str = str.Replace("\r\n", " ");
			str = str.Replace("\n",   " ");

			return str.Trim();
		}

		internal static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
		{
			foreach (var item in items) 
				hashSet.Add(item);
		}

		public static IQueryable<T> CreateEmptyQuery<T>()
		{
			return Enumerable.Empty<T>().AsQueryable();
		}

		public static IQueryable CreateEmptyQuery(Type elementType)
		{
			var method = Methods.LinqToDB.Tools.CreateEmptyQuery.MakeGenericMethod(elementType);
			return (IQueryable)method.Invoke(null, Array<object>.Empty)!;
		}

		public static Assembly? TryLoadAssembly(string? assemblyName, string? providerFactory)
		{
			if (assemblyName != null)
			{
				try
				{
					return Assembly.Load(assemblyName);
				}
				catch {}
			}

#if !NETSTANDARD2_0
			try
			{
				return DbProviderFactories.GetFactory(providerFactory).GetType().Assembly;
			}
			catch {}
#endif

			return null;
		}
	}
}
