using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Various general-purpose helpers.
	/// </summary>
	[PublicAPI]
	public static class Tools
	{
		/// <summary>
		/// Clears all linq2db caches.
		/// </summary>
		public static void ClearAllCaches()
		{
			// Every linq2db cache self-registers with CacheRegistry at construction, so this clears them all —
			// including caches (remote services, provider-version detection, serialization converters, combined
			// mapping schemas) that were previously unreachable from here. Query.ClearCaches() additionally drains
			// the legacy CacheCleaners queue (IdentifierBuilder, MemberCache, ...) that predates the registry.
			CacheRegistry.ClearAll();
			Query.ClearCaches();
		}

		/// <summary>
		/// Returns a point-in-time snapshot of the LINQ query-plan cache statistics (hit/miss totals,
		/// hit rate, cached-plan count). Intended for diagnostics — see <see cref="QueryCacheStatistics"/>.
		/// </summary>
		/// <remarks>
		/// Hits are counted only while <see cref="QueryCache.CollectHitStatistics"/> is enabled (off by default,
		/// as counting every hit adds an atomic write to the query hot path); misses are always counted. For
		/// flow-scoped, parallel-safe measurement of a specific code path, use <see cref="QueryCache.BeginMeasure"/>.
		/// </remarks>
		public static QueryCacheStatistics GetQueryCacheStatistics()
		{
			var cache = QueryCache.Default;
			return new QueryCacheStatistics(cache.TotalHits, cache.TotalMisses, cache.Count);
		}

		/// <summary>
		/// Returns the current entry count of every registered linq2db cache (name + count). Intended for
		/// diagnostics — e.g. confirming caches stay bounded. Hit/miss statistics are query-cache-specific;
		/// see <see cref="GetQueryCacheStatistics"/> for those.
		/// </summary>
		public static IReadOnlyList<CacheEntryCount> GetCacheEntryCounts()
		{
			var snapshot = CacheRegistry.Snapshot();
			var result   = new CacheEntryCount[snapshot.Count];

			for (var i = 0; i < snapshot.Count; i++)
				result[i] = new CacheEntryCount(snapshot[i].Name, snapshot[i].Count);

			return result;
		}
	}
}
