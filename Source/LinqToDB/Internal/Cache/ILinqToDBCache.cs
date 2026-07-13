namespace LinqToDB.Internal.Cache
{
	/// <summary>
	/// Common management contract implemented by every linq2db cache so the <see cref="CacheRegistry"/>
	/// can clear it and report its entry count without knowing its concrete type or storage strategy.
	/// This is the unification seam: a cache participates in "clear all caches" and the entry-count
	/// diagnostics simply by implementing this interface and registering itself at construction.
	/// </summary>
	interface ILinqToDBCache
	{
		/// <summary>Stable, human-readable identifier, e.g. <c>"MappingSchema.EntityDescriptors"</c>.</summary>
		string Name { get; }

		/// <summary>Current number of entries held by the cache.</summary>
		long Count { get; }

		/// <summary>Empties the cache. Must be safe to call concurrently with normal cache use.</summary>
		void Clear();
	}
}
