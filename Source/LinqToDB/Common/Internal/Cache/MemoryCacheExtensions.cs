// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

namespace LinqToDB.Common.Internal.Cache
{
	public static class CacheExtensions
	{
		public static object? Get<TKey>(this IMemoryCache<TKey> cache, TKey key)
			where TKey : notnull
		{
			cache.TryGetValue(key, out object? value);
			return value;
		}

		public static TItem? Get<TKey, TItem>(this IMemoryCache<TKey> cache, TKey key)
			where TKey : notnull
		{
			return (TItem?)(cache.Get(key) ?? default(TItem));
		}

		public static bool TryGetValue<TKey, TItem>(this IMemoryCache<TKey> cache, TKey key, out TItem? value)
			where TKey : notnull
		{
			if (cache.TryGetValue(key, out object? result))
			{
				if (result == null)
				{
					value = default;
					return true;
				}

				if (result is TItem item)
				{
					value = item;
					return true;
				}
			}

			value = default;
			return false;
		}

		public static TItem Set<TKey, TItem>(this IMemoryCache<TKey> cache, TKey key, TItem value)
			where TKey : notnull
		{
			using ICacheEntry<TKey> entry = cache.CreateEntry(key);
			entry.Value = value;

			return value;
		}

		public static TItem Set<TKey, TItem>(this IMemoryCache<TKey> cache, TKey key, TItem value, DateTimeOffset absoluteExpiration)
			where TKey : notnull
		{
			using ICacheEntry<TKey> entry = cache.CreateEntry(key);
			entry.AbsoluteExpiration = absoluteExpiration;
			entry.Value = value;

			return value;
		}

		public static TItem Set<TKey, TItem>(this IMemoryCache<TKey> cache, TKey key, TItem value, TimeSpan absoluteExpirationRelativeToNow)
			where TKey : notnull
		{
			using ICacheEntry<TKey> entry = cache.CreateEntry(key);
			entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
			entry.Value = value;

			return value;
		}

		public static TItem Set<TKey, TItem>(this IMemoryCache<TKey> cache, TKey key, TItem value, IChangeToken expirationToken)
			where TKey : notnull
		{
			using ICacheEntry<TKey> entry = cache.CreateEntry(key);
			entry.AddExpirationToken(expirationToken);
			entry.Value = value;

			return value;
		}

		public static TItem Set<TKey, TItem>(this IMemoryCache<TKey> cache, TKey key, TItem value, MemoryCacheEntryOptions<TKey> options)
			where TKey : notnull
		{
			using ICacheEntry<TKey> entry = cache.CreateEntry(key);
			if (options != null)
			{
				entry.SetOptions(options);
			}

			entry.Value = value;

			return value;
		}

		public static TItem GetOrCreate<TKey, TItem>(this IMemoryCache<TKey> cache, TKey key, Func<ICacheEntry<TKey>, TItem> factory)
			where TKey : notnull
		{
			if (!cache.TryGetValue(key, out var result))
			{
				using ICacheEntry<TKey> entry = cache.CreateEntry(key);

				result = factory(entry);
				entry.Value = result;
			}

			return (TItem)result!;
		}

		public static TItem GetOrCreate<TItem, TKey, TContext>(this IMemoryCache<TKey> cache, TKey key, TContext context, Func<ICacheEntry<TKey>, TContext, TItem> factory)
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

		public static TItem GetOrCreate<TItem, TKey, TDerivedKey, TContext>(this IMemoryCache<TKey> cache, TDerivedKey key, TContext context, Func<ICacheEntry<TKey>, TDerivedKey, TContext, TItem> factory)
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

		public static async Task<TItem> GetOrCreateAsync<TKey, TItem>(this IMemoryCache<TKey> cache, TKey key, Func<ICacheEntry<TKey>, Task<TItem>> factory)
			where TKey : notnull
		{
			if (!cache.TryGetValue(key, out object? result))
			{
				using ICacheEntry<TKey> entry = cache.CreateEntry(key);

				result = await factory(entry).ConfigureAwait(false);
				entry.Value = result;
			}

			return (TItem)result!;
		}
	}
}
