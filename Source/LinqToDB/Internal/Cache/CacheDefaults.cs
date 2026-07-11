namespace LinqToDB.Internal.Cache
{
	/// <summary>Shared default values for linq2db work caches.</summary>
	static class CacheDefaults
	{
		/// <summary>
		/// "Effectively unbounded" safety capacity for the work caches migrated off the old MemoryCache clone.
		/// Chosen high enough that realistic workloads never evict (preserving the pre-unification behavior,
		/// which was unbounded + sliding-TTL) yet finite enough to bound catastrophic runaway growth.
		/// Real, per-cache tunable limits become opt-in later via the configuration surface.
		/// </summary>
		public const int WorkCacheCapacity = 100_000;
	}
}
