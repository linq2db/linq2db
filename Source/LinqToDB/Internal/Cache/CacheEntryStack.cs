// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace LinqToDB.Internal.Cache
{
	internal sealed class CacheEntryStack<TKey,TEntry>
		where TKey: notnull
	{
		private readonly CacheEntryStack<TKey,TEntry>? _previous;
		private readonly CacheEntry<TKey,TEntry>?      _entry;

		private CacheEntryStack()
		{
		}

		private CacheEntryStack(CacheEntryStack<TKey,TEntry> previous, CacheEntry<TKey,TEntry> entry)
		{
			_previous = previous ?? throw new ArgumentNullException(nameof(previous));
			_entry    = entry;
		}

		public static CacheEntryStack<TKey,TEntry> Empty { get; } = new();

		public CacheEntryStack<TKey,TEntry> Push(CacheEntry<TKey,TEntry> c)
		{
			return new(this, c);
		}

		public CacheEntry<TKey,TEntry>? Peek()
		{
			return _entry;
		}
	}
}
