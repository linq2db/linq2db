namespace LinqToDB.Internal.Cache
{
	/// <summary>
	/// Public, point-in-time snapshot of one linq2db cache's counters, returned by
	/// <see cref="LinqToDB.Linq.Tools.GetCacheStatistics"/>.
	/// </summary>
	public sealed class CacheStatistics
	{
		internal CacheStatistics(in CacheStats stats)
		{
			Name      = stats.Name;
			Kind      = stats.Kind;
			Count     = stats.Count;
			Hits      = stats.Hits;
			Misses    = stats.Misses;
			Evictions = stats.Evictions;
			Capacity  = stats.Capacity;
		}

		/// <summary>Stable identifier of the cache, e.g. <c>"MappingSchema.EntityDescriptors"</c>.</summary>
		public string Name { get; }

		/// <summary>Classification of the cache.</summary>
		public CacheKind Kind { get; }

		/// <summary>Current entry count, or <c>-1</c> when the backing store cannot report a size.</summary>
		public long Count { get; }

		/// <summary>Total hits since the last clear (may be <c>0</c> when hit-counting is disabled for the cache).</summary>
		public long Hits { get; }

		/// <summary>Total misses since the last clear.</summary>
		public long Misses { get; }

		/// <summary>Total evictions since the last clear (<c>0</c> when the cache does not track evictions).</summary>
		public long Evictions { get; }

		/// <summary>Configured entry capacity, or <see langword="null"/> when the cache is unbounded.</summary>
		public long? Capacity { get; }
	}
}
