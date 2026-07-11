using System;

using BitFaster.Caching;
using BitFaster.Caching.Lfu;
using BitFaster.Caching.Lru;

namespace LinqToDB.Internal.Cache
{
	/// <summary>
	/// A bounded, thread-safe cache — the single storage primitive for every linq2db cache whose key space
	/// is unbounded by runtime input (compiled delegates, descriptors, detected versions, query plans).
	/// Backed by <see cref="BitFaster.Caching"/> (<see cref="ConcurrentLru{K,V}"/> / <see cref="ConcurrentTLru{K,V}"/> /
	/// <see cref="ConcurrentLfu{K,V}"/>); self-registers with <see cref="CacheRegistry"/> at construction.
	/// </summary>
	sealed class BoundedCache<TKey,TValue> : ILinqToDBCache
		where TKey : notnull
	{
		readonly ICache<TKey,TValue> _cache;
		readonly int                 _capacity;

		public string    Name { get; }
		public CacheKind Kind { get; }

		/// <param name="name">Stable identifier for diagnostics, e.g. <c>"CommandInfo.ObjectReaders"</c>.</param>
		/// <param name="capacity">Maximum number of entries before eviction kicks in.</param>
		/// <param name="expireAfterAccess">Optional idle timeout; entries not accessed within this window expire.</param>
		/// <param name="policy">Eviction policy (see <see cref="CacheEvictionPolicy"/>).</param>
		/// <param name="kind">Classification reported to the registry (see <see cref="CacheKind"/>).</param>
		/// <param name="scoped">When <see langword="true"/>, the registry holds only a weak reference so the cache
		/// can be collected with its owner; use for per-scope caches. Process-static caches leave this <see langword="false"/>.</param>
		public BoundedCache(
			string              name,
			int                 capacity,
			TimeSpan?           expireAfterAccess = null,
			CacheEvictionPolicy policy            = CacheEvictionPolicy.Lru,
			CacheKind           kind              = CacheKind.BoundedWork,
			bool                scoped            = false)
		{
			Name      = name;
			Kind      = kind;
			_capacity = capacity;
			_cache    = Build(capacity, expireAfterAccess, policy);

			if (scoped)
				CacheRegistry.RegisterScoped(this);
			else
				CacheRegistry.Register(this);
		}

		static ICache<TKey,TValue> Build(int capacity, TimeSpan? ttl, CacheEvictionPolicy policy)
		{
			return policy switch
			{
				CacheEvictionPolicy.Lfu => new ConcurrentLfu<TKey,TValue>(capacity),
				_ when ttl.HasValue     => new ConcurrentTLru<TKey,TValue>(capacity, ttl.Value),
				_                       => new ConcurrentLru<TKey,TValue>(capacity),
			};
		}

		/// <summary>Returns the cached value, creating and caching it via <paramref name="valueFactory"/> on a miss.</summary>
		public TValue GetOrAdd(TKey key, Func<TKey,TValue> valueFactory)
			=> _cache.GetOrAdd(key, valueFactory);

		/// <summary>Attempts to get the value for <paramref name="key"/> without creating it.</summary>
		public bool TryGet(TKey key, out TValue value)
			=> _cache.TryGet(key, out value!);

		/// <summary>Removes the entry for <paramref name="key"/>, if present.</summary>
		public bool TryRemove(TKey key)
			=> _cache.TryRemove(key);

		/// <summary>Current entry count.</summary>
		public long Count => _cache.Count;

		public void Clear() => _cache.Clear();

		public CacheStats GetStats()
		{
			var metrics = _cache.Metrics;

			if (metrics.HasValue)
			{
				var m = metrics.Value!;
				return new CacheStats(Name, Kind, _cache.Count, m.Hits, m.Misses, m.Evicted, _capacity);
			}

			return new CacheStats(Name, Kind, _cache.Count, 0, 0, 0, _capacity);
		}
	}
}
