// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace LinqToDB.Common.Internal.Cache
{
	internal class CacheEntryHelper
	{
		private static readonly AsyncLocal<CacheEntryStack> _scopes = new ();

		internal static CacheEntryStack? Scopes
		{
			get => _scopes.Value;
			set => _scopes.Value = value!;
		}

		internal static CacheEntry? Current
		{
			get
			{
				var scopes = GetOrCreateScopes();
				return scopes.Peek();
			}
		}

		internal static IDisposable EnterScope(CacheEntry entry)
		{
			var scopes = GetOrCreateScopes();

			var scopeLease = new ScopeLease(scopes);
			Scopes = scopes.Push(entry);

			return scopeLease;
		}

		private static CacheEntryStack GetOrCreateScopes()
		{
			var scopes = Scopes;
			if (scopes == null)
			{
				scopes = CacheEntryStack.Empty;
				Scopes = scopes;
			}

			return scopes;
		}

		private sealed class ScopeLease : IDisposable
		{
			readonly CacheEntryStack _cacheEntryStack;

			public ScopeLease(CacheEntryStack cacheEntryStack)
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
