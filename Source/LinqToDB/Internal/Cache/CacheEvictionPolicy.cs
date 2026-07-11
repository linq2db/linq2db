namespace LinqToDB.Internal.Cache
{
	/// <summary>Eviction policy for a bounded <see cref="BoundedCache{TKey,TValue}"/>.</summary>
	enum CacheEvictionPolicy
	{
		/// <summary>Least-recently-used (2Q). Cheap; the right default for most work caches.</summary>
		Lru,

		/// <summary>Frequency-aware (W-TinyLFU). Higher hit-rate for skewed access patterns; used by the query cache.</summary>
		Lfu,
	}
}
