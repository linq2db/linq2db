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

		static ICache<TKey,TValue> Build(int capacity, TimeSpan? expireAfterAccess, CacheEvictionPolicy policy)
		{
			if (policy == CacheEvictionPolicy.Lfu)
				return new ConcurrentLfu<TKey,TValue>(capacity);

			// Sliding expiration (reset on access) matches the retired MemoryCache clone's SlidingExpiration;
			// it needs the builder — the ConcurrentTLru direct ctor is expire-after-write, not sliding.
			if (expireAfterAccess.HasValue)
				return new ConcurrentLruBuilder<TKey,TValue>()
					.WithCapacity(capacity)
					.WithExpireAfterAccess(expireAfterAccess.Value)
					.Build();

			return new ConcurrentLru<TKey,TValue>(capacity);
		}

		/// <summary>Returns the cached value, creating and caching it via <paramref name="valueFactory"/> on a miss.</summary>
		public TValue GetOrAdd(TKey key, Func<TKey,TValue> valueFactory)
			=> _cache.GetOrAdd(key, valueFactory);

		/// <summary>Returns the cached value, creating it via <paramref name="valueFactory"/> on a miss. The
		/// <paramref name="factoryArgument"/> lets callers pass state without allocating a closure per call.</summary>
		public TValue GetOrAdd<TArg>(TKey key, Func<TKey,TArg,TValue> valueFactory, TArg factoryArgument)
			// BitFaster's closure-free GetOrAdd(key, factory, arg) overload ships only on its net6.0+ assembly;
			// net462/netstandard2.0 consume the netstandard2.0 assembly, which lacks it — fall back to a closure.
#if NET6_0_OR_GREATER
			=> _cache.GetOrAdd(key, valueFactory, factoryArgument);
#else
			=> _cache.GetOrAdd(key, k => valueFactory(k, factoryArgument));
#endif

		/// <summary>Derived-key variant: the concrete <paramref name="key"/> (a subtype of <typeparamref name="TKey"/>)
		/// is passed to the factory strongly-typed, while the cache is keyed by it as <typeparamref name="TKey"/>.
		/// The <paramref name="factoryArgument"/> avoids a per-call closure on net6.0+.</summary>
		public TValue GetOrAdd<TDerivedKey,TArg>(TDerivedKey key, TArg factoryArgument, Func<TDerivedKey,TArg,TValue> valueFactory)
			where TDerivedKey : TKey
#if NET6_0_OR_GREATER
			=> _cache.GetOrAdd(
				key,
				static (_, state) => state.valueFactory(state.key, state.factoryArgument),
				(key, factoryArgument, valueFactory));
#else
			=> _cache.GetOrAdd(key, _ => valueFactory(key, factoryArgument));
#endif

		/// <summary>Attempts to get the value for <paramref name="key"/> without creating it.</summary>
		public bool TryGet(TKey key, out TValue value)
			=> _cache.TryGet(key, out value!);

		/// <summary>Inserts <paramref name="value"/> for <paramref name="key"/>, replacing any existing entry.</summary>
		public void AddOrUpdate(TKey key, TValue value)
			=> _cache.AddOrUpdate(key, value);

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
