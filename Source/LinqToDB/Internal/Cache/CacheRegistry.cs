using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace LinqToDB.Internal.Cache
{
	/// <summary>
	/// Central registry of every live linq2db cache. A cache joins the registry at construction
	/// (see <see cref="BoundedCache{TKey,TValue}"/> / <see cref="WeakCache{TKey,TValue}"/>), which makes
	/// "clear all caches" and cache diagnostics <b>provably total</b>: there is no per-type clearer
	/// to wire up and none to forget.
	/// </summary>
	/// <remarks>
	/// Process-static caches register strongly (<see cref="Register{T}"/>); caches whose lifetime is
	/// tied to an owning object (per-<c>MappingSchema</c>, per-provider) register weakly
	/// (<see cref="RegisterScoped{T}"/>) so the registry never extends the owner's lifetime.
	/// Dead weak references are pruned lazily during <see cref="ClearAll"/> / <see cref="Snapshot"/>.
	/// </remarks>
	static class CacheRegistry
	{
		static readonly ConcurrentDictionary<ILinqToDBCache, byte>                  _strong = new();
		static readonly ConcurrentDictionary<WeakReference<ILinqToDBCache>, byte>   _weak   = new();

		/// <summary>Registers a process-static cache. Returns <paramref name="cache"/> for fluent field init.</summary>
		public static T Register<T>(T cache)
			where T : ILinqToDBCache
		{
			_strong.TryAdd(cache, 0);
			return cache;
		}

		/// <summary>Registers a cache whose lifetime is bounded by an owning object; the registry holds only a
		/// weak reference. Returns <paramref name="cache"/> for fluent field init.</summary>
		public static T RegisterScoped<T>(T cache)
			where T : ILinqToDBCache
		{
			_weak.TryAdd(new WeakReference<ILinqToDBCache>(cache), 0);
			return cache;
		}

		/// <summary>Clears every live registered cache and prunes dead weak references.</summary>
		public static void ClearAll()
		{
			foreach (var cache in _strong.Keys)
				cache.Clear();

			foreach (var weak in _weak.Keys)
			{
				if (weak.TryGetTarget(out var cache))
					cache.Clear();
				else
					_weak.TryRemove(weak, out _);
			}
		}

		/// <summary>Clears every live registered cache of the given <paramref name="kind"/>.</summary>
		public static void Clear(CacheKind kind)
		{
			foreach (var cache in _strong.Keys)
				if (cache.Kind == kind)
					cache.Clear();

			foreach (var weak in _weak.Keys)
			{
				if (weak.TryGetTarget(out var cache))
				{
					if (cache.Kind == kind)
						cache.Clear();
				}
				else
					_weak.TryRemove(weak, out _);
			}
		}

		/// <summary>Returns a snapshot of every live registered cache's statistics and prunes dead weak references.</summary>
		public static IReadOnlyList<CacheStats> Snapshot()
		{
			var result = new List<CacheStats>();

			foreach (var cache in _strong.Keys)
				result.Add(cache.GetStats());

			foreach (var weak in _weak.Keys)
			{
				if (weak.TryGetTarget(out var cache))
					result.Add(cache.GetStats());
				else
					_weak.TryRemove(weak, out _);
			}

			return result;
		}
	}
}
