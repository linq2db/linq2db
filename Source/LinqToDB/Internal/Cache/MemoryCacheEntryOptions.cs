// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace LinqToDB.Internal.Cache
{
	/// <summary>
	/// Represents the cache options applied to an entry of the <see cref="IMemoryCache{TKey,TItem}"/> instance.
	/// </summary>
	public class MemoryCacheEntryOptions<TKey>
		where TKey: notnull
	{
		private DateTimeOffset? _absoluteExpiration;
		private TimeSpan? _absoluteExpirationRelativeToNow;
		private TimeSpan? _slidingExpiration;
		private long? _size;

		/// <summary>
		/// Gets or sets an absolute expiration date for the cache entry.
		/// </summary>
		public DateTimeOffset? AbsoluteExpiration
		{
			get => _absoluteExpiration;
			set => _absoluteExpiration = value;
		}

		/// <summary>
		/// Gets or sets an absolute expiration time, relative to now.
		/// </summary>
		public TimeSpan? AbsoluteExpirationRelativeToNow
		{
			get => _absoluteExpirationRelativeToNow;
			set
			{
				if (value <= TimeSpan.Zero)
				{
					throw new ArgumentOutOfRangeException(
						nameof(AbsoluteExpirationRelativeToNow),
						value,
						"The relative expiration value must be positive.");
				}

				_absoluteExpirationRelativeToNow = value;
			}
		}

		/// <summary>
		/// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
		/// This will not extend the entry lifetime beyond the absolute expiration (if set).
		/// </summary>
		public TimeSpan? SlidingExpiration
		{
			get => _slidingExpiration;
			set
			{
				if (value <= TimeSpan.Zero)
				{
					throw new ArgumentOutOfRangeException(
						nameof(SlidingExpiration),
						value,
						"The sliding expiration value must be positive.");
				}

				_slidingExpiration = value;
			}
		}

		/// <summary>
		/// Gets the <see cref="IChangeToken"/> instances which cause the cache entry to expire.
		/// </summary>
		public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

		/// <summary>
		/// Gets or sets the callbacks will be fired after the cache entry is evicted from the cache.
		/// </summary>
		public IList<PostEvictionCallbackRegistration<TKey>> PostEvictionCallbacks { get; }
			= new List<PostEvictionCallbackRegistration<TKey>>();

		/// <summary>
		/// Gets or sets the priority for keeping the cache entry in the cache during a
		/// memory pressure triggered cleanup. The default is <see cref="CacheItemPriority.Normal"/>.
		/// </summary>
		public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;

		/// <summary>
		/// Gets or sets the size of the cache entry value.
		/// </summary>
		public long? Size
		{
			get => _size;
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be non-negative.");
				}

				_size = value;
			}
		}
	}
}
