// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace LinqToDB.Internal.Cache
{
	static class MemoryCacheEntryExtensions
	{
		/// <summary>
		/// Sets the priority for keeping the cache entry in the cache during a memory pressure tokened cleanup.
		/// </summary>
		/// <param name="options">The option on which to set the priority.</param>
		/// <param name="priority">The <see cref="CacheItemPriority"/> to set on the option.</param>
		/// <returns>The <see cref="MemoryCacheEntryOptions{TKey}"/> so that additional calls can be chained.</returns>
		public static MemoryCacheEntryOptions<TKey> SetPriority<TKey>(
			this MemoryCacheEntryOptions<TKey> options,
			CacheItemPriority priority)
			where TKey: notnull
		{
			options.Priority = priority;
			return options;
		}

		/// <summary>
		/// Sets the size of the cache entry value.
		/// </summary>
		/// <param name="options">The options to set the entry size on.</param>
		/// <param name="size">The size to set on the <see cref="MemoryCacheEntryOptions{TKey}"/>.</param>
		/// <returns>The <see cref="MemoryCacheEntryOptions{TKey}"/> so that additional calls can be chained.</returns>
		public static MemoryCacheEntryOptions<TKey> SetSize<TKey>(
			this MemoryCacheEntryOptions<TKey> options,
			long size)
			where TKey : notnull
		{
			if (size < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(size), size, $"{nameof(size)} must be non-negative.");
			}

			options.Size = size;
			return options;
		}

		/// <summary>
		/// Expire the cache entry if the given <see cref="IChangeToken"/> expires.
		/// </summary>
		/// <param name="options">The <see cref="MemoryCacheEntryOptions{TKey}"/>.</param>
		/// <param name="expirationToken">The <see cref="IChangeToken"/> that causes the cache entry to expire.</param>
		/// <returns>The <see cref="MemoryCacheEntryOptions{TKey}"/> so that additional calls can be chained.</returns>
		public static MemoryCacheEntryOptions<TKey> AddExpirationToken<TKey>(
			this MemoryCacheEntryOptions<TKey> options,
			IChangeToken expirationToken)
			where TKey : notnull
		{
			ArgumentNullException.ThrowIfNull(expirationToken);

			options.ExpirationTokens.Add(expirationToken);
			return options;
		}

		/// <summary>
		/// Sets an absolute expiration time, relative to now.
		/// </summary>
		/// <param name="options">The <see cref="MemoryCacheEntryOptions{TKey}"/>.</param>
		/// <param name="relative">The expiration time, relative to now.</param>
		/// <returns>The <see cref="MemoryCacheEntryOptions{TKey}"/> so that additional calls can be chained.</returns>
		public static MemoryCacheEntryOptions<TKey> SetAbsoluteExpiration<TKey>(
			this MemoryCacheEntryOptions<TKey> options,
			TimeSpan relative)
			where TKey : notnull
		{
			options.AbsoluteExpirationRelativeToNow = relative;
			return options;
		}

		/// <summary>
		/// Sets an absolute expiration date for the cache entry.
		/// </summary>
		/// <param name="options">The <see cref="MemoryCacheEntryOptions{TKey}"/>.</param>
		/// <param name="absolute">The expiration time, in absolute terms.</param>
		/// <returns>The <see cref="MemoryCacheEntryOptions{TKey}"/> so that additional calls can be chained.</returns>
		public static MemoryCacheEntryOptions<TKey> SetAbsoluteExpiration<TKey>(
			this MemoryCacheEntryOptions<TKey> options,
			DateTimeOffset absolute)
			where TKey : notnull
		{
			options.AbsoluteExpiration = absolute;
			return options;
		}

		/// <summary>
		/// Sets how long the cache entry can be inactive (e.g. not accessed) before it will be removed.
		/// This will not extend the entry lifetime beyond the absolute expiration (if set).
		/// </summary>
		/// <param name="options">The <see cref="MemoryCacheEntryOptions{TKey}"/>.</param>
		/// <param name="offset">The sliding expiration time.</param>
		/// <returns>The <see cref="MemoryCacheEntryOptions{TKey}"/> so that additional calls can be chained.</returns>
		public static MemoryCacheEntryOptions<TKey> SetSlidingExpiration<TKey>(
			this MemoryCacheEntryOptions<TKey> options,
			TimeSpan offset)
			where TKey : notnull
		{
			options.SlidingExpiration = offset;
			return options;
		}

		/// <summary>
		/// The given callback will be fired after the cache entry is evicted from the cache.
		/// </summary>
		/// <param name="options">The <see cref="MemoryCacheEntryOptions{TKey}"/>.</param>
		/// <param name="callback">The callback to register for calling after an entry is evicted.</param>
		/// <returns>The <see cref="MemoryCacheEntryOptions{TKey}"/> so that additional calls can be chained.</returns>
		public static MemoryCacheEntryOptions<TKey> RegisterPostEvictionCallback<TKey>(
			this MemoryCacheEntryOptions<TKey> options,
			PostEvictionDelegate<TKey> callback)
			where TKey : notnull
		{
			ArgumentNullException.ThrowIfNull(callback);

			return options.RegisterPostEvictionCallback(callback, state: null);
		}

		/// <summary>
		/// The given callback will be fired after the cache entry is evicted from the cache.
		/// </summary>
		/// <param name="options">The <see cref="MemoryCacheEntryOptions{TKey}"/>.</param>
		/// <param name="callback">The callback to register for calling after an entry is evicted.</param>
		/// <param name="state">The state to pass to the callback.</param>
		/// <returns>The <see cref="MemoryCacheEntryOptions{TKey}"/> so that additional calls can be chained.</returns>
		public static MemoryCacheEntryOptions<TKey> RegisterPostEvictionCallback<TKey>(
			this MemoryCacheEntryOptions<TKey> options,
			PostEvictionDelegate<TKey> callback,
			object? state)
			where TKey : notnull
		{
			ArgumentNullException.ThrowIfNull(callback);

			options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration<TKey>()
			{
				EvictionCallback = callback,
				State = state,
			});
			return options;
		}
	}
}
