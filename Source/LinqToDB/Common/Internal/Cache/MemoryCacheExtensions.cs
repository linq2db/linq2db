// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace LinqToDB.Common.Internal.Cache
{
	public static class CacheExtensions
	{
		public static object? Get(this IMemoryCache cache, object key)
		{
			cache.TryGetValue(key, out object? value);
			return value;
		}

		[return: MaybeNull]
		public static TItem Get<TItem>(this IMemoryCache cache, object key)
		{
			return (TItem)(cache.Get(key) ?? default(TItem));
		}

		public static bool TryGetValue<TItem>(this IMemoryCache cache, object key, out TItem? value)
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

		public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value)
		{
			using ICacheEntry entry = cache.CreateEntry(key);
			entry.Value = value;

			return value;
		}

		public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, DateTimeOffset absoluteExpiration)
		{
			using ICacheEntry entry = cache.CreateEntry(key);
			entry.AbsoluteExpiration = absoluteExpiration;
			entry.Value = value;

			return value;
		}

		public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, TimeSpan absoluteExpirationRelativeToNow)
		{
			using ICacheEntry entry = cache.CreateEntry(key);
			entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
			entry.Value = value;

			return value;
		}

		public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, IChangeToken expirationToken)
		{
			using ICacheEntry entry = cache.CreateEntry(key);
			entry.AddExpirationToken(expirationToken);
			entry.Value = value;

			return value;
		}

		public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, MemoryCacheEntryOptions options)
		{
			using ICacheEntry entry = cache.CreateEntry(key);
			if (options != null)
			{
				entry.SetOptions(options);
			}

			entry.Value = value;

			return value;
		}

		public static TItem GetOrCreate<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, TItem> factory)
		{
			if (!cache.TryGetValue(key, out var result))
			{
				using ICacheEntry entry = cache.CreateEntry(key);

				result = factory(entry);
				entry.Value = result;
			}

			return (TItem)result!;
		}

		public static TItem GetOrCreate<TItem, TKey, TContext>(this IMemoryCache cache, TKey key, TContext context, Func<ICacheEntry, TKey, TContext, TItem> factory)
					where TKey : notnull
		{
			if (!cache.TryGetValue(key, out var result))
			{
				using ICacheEntry entry = cache.CreateEntry(key);

				result = factory(entry, key, context);
				entry.Value = result;
			}

			return (TItem)result!;
		}

		public static async Task<TItem> GetOrCreateAsync<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
		{
			if (!cache.TryGetValue(key, out object? result))
			{
				using ICacheEntry entry = cache.CreateEntry(key);

				result = await factory(entry).ConfigureAwait(false);
				entry.Value = result;
			}

			return (TItem)result!;
		}
	}
}
