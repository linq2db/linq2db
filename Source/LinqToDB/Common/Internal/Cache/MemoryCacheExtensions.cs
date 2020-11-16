// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

		public static bool TryGetValue<TItem>(this IMemoryCache cache, object key, [MaybeNullWhen(false)] out TItem value)
		{
			if (cache.TryGetValue(key, out object? result))
			{
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
			var entry = cache.CreateEntry(key);
			entry.Value = value;
			entry.Dispose();

			return value;
		}

		public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, DateTimeOffset absoluteExpiration)
		{
			var entry = cache.CreateEntry(key);
			entry.AbsoluteExpiration = absoluteExpiration;
			entry.Value = value;
			entry.Dispose();

			return value;
		}

		public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, TimeSpan absoluteExpirationRelativeToNow)
		{
			var entry = cache.CreateEntry(key);
			entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
			entry.Value = value;
			entry.Dispose();

			return value;
		}

		public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, IChangeToken expirationToken)
		{
			var entry = cache.CreateEntry(key);
			entry.AddExpirationToken(expirationToken);
			entry.Value = value;
			entry.Dispose();

			return value;
		}

		public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, MemoryCacheEntryOptions options)
		{
			using (var entry = cache.CreateEntry(key))
			{
				if (options != null)
				{
					entry.SetOptions(options);
				}

				entry.Value = value;
			}

			return value;
		}

		public static TItem GetOrCreate<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, TItem> factory)
		{
			if (!cache.TryGetValue(key, out var result))
			{
				var entry = cache.CreateEntry(key);
				result = factory(entry);
				entry.SetValue(result);
				// need to manually call dispose instead of having a using
				// in case the factory passed in throws, in which case we
				// do not want to add the entry to the cache
				entry.Dispose();
			}

			return (TItem)result!;
		}

		public static TItem GetOrCreate<TItem, TKey, TContext>(this IMemoryCache cache, TKey key, TContext context, Func<ICacheEntry, TKey, TContext, TItem> factory)
			where TKey: notnull
		{
			if (!cache.TryGetValue(key, out var result))
			{
				var entry = cache.CreateEntry(key);
				result = factory(entry, key, context);
				entry.SetValue(result);
				// need to manually call dispose instead of having a using
				// in case the factory passed in throws, in which case we
				// do not want to add the entry to the cache
				entry.Dispose();
			}

			return (TItem)result!;
		}

		public static async Task<TItem> GetOrCreateAsync<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
		{
			if (!cache.TryGetValue(key, out object? result))
			{
				var entry = cache.CreateEntry(key);
				result = await factory(entry).ConfigureAwait(Configuration.ContinueOnCapturedContext);
				entry.SetValue(result);
				// need to manually call dispose instead of having a using
				// in case the factory passed in throws, in which case we
				// do not want to add the entry to the cache
				entry.Dispose();
			}

			return (TItem)result!;
		}
	}
}
