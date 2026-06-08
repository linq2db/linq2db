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

		private const string WhitespacePattern = /* lang=regex */ @"[\r\n\s]+";
#if SUPPORTS_REGEX_GENERATORS
		[GeneratedRegex(WhitespacePattern, RegexOptions.ExplicitCapture)]
		private static partial Regex WhitespaceRegex();
#else
		private static readonly Regex _whitespaceRegex = new(WhitespacePattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
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

		/// <summary>
		/// Determines whether a provider backend assembly with the given simple name is present — that is,
		/// it is already loaded, can be loaded via <see cref="Assembly.Load(string)"/>, or its <c>.dll</c> is
		/// physically deployed next to the linq2db assembly.
		/// </summary>
		/// <remarks>
		/// As a side effect this may load <paramref name="assemblyName"/> into the current context.
		/// The file-existence fallback is intentionally disabled under <c>PublishSingleFile</c> deployments
		/// (where <see cref="Assembly.Location"/> is empty) and never probes the current working directory —
		/// that current-directory fallback was the original single-file detection bug (linq2db#5488).
		/// </remarks>
		/// <param name="assemblyName">Simple name of the provider assembly to look for.</param>
		/// <returns><see langword="true"/> if the assembly is loaded, loadable, or deployed next to linq2db.</returns>
		public static bool IsProviderAssemblyPresent(string assemblyName)
		{
			return TryLoadAssembly(assemblyName, null, out _) != null
				|| ProviderAssemblyFileExists(assemblyName);
		}

		// Best-effort "is the provider dll deployed next to linq2db?" probe that preserves the historical
		// "deployed provider wins" behaviour. If the dll exists but cannot be loaded (binding/version/native
		// dependency issue), selecting that provider is still preferable — the later load failure is a more
		// relevant diagnostic than silently falling back to a different provider.
		private static bool ProviderAssemblyFileExists(string assemblyName)
		{
			// Assembly.Location is empty under PublishSingleFile; return false in that case rather than probing,
			// and never fall back to the current working directory (the original #5488 single-file bug).
			// IL3000 is suppressed because that empty-location case is explicitly handled below.
#pragma warning disable IL3000
			var location = typeof(Tools).Assembly.Location;
#pragma warning restore IL3000

			if (string.IsNullOrEmpty(location))
				return false;

			var directory = Path.GetDirectoryName(location);

			return directory != null
				&& File.Exists(Path.Combine(directory, assemblyName + ".dll"));
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
					return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal))
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
