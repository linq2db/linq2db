namespace LinqToDB.Internal.Cache
{
	/// <summary>Point-in-time snapshot of a cache's counters.</summary>
	readonly record struct CacheStats(
		string    Name,
		CacheKind Kind,
		long      Count,
		long      Hits,
		long      Misses,
		long      Evictions,
		long?     Capacity);
}
