// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace LinqToDB.Common.Internal.Cache
{
	/// <summary>
	/// Represents a local in-memory cache whose values are not serialized.
	/// </summary>
	public interface IMemoryCache<TKey> : IDisposable
		where TKey: notnull
	{
		/// <summary>
		/// Gets the item associated with this key if present.
		/// </summary>
		/// <param name="key">An object identifying the requested entry.</param>
		/// <param name="value">The located value or null.</param>
		/// <returns>True if the key was found.</returns>
		bool TryGetValue(TKey key, out object? value);

		/// <summary>
		/// Create or overwrite an entry in the cache.
		/// </summary>
		/// <param name="key">An object identifying the entry.</param>
		/// <returns>The newly created <see cref="ICacheEntry{TKey}"/> instance.</returns>
		ICacheEntry<TKey> CreateEntry(TKey key);

		/// <summary>
		/// Removes the object associated with the given key.
		/// </summary>
		/// <param name="key">An object identifying the entry.</param>
		void Remove(TKey key);
	}
}
