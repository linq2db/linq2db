using System;

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
	}
}
