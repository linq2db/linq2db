// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace LinqToDB.Common.Internal.Cache
{
	internal class CacheEntryStack<TKey>
		where TKey: notnull
	{
		private readonly CacheEntryStack<TKey>? _previous;
		private readonly CacheEntry<TKey>? _entry;

		private CacheEntryStack()
		{
		}

		private CacheEntryStack(CacheEntryStack<TKey> previous, CacheEntry<TKey> entry)
		{
			if (previous == null)
			{
				ThrowHelper.ThrowArgumentNullException(nameof(previous));
			}

			_previous = previous;
			_entry = entry;
		}

		public static CacheEntryStack<TKey> Empty { get; } = new CacheEntryStack<TKey>();

		public CacheEntryStack<TKey> Push(CacheEntry<TKey> c)
		{
			return new CacheEntryStack<TKey>(this, c);
		}

		public CacheEntry<TKey>? Peek()
		{
			return _entry;
		}
	}
}
