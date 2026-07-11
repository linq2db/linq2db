namespace LinqToDB.Internal.Cache
{
	/// <summary>
	/// Common management contract implemented by every linq2db cache so the <see cref="CacheRegistry"/>
	/// can clear it and report statistics without knowing its concrete type or storage strategy.
	/// This is the unification seam: a cache participates in "clear all caches" and diagnostics simply
	/// by implementing this interface and registering itself at construction.
	/// </summary>
	interface ILinqToDBCache
	{
		/// <summary>Stable, human-readable identifier, e.g. <c>"MappingSchema.EntityDescriptors"</c>.</summary>
		string Name { get; }

		/// <summary>Classifies the cache so callers can clear/inspect a subset (see <see cref="CacheKind"/>).</summary>
		CacheKind Kind { get; }

		/// <summary>Empties the cache. Must be safe to call concurrently with normal cache use.</summary>
		void Clear();

		/// <summary>Returns a point-in-time snapshot of the cache's counters.</summary>
		CacheStats GetStats();
	}
}
