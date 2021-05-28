﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Common.Internal.Cache
{
	internal class CacheEntry<TKey> : ICacheEntry<TKey>
		where TKey: notnull
	{
		private bool                                           _disposed;
		private static readonly Action<object>                 ExpirationCallback = ExpirationTokensExpired;
		private readonly Action<CacheEntry<TKey>>              _notifyCacheOfExpiration;
		private readonly Action<CacheEntry<TKey>>              _notifyCacheEntryCommit;
		private IList<IDisposable>?                            _expirationTokenRegistrations;
		private IList<PostEvictionCallbackRegistration<TKey>>? _postEvictionCallbacks;
		private bool                                           _isExpired;

		internal IList<IChangeToken>? _expirationTokens;
		internal DateTimeOffset?      _absoluteExpiration;
		internal TimeSpan?            _absoluteExpirationRelativeToNow;
		private TimeSpan?             _slidingExpiration;
		private long?                 _size;
		private IDisposable?          _scope;
		private object?               _value;
		private bool                  _valueHasBeenSet;

		internal readonly object _lock = new ();

		internal CacheEntry(
			TKey key,
			Action<CacheEntry<TKey>> notifyCacheEntryCommit,
			Action<CacheEntry<TKey>> notifyCacheOfExpiration)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (notifyCacheEntryCommit == null)
			{
				throw new ArgumentNullException(nameof(notifyCacheEntryCommit));
			}

			if (notifyCacheOfExpiration == null)
			{
				throw new ArgumentNullException(nameof(notifyCacheOfExpiration));
			}

			Key = key;
			_notifyCacheEntryCommit = notifyCacheEntryCommit;
			_notifyCacheOfExpiration = notifyCacheOfExpiration;

			_scope = CacheEntryHelper<TKey>.EnterScope(this);
		}

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
		public IList<IChangeToken> ExpirationTokens
		{
			get
			{
				if (_expirationTokens == null)
				{
					_expirationTokens = new List<IChangeToken>();
				}

				return _expirationTokens;
			}
		}

		/// <summary>
		/// Gets or sets the callbacks will be fired after the cache entry is evicted from the cache.
		/// </summary>
		public IList<PostEvictionCallbackRegistration<TKey>> PostEvictionCallbacks
		{
			get
			{
				if (_postEvictionCallbacks == null)
				{
					_postEvictionCallbacks = new List<PostEvictionCallbackRegistration<TKey>>();
				}

				return _postEvictionCallbacks;
			}
		}

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

		public TKey Key { get; private set; }

		public object? Value
		{
			get => _value;
			set
			{
				_value = value;
				_valueHasBeenSet = true;
			}
		}

		internal DateTimeOffset LastAccessed { get; set; }

		internal EvictionReason EvictionReason { get; private set; }

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;

				// Ensure the _scope reference is cleared because it can reference other CacheEntry instances.
				// This CacheEntry is going to be put into a MemoryCache, and we don't want to root unnecessary objects.
				_scope!.Dispose();
				_scope = null;

				// Don't commit or propagate options if the CacheEntry Value was never set.
				// We assume an exception occurred causing the caller to not set the Value successfully,
				// so don't use this entry.
				if (_valueHasBeenSet)
				{
					_notifyCacheEntryCommit(this);
					PropagateOptions(CacheEntryHelper<TKey>.Current);
				}
			}
		}

		internal bool CheckExpired(DateTimeOffset now)
		{
			return _isExpired || CheckForExpiredTime(now) || CheckForExpiredTokens();
		}

		internal void SetExpired(EvictionReason reason)
		{
			if (EvictionReason == EvictionReason.None)
			{
				EvictionReason = reason;
			}
			_isExpired = true;
			DetachTokens();
		}

		private bool CheckForExpiredTime(DateTimeOffset now)
		{
			if (_absoluteExpiration.HasValue && _absoluteExpiration.Value <= now)
			{
				SetExpired(EvictionReason.Expired);
				return true;
			}

			if (_slidingExpiration.HasValue
				&& (now - LastAccessed) >= _slidingExpiration)
			{
				SetExpired(EvictionReason.Expired);
				return true;
			}

			return false;
		}

		internal bool CheckForExpiredTokens()
		{
			if (_expirationTokens != null)
			{
				for (int i = 0; i < _expirationTokens.Count; i++)
				{
					var expiredToken = _expirationTokens[i];
					if (expiredToken.HasChanged)
					{
						SetExpired(EvictionReason.TokenExpired);
						return true;
					}
				}
			}
			return false;
		}

		internal void AttachTokens()
		{
			if (_expirationTokens != null)
			{
				lock (_lock)
				{
					for (int i = 0; i < _expirationTokens.Count; i++)
					{
						var expirationToken = _expirationTokens[i];
						if (expirationToken.ActiveChangeCallbacks)
						{
							if (_expirationTokenRegistrations == null)
							{
								_expirationTokenRegistrations = new List<IDisposable>(1);
							}
							var registration = expirationToken.RegisterChangeCallback(ExpirationCallback, this);
							_expirationTokenRegistrations.Add(registration);
						}
					}
				}
			}
		}

		private static void ExpirationTokensExpired(object obj)
		{
			// start a new thread to avoid issues with callbacks called from RegisterChangeCallback
			Task.Factory.StartNew(state =>
			{
				var entry = (CacheEntry<TKey>)state!;
				entry.SetExpired(EvictionReason.TokenExpired);
				entry._notifyCacheOfExpiration(entry);
			}, obj, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		private void DetachTokens()
		{
			lock (_lock)
			{
				var registrations = _expirationTokenRegistrations;
				if (registrations != null)
				{
					_expirationTokenRegistrations = null;
					for (int i = 0; i < registrations.Count; i++)
					{
						var registration = registrations[i];
						registration.Dispose();
					}
				}
			}
		}

		internal void InvokeEvictionCallbacks()
		{
			if (_postEvictionCallbacks != null)
			{
				Task.Factory.StartNew(state => InvokeCallbacks((CacheEntry<TKey>)state!), this,
					CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
			}
		}

		private static void InvokeCallbacks(CacheEntry<TKey> entry)
		{
			var callbackRegistrations = Interlocked.Exchange(ref entry._postEvictionCallbacks, null);

			if (callbackRegistrations == null)
			{
				return;
			}

			for (int i = 0; i < callbackRegistrations.Count; i++)
			{
				var registration = callbackRegistrations[i];

				try
				{
					registration.EvictionCallback?.Invoke(entry.Key, entry.Value, entry.EvictionReason, registration.State);
				}
				catch (Exception)
				{
					// This will be invoked on a background thread, don't let it throw.
					// TODO: LOG
				}
			}
		}

		internal void PropagateOptions(CacheEntry<TKey>? parent)
		{
			if (parent == null)
			{
				return;
			}

			// Copy expiration tokens and AbsoluteExpiration to the cache entries hierarchy.
			// We do this regardless of it gets cached because the tokens are associated with the value we'll return.
			if (_expirationTokens != null)
			{
				lock (_lock)
				{
					lock (parent._lock)
					{
						foreach (var expirationToken in _expirationTokens)
						{
							parent.AddExpirationToken(expirationToken);
						}
					}
				}
			}

			if (_absoluteExpiration.HasValue)
			{
				if (!parent._absoluteExpiration.HasValue || _absoluteExpiration < parent._absoluteExpiration)
				{
					parent._absoluteExpiration = _absoluteExpiration;
				}
			}
		}
	}
}
