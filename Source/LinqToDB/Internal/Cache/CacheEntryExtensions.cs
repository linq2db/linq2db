// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace LinqToDB.Internal.Cache
{
	static class CacheEntryExtensions
	{
		/// <summary>
		/// Sets the priority for keeping the cache entry in the cache during a memory pressure tokened cleanup.
		/// </summary>
		/// <param name="entry">The entry to set the priority for.</param>
		/// <param name="priority">The <see cref="CacheItemPriority"/> to set on the entry.</param>
		/// <returns>The <see cref="ICacheEntry{TKey,TEntry}"/> for chaining.</returns>
		public static ICacheEntry<TKey,TEntry> SetPriority<TKey,TEntry>(
			this ICacheEntry<TKey,TEntry> entry,
			CacheItemPriority             priority)
			where TKey: notnull
		{
			entry.Priority = priority;
			return entry;
		}

		/// <summary>
		/// Expire the cache entry if the given <see cref="IChangeToken"/> expires.
		/// </summary>
		/// <param name="entry">The <see cref="ICacheEntry{TKey,TEntry}"/>.</param>
		/// <param name="expirationToken">The <see cref="IChangeToken"/> that causes the cache entry to expire.</param>
		/// <returns>The <see cref="ICacheEntry{TKey,TEntry}"/> for chaining.</returns>
		public static ICacheEntry<TKey,TEntry> AddExpirationToken<TKey,TEntry>(
			this ICacheEntry<TKey,TEntry> entry,
			IChangeToken                  expirationToken)
			where TKey: notnull
		{
			ArgumentNullException.ThrowIfNull(expirationToken);

			entry.ExpirationTokens.Add(expirationToken);
			return entry;
		}

		/// <summary>
		/// Sets an absolute expiration time, relative to now.
		/// </summary>
		/// <param name="entry">The <see cref="ICacheEntry{TKey,TEntry}"/>.</param>
		/// <param name="relative">The <see cref="TimeSpan"/> representing the expiration time relative to now.</param>
		/// <returns>The <see cref="ICacheEntry{TKey,TEntry}"/> for chaining.</returns>
		public static ICacheEntry<TKey,TEntry> SetAbsoluteExpiration<TKey,TEntry>(
			this ICacheEntry<TKey,TEntry> entry,
			TimeSpan                      relative)
			where TKey: notnull
		{
			entry.AbsoluteExpirationRelativeToNow = relative;
			return entry;
		}

		/// <summary>
		/// Sets an absolute expiration date for the cache entry.
		/// </summary>
		/// <param name="entry">The <see cref="ICacheEntry{TKey,TEntry}"/>.</param>
		/// <param name="absolute">A <see cref="DateTimeOffset"/> representing the expiration time in absolute terms.</param>
		/// <returns>The <see cref="ICacheEntry{TKey,TEntry}"/> for chaining.</returns>
		public static ICacheEntry<TKey,TEntry> SetAbsoluteExpiration<TKey,TEntry>(
			this ICacheEntry<TKey,TEntry> entry,
			DateTimeOffset                absolute)
			where TKey : notnull
		{
			entry.AbsoluteExpiration = absolute;
			return entry;
		}

		/// <summary>
		/// Sets how long the cache entry can be inactive (e.g. not accessed) before it will be removed.
		/// This will not extend the entry lifetime beyond the absolute expiration (if set).
		/// </summary>
		/// <param name="entry">The <see cref="ICacheEntry{TKey,TEntry}"/>.</param>
		/// <param name="offset">A <see cref="TimeSpan"/> representing a sliding expiration.</param>
		/// <returns>The <see cref="ICacheEntry{TKey,TEntry}"/> for chaining.</returns>
		public static ICacheEntry<TKey,TEntry> SetSlidingExpiration<TKey,TEntry>(
			this ICacheEntry<TKey,TEntry> entry,
			TimeSpan                      offset)
			where TKey : notnull
		{
			entry.SlidingExpiration = offset;
			return entry;
		}

		/// <summary>
		/// The given callback will be fired after the cache entry is evicted from the cache.
		/// </summary>
		/// <param name="entry">The <see cref="ICacheEntry{TKey,TEntry}"/>.</param>
		/// <param name="callback">The callback to run after the entry is evicted.</param>
		/// <returns>The <see cref="ICacheEntry{TKey,TEntry}"/> for chaining.</returns>
		public static ICacheEntry<TKey,TEntry> RegisterPostEvictionCallback<TKey,TEntry>(
			this ICacheEntry<TKey,TEntry> entry,
			PostEvictionDelegate<TKey>    callback)
			where TKey : notnull
		{
			ArgumentNullException.ThrowIfNull(callback);

			return entry.RegisterPostEvictionCallback(callback, state: null);
		}

		/// <summary>
		/// The given callback will be fired after the cache entry is evicted from the cache.
		/// </summary>
		/// <param name="entry">The <see cref="ICacheEntry{TKey,TEntry}"/>.</param>
		/// <param name="callback">The callback to run after the entry is evicted.</param>
		/// <param name="state">The state to pass to the post-eviction callback.</param>
		/// <returns>The <see cref="ICacheEntry{TKey,TEntry}"/> for chaining.</returns>
		public static ICacheEntry<TKey,TEntry> RegisterPostEvictionCallback<TKey,TEntry>(
			this ICacheEntry<TKey,TEntry> entry,
			PostEvictionDelegate<TKey>    callback,
			object?                       state)
			where TKey : notnull
		{
			ArgumentNullException.ThrowIfNull(callback);

			entry.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration<TKey>()
			{
				EvictionCallback = callback,
				State = state,
			});
			return entry;
		}

		/// <summary>
		/// Sets the value of the cache entry.
		/// </summary>
		/// <param name="entry">The <see cref="ICacheEntry{TKey,TEntry}"/>.</param>
		/// <param name="value">The value to set on the <paramref name="entry"/>.</param>
		/// <returns>The <see cref="ICacheEntry{TKey,TEntry}"/> for chaining.</returns>
		public static ICacheEntry<TKey,TEntry> SetValue<TKey,TEntry>(
			this ICacheEntry<TKey,TEntry> entry,
			TEntry                        value)
			where TKey : notnull
		{
			entry.Value = value;
			return entry;
		}

		/// <summary>
		/// Sets the size of the cache entry value.
		/// </summary>
		/// <param name="entry">The <see cref="ICacheEntry{TKey,TEntry}"/>.</param>
		/// <param name="size">The size to set on the <paramref name="entry"/>.</param>
		/// <returns>The <see cref="ICacheEntry{TKey,TEntry}"/> for chaining.</returns>
		public static ICacheEntry<TKey,TEntry> SetSize<TKey,TEntry>(
			this ICacheEntry<TKey,TEntry> entry,
			long                          size)
			where TKey : notnull
		{
			if (size < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(size), size, $"{nameof(size)} must be non-negative.");
			}

			entry.Size = size;
			return entry;
		}

		/// <summary>
		/// Applies the values of an existing <see cref="MemoryCacheEntryOptions{TKey}"/> to the entry.
		/// </summary>
		/// <param name="entry">The <see cref="ICacheEntry{TKey,TEntry}"/>.</param>
		/// <param name="options">Set the values of these options on the <paramref name="entry"/>.</param>
		/// <returns>The <see cref="ICacheEntry{TKey,TEntry}"/> for chaining.</returns>
		public static ICacheEntry<TKey,TEntry> SetOptions<TKey,TEntry>(this ICacheEntry<TKey,TEntry> entry, MemoryCacheEntryOptions<TKey> options)
			where TKey : notnull
		{
			ArgumentNullException.ThrowIfNull(options);

			entry.AbsoluteExpiration = options.AbsoluteExpiration;
			entry.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
			entry.SlidingExpiration = options.SlidingExpiration;
			entry.Priority = options.Priority;
			entry.Size = options.Size;

			foreach (var expirationToken in options.ExpirationTokens)
			{
				entry.AddExpirationToken(expirationToken);
			}

			foreach (var postEvictionCallback in options.PostEvictionCallbacks)
			{
				entry.RegisterPostEvictionCallback(postEvictionCallback.EvictionCallback, postEvictionCallback.State);
			}

			return entry;
		}
	}
}
