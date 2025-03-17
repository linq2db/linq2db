// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace LinqToDB.Internal.Cache
{
	internal sealed class CacheEntryHelper<TKey,TEntry>
		where TKey : notnull
	{
		private static readonly AsyncLocal<CacheEntryStack<TKey,TEntry>> _scopes = new ();

		internal static CacheEntryStack<TKey,TEntry>? Scopes
		{
			get => _scopes.Value;
			set => _scopes.Value = value!;
		}

		internal static CacheEntry<TKey,TEntry>? Current
		{
			get
			{
				var scopes = GetOrCreateScopes();
				return scopes.Peek();
			}
		}

		internal static IDisposable EnterScope(CacheEntry<TKey,TEntry> entry)
		{
			var scopes = GetOrCreateScopes();

			var scopeLease = new ScopeLease(scopes);
			Scopes = scopes.Push(entry);

			return scopeLease;
		}

		private static CacheEntryStack<TKey,TEntry> GetOrCreateScopes()
		{
			return Scopes ??= CacheEntryStack<TKey,TEntry>.Empty;
		}

		private sealed class ScopeLease : IDisposable
		{
			readonly CacheEntryStack<TKey,TEntry> _cacheEntryStack;

			public ScopeLease(CacheEntryStack<TKey,TEntry> cacheEntryStack)
			{
				_cacheEntryStack = cacheEntryStack;
			}

			public void Dispose()
			{
				Scopes = _cacheEntryStack;
			}
		}
	}
}
