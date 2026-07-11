using System.Runtime.CompilerServices;

namespace LinqToDB.Internal.Cache
{
	/// <summary>
	/// A thread-safe, weak-keyed cache: entries are reclaimed by the GC once the key becomes collectible.
	/// Used for caches keyed on collectible objects (transient <c>MappingSchema</c> references, types from a
	/// collectible <c>AssemblyLoadContext</c>) where a size bound is the wrong tool —
	/// the fix for those is lifetime, not eviction policy. Self-registers with <see cref="CacheRegistry"/>.
	/// </summary>
	/// <remarks>
	/// Backed by <see cref="ConditionalWeakTable{TKey,TValue}"/>. Its <c>Clear()</c>
	/// and enumeration are unavailable on <c>net462</c>/<c>netstandard2.0</c>, so <see cref="Clear"/> swaps in a fresh
	/// table (the old one is collected) and <see cref="GetStats"/> reports <c>Count == -1</c> (unknown).
	/// </remarks>
	sealed class WeakCache<TKey,TValue> : ILinqToDBCache
		where TKey : class
		where TValue : class
	{
		volatile ConditionalWeakTable<TKey,TValue> _table = new();

		public string    Name { get; }
		public CacheKind Kind => CacheKind.WeakKeyed;

		/// <param name="name">Stable identifier for diagnostics.</param>
		/// <param name="scoped">When <see langword="true"/>, the registry holds only a weak reference so the cache can be
		/// collected with its owner; use for per-scope caches. Process-static caches leave this <see langword="false"/>.</param>
		public WeakCache(string name, bool scoped = false)
		{
			Name = name;

			if (scoped)
				CacheRegistry.RegisterScoped(this);
			else
				CacheRegistry.Register(this);
		}

		/// <summary>Returns the cached value, creating and caching it via <paramref name="valueFactory"/> on a miss.</summary>
		public TValue GetValue(TKey key, ConditionalWeakTable<TKey,TValue>.CreateValueCallback valueFactory)
			=> _table.GetValue(key, valueFactory);

		/// <summary>Attempts to get the value for <paramref name="key"/> without creating it.</summary>
		public bool TryGetValue(TKey key, out TValue value)
			=> _table.TryGetValue(key, out value!);

		/// <summary>Removes the entry for <paramref name="key"/>, if present.</summary>
		public bool Remove(TKey key)
			=> _table.Remove(key);

		// No portable ConditionalWeakTable.Clear() on net462/netstandard2.0 — swap in a fresh table instead.
		public void Clear() => _table = new();

		public CacheStats GetStats() => new(Name, Kind, Count: -1, Hits: 0, Misses: 0, Evictions: 0, Capacity: null);
	}
}
