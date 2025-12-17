using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using LinqToDB.Data;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Various general-purpose helpers.
	/// </summary>
	public static partial class Tools
	{
		/// <summary>
		/// Checks that collection is not null and have at least one element.
		/// </summary>
		/// <param name="array">Collection to check.</param>
		/// <returns><see langword="true"/> if collection is null or contains no elements, <see langword="false"/> otherwise.</returns>
		public static bool IsNullOrEmpty([NotNullWhen(false)] this ICollection? array)
		{
			return array == null || array.Count == 0;
		}

		/// <summary>
		/// Returns path to original directory with provided assembly.
		/// </summary>
		/// <param name="assembly">Assembly.</param>
		/// <returns>Assembly directory path.</returns>
		internal static string GetPath(this Assembly assembly)
		{
			return Path.GetDirectoryName(assembly.GetFileName())
				?? throw new InvalidOperationException($"Cannot get path to {assembly.GetFileName()}");
		}

		/// <summary>
		/// Returns original path to assembly file.
		/// </summary>
		/// <param name="assembly">Assembly.</param>
		/// <returns>Assembly file path.</returns>
		internal static string GetFileName(this Assembly assembly)
		{
			return assembly.Location;
		}

		private const string WhitespacePattern = /* lang=regex */ @"[\r\n\s]+";
#if SUPPORTS_REGEX_GENERATORS
		[GeneratedRegex(WhitespacePattern, RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1)]
		private static partial Regex WhitespaceRegex();
#else
		private static readonly Regex _whitespaceRegex = new(WhitespacePattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(1));
		private static Regex WhitespaceRegex() => _whitespaceRegex;
#endif

		public static string ToDebugDisplay(string str)
		{
			return WhitespaceRegex().Replace(str, m => " ").Trim();
		}

		internal static HashSet<T> AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
		{
			foreach (var item in items)
				hashSet.Add(item);
			return hashSet;
		}

		public static IQueryable<T> CreateEmptyQuery<T>()
		{
			return Enumerable.Empty<T>().AsQueryable();
		}

		public static Assembly? TryLoadAssembly(string? assemblyName, string? providerFactory)
		{
			return TryLoadAssembly(assemblyName, providerFactory, out _);
		}

		internal static Assembly? TryLoadAssembly(string? assemblyName, string? providerFactory, out Exception? exception)
		{
			exception = null;

			if (assemblyName != null)
			{
				try
				{
					// first try to get already loaded assembly as under .net framework
					// we can end up with multiple versions of assemblies in memory which
					// doesn't make sense and actually breaks T4 templates
					// https://github.com/linq2db/linq2db/issues/3218
					return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName)
					       ?? Assembly.Load(assemblyName);
				}
				catch (Exception ex)
				{
					exception = ex;
				}
			}

#if !NETSTANDARD2_0
			try
			{
				if (providerFactory != null)
					return DbProviderFactories.GetFactory(providerFactory).GetType().Assembly;
			}
			catch (Exception ex)
			{
				exception = ex;
			}
#endif

			return null;
		}
	}
}
