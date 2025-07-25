// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Cache
{
	static class CacheExtensions
	{
		public static TItem? Get<TKey,TItem>(this IMemoryCache<TKey,TItem> cache, TKey key)
			where TKey : notnull
		{
			cache.TryGetValue(key, out var value);
			return value;
		}

		public static TItem Set<TKey,TItem>(this IMemoryCache<TKey,TItem> cache, TKey key, TItem value)
			where TKey : notnull
		{
			using var entry = cache.CreateEntry(key);

			entry.Value = value;

			return value;
		}

		public static TItem Set<TKey,TItem>(this IMemoryCache<TKey,TItem> cache, TKey key, TItem value, DateTimeOffset absoluteExpiration)
			where TKey : notnull
		{
			using var entry = cache.CreateEntry(key);

			entry.AbsoluteExpiration = absoluteExpiration;
			entry.Value              = value;

			return value;
		}

		public static TItem Set<TKey,TItem>(this IMemoryCache<TKey,TItem> cache, TKey key, TItem value, TimeSpan absoluteExpirationRelativeToNow)
			where TKey : notnull
		{
			using var entry = cache.CreateEntry(key);

			entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
			entry.Value                           = value;

			return value;
		}

		public static TItem Set<TKey,TItem>(this IMemoryCache<TKey,TItem> cache, TKey key, TItem value, IChangeToken expirationToken)
			where TKey : notnull
		{
			using var entry = cache.CreateEntry(key);

			entry.AddExpirationToken(expirationToken);
			entry.Value = value;

			return value;
		}

		public static TItem Set<TKey,TItem>(this IMemoryCache<TKey,TItem> cache, TKey key, TItem value, MemoryCacheEntryOptions<TKey>? options)
			where TKey : notnull
		{
			using var entry = cache.CreateEntry(key);

			if (options != null)
			{
				entry.SetOptions(options);
			}

			entry.Value = value;

			return value;
		}

		public static TItem GetOrCreate<TKey,TItem>(this IMemoryCache<TKey,TItem> cache, TKey key, Func<ICacheEntry<TKey,TItem>, TItem> factory)
			where TKey : notnull
		{
			if (!cache.TryGetValue(key, out var result))
			{
				using var entry = cache.CreateEntry(key);

				result = factory(entry);
				entry.Value = result;
			}

			return (TItem)result!;
		}

		public static TItem GetOrCreate<TItem,TKey,TContext>(this IMemoryCache<TKey,TItem> cache, TKey key, TContext context, Func<ICacheEntry<TKey,TItem>,TContext,TItem> factory)
			where TKey : notnull
		{
			if (!cache.TryGetValue(key, out var result))
			{
				using var entry = cache.CreateEntry(key);

				result = factory(entry, context);
				entry.Value = result;
			}

			return (TItem)result!;
		}

		public static TItem GetOrCreate<TItem,TKey,TDerivedKey,TContext>(this IMemoryCache<TKey,TItem> cache, TDerivedKey key, TContext context, Func<ICacheEntry<TKey,TItem>,TDerivedKey,TContext,TItem> factory)
			where TKey : notnull
			where TDerivedKey : TKey
		{
			if (!cache.TryGetValue(key, out var result))
			{
				using var entry = cache.CreateEntry(key);

				result = factory(entry, key, context);
				entry.Value = result;
			}

			return (TItem)result!;
		}

		public static async Task<TItem> GetOrCreateAsync<TKey,TItem>(this IMemoryCache<TKey,TItem> cache, TKey key, Func<ICacheEntry<TKey,TItem>,Task<TItem>> factory)
			where TKey : notnull
		{
			if (!cache.TryGetValue(key, out var result))
			{
				using var entry = cache.CreateEntry(key);

				result = await factory(entry).ConfigureAwait(false);
				entry.Value = result;
			}

			return result;
		}
	}
}
