namespace LinqToDB.Internal.Cache
{
	/// <summary>Shared default values for linq2db work caches.</summary>
	static class CacheDefaults
	{
		/// <summary>
		/// Capacity applied to work caches migrated off the old MemoryCache clone. Resolves to
		/// <see cref="LinqToDB.Common.Configuration.Cache.WorkCacheEntryLimit"/> when the user set one; otherwise
		/// falls back to a high "effectively unbounded" default — high enough that realistic workloads never
		/// evict (preserving the pre-unification unbounded + sliding-TTL behavior), yet finite enough to bound
		/// catastrophic runaway growth. Read when each cache is constructed, so configure it at startup.
		/// </summary>
		public static int WorkCacheCapacity => LinqToDB.Common.Configuration.Cache.WorkCacheEntryLimit ?? 100_000;
	}
}
