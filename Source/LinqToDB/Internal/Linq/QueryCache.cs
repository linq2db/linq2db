using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;

using LinqToDB.Interceptors;
using LinqToDB.Internal.Interceptors;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Global LINQ query cache.
	/// Buckets queries by a shallow composite key, then verifies candidates inside each bucket
	/// via <see cref="Query.Compare"/>.
	/// </summary>
	/// <remarks>
	/// Lookup is O(1) at the bucket-map level. Inside each bucket, candidates are still verified
	/// with the original <see cref="Query.Compare"/> equality check.
	///
	/// Expiration:
	/// - Each entry stores its own base timeout, captured from the context that created it.
	/// - Each entry stores an explicit expiration deadline.
	/// - Cache hits extend the deadline using the current hotness tier.
	/// - Sweeps update hit-rate estimates, extend deadlines when an entry has earned a hotter tier,
	///   remove expired entries, and optionally trim the cache to a global capacity.
	/// - Sweeps never shorten an already-earned deadline.
	///
	/// Capacity:
	/// - Each bucket is capped by <see cref="BucketCap"/>.
	/// - Full buckets evict the entry with the earliest expiration deadline, then oldest access time,
	///   then lowest hit rate.
	/// - The whole cache is also approximately capped by <see cref="DefaultMaxEntries"/>, overridable
	///   through <see cref="MaxEntriesOverride"/>.
	///
	/// Threading:
	/// - <see cref="ConcurrentDictionary{TKey,TValue}"/> owns the bucket map.
	/// - Each bucket has its own lock for copy-on-write updates.
	/// - <see cref="Bucket.Entries"/> is published with volatile semantics.
	/// - The global sweep is single-flighted and runs on the thread pool.
	/// </remarks>
	internal sealed class QueryCache
	{
		/// <summary>Process-wide default cache used by <see cref="Query{T}"/>.</summary>
		public static readonly QueryCache Default = new();

		const int  BucketCap              = 16;
		const int  DefaultMaxEntries      = 10_000;
		const long DefaultSweepIntervalMs = 5L * 60 * 1000; // 5 minutes

		// Avoid promoting entries based on a tiny, noisy sample like "1 hit in 1 ms".
		const int PendingHitPromotionMinHits = 5;

		static readonly long MinimumHitRateWindowTicks = ToStopwatchTicks(TimeSpan.FromMinutes(1));

		readonly ConcurrentDictionary<CacheKey, Bucket>     _cache  = new();
		readonly ConcurrentDictionary<Type,     CounterBox> _misses = new();

		long _lastSweepTicks = Stopwatch.GetTimestamp();
		long _entryCount;

		// Incremented by ClearAll. Buckets created under older versions are ignored/removed.
		long _version;

		int _sweepRunning; // 0 = idle, 1 = queued/running

		/// <summary>
		/// Per-instance override for the base idle timeout.
		/// When <see langword="null"/>, each new entry captures
		/// <see cref="LinqOptions.CacheSlidingExpirationOrDefault"/> from the invoking
		/// <see cref="IDataContext"/>.
		/// </summary>
		public TimeSpan? IdleTimeoutOverride { get; set; }

		/// <summary>
		/// Per-instance override for the global-sweep interval.
		/// When <see langword="null"/>, the cache uses <see cref="DefaultSweepIntervalMs"/>.
		/// </summary>
		public TimeSpan? SweepIntervalOverride { get; set; }

		/// <summary>
		/// Per-instance override for the approximate global cache entry cap.
		/// When <see langword="null"/>, the cache uses <see cref="DefaultMaxEntries"/>.
		/// Set to <c>0</c> to prevent new entries from being cached.
		/// </summary>
		public int? MaxEntriesOverride { get; set; }

		[DebuggerDisplay("{ResultType.Name}/{ContextType.Name} cfg={ConfigurationID} flags={Flags} chain={ChainHash}")]
		readonly struct CacheKey : IEquatable<CacheKey>
		{
			public readonly Type       ResultType;
			public readonly Type       ContextType;
			public readonly int        ConfigurationID;
			public readonly QueryFlags Flags;
			public readonly bool       InlineParameters;
			public readonly bool       IsEntityServiceProvided;
			public readonly int        ChainHash;

			readonly int _hash;

			public CacheKey(
				Type       resultType,
				Type       contextType,
				int        configurationID,
				QueryFlags flags,
				bool       inlineParameters,
				bool       isEntityServiceProvided,
				int        chainHash)
			{
				ResultType              = resultType;
				ContextType             = contextType;
				ConfigurationID         = configurationID;
				Flags                   = flags;
				InlineParameters        = inlineParameters;
				IsEntityServiceProvided = isEntityServiceProvided;
				ChainHash               = chainHash;

				_hash = HashCode.Combine(
					resultType,
					contextType,
					configurationID,
					(int)flags,
					inlineParameters,
					isEntityServiceProvided,
					chainHash);
			}

			public bool Equals(CacheKey other)
			{
				return ConfigurationID         == other.ConfigurationID
					&& Flags                   == other.Flags
					&& InlineParameters        == other.InlineParameters
					&& IsEntityServiceProvided == other.IsEntityServiceProvided
					&& ChainHash               == other.ChainHash
					&& ResultType              == other.ResultType
					&& ContextType             == other.ContextType;
			}

			public override bool Equals(object? obj) => obj is CacheKey other && Equals(other);
			public override int  GetHashCode()       => _hash;
		}

		[DebuggerDisplay("Count = {Entries.Length}")]
		sealed class Bucket
		{
			public readonly Lock SyncRoot = new();
			public readonly long Version;

			// Replaced wholesale under SyncRoot. Readers use Volatile.Read.
			public Entry[] Entries = Array.Empty<Entry>();

			// Set after the bucket has been removed from the dictionary.
			public int Removed;

			public Bucket(long version)
			{
				Version = version;
			}
		}

		[DebuggerDisplay("HitsPerHour={HitsPerHour} HitsSinceSweep={HitsSinceSweep} Flags={QueryFlags}")]
		sealed class Entry
		{
			public Query      Query      = null!;
			public QueryFlags QueryFlags;

			public long LastAccessTicks;
			public long LastSweepTicks;

			public long BaseTimeoutTicks;
			public long ExpiresAtTicks;

			public long HitsSinceSweep;
			public int  HitsPerHour;
		}

		[DebuggerDisplay("Query={Query} Expressions={Expressions}")]
		internal sealed class FindResult
		{
			public Query             Query       { get; }
			public IQueryExpressions Expressions { get; }

			public FindResult(Query query, IQueryExpressions expressions)
			{
				Query       = query;
				Expressions = expressions;
			}
		}

		sealed class CounterBox
		{
			public long Value;
		}

		readonly struct TrimCandidate
		{
			public readonly CacheKey Key;
			public readonly Bucket   Bucket;
			public readonly Entry    Entry;
			public readonly long     ExpiresAtTicks;
			public readonly long     LastAccessTicks;
			public readonly int      HitsPerHour;

			public TrimCandidate(CacheKey key, Bucket bucket, Entry entry)
			{
				Key             = key;
				Bucket          = bucket;
				Entry           = entry;
				ExpiresAtTicks  = Interlocked.Read(ref entry.ExpiresAtTicks);
				LastAccessTicks = Interlocked.Read(ref entry.LastAccessTicks);
				HitsPerHour     = Volatile.Read (ref entry.HitsPerHour);
			}
		}

		/// <summary>
		/// Counts queries that missed the cache, per result type, to mirror the legacy
		/// <c>Query&lt;T&gt;.CacheMissCount</c> surface.
		/// </summary>
		public long GetMissCount(Type resultType)
			=> _misses.TryGetValue(resultType, out var box) ? Interlocked.Read(ref box.Value) : 0;

		public void IncrementMissCount(Type resultType)
		{
			var box = _misses.GetOrAdd(resultType, static _ => new CounterBox());
			Interlocked.Increment(ref box.Value);
		}

		/// <summary>Empties the cache for entries of the given result type.</summary>
		public void ClearForType(Type resultType)
		{
			foreach (var pair in _cache)
			{
				if (pair.Key.ResultType != resultType)
					continue;

				var bucket = pair.Value;

				lock (bucket.SyncRoot)
				{
					var current = Volatile.Read(ref bucket.Entries);
					RemoveBucketFromCache(pair.Key, bucket, current.Length);
				}
			}

			_misses.TryRemove(resultType, out _);
		}

		/// <summary>Empties the entire cache.</summary>
		public void ClearAll()
		{
			// Invalidate existing buckets.
			Interlocked.Increment(ref _version);

			foreach (var pair in _cache)
			{
				var bucket = pair.Value;

				lock (bucket.SyncRoot)
				{
					Volatile.Write(ref bucket.Removed, 1);
					Volatile.Write(ref bucket.Entries, Array.Empty<Entry>());
				}
			}

			_cache.Clear();
			_misses.Clear();
			Interlocked.Exchange(ref _entryCount, 0);

			// Invalidate any bucket that raced with the clear and was created during the window above.
			Interlocked.Increment(ref _version);
		}

		/// <summary>
		/// Looks up a cached query that matches the supplied <paramref name="expressions"/>
		/// under <paramref name="dataContext"/>. Returns <see langword="null"/> on miss.
		/// </summary>
		public FindResult? Find(
			Type              resultType,
			IDataContext      dataContext,
			IQueryExpressions expressions,
			QueryFlags        queryFlags)
		{
			var now = Stopwatch.GetTimestamp();

			MaybeSweepGlobal(now);

			if (ResolveMaxEntries() <= 0)
			{
				MaybeTrimGlobal();
				return null;
			}

			var key = BuildKey(resultType, dataContext, expressions, queryFlags);

			if (!_cache.TryGetValue(key, out var bucket))
				return null;

			if (bucket.Version != Volatile.Read(ref _version) || Volatile.Read(ref bucket.Removed) != 0)
				return null;

			var entries = Volatile.Read(ref bucket.Entries);

			for (var i = 0; i < entries.Length; i++)
			{
				var entry = entries[i];

				if (entry.QueryFlags != queryFlags)
					continue;

				// Read the current deadline; skip expired entries without paying for a CAS-loop
				// extension on the contended path. RecordAccess (sampled) handles deadline
				// extension for entries that survive Compare.
				if (IsExpired(entry, Stopwatch.GetTimestamp()))
					continue;

				if (entry.Query.Compare(dataContext, expressions, out var matched))
				{
					var hitNow = Stopwatch.GetTimestamp();

					if (IsExpired(entry, hitNow))
						continue;

					RecordAccess(entry, hitNow, countHit: true);

					return new FindResult(entry.Query, matched);
				}
			}

			return null;
		}

		/// <summary>
		/// Adds <paramref name="query"/> to the cache if no equivalent non-expired entry exists.
		/// Expired entries in the destination bucket are removed before adding.
		/// </summary>
		public void TryAdd(
			Type              resultType,
			IDataContext      dataContext,
			Query             query,
			IQueryExpressions expressions,
			QueryFlags        queryFlags)
		{
			var now = Stopwatch.GetTimestamp();

			MaybeSweepGlobal(now);

			var maxEntries = ResolveMaxEntries();

			if (maxEntries <= 0)
			{
				MaybeTrimGlobal();
				return;
			}

			var baseTimeoutTicks = ToStopwatchTicks(ResolveBaseTimeout(dataContext));

			if (baseTimeoutTicks <= 0)
				return;

			var key = BuildKey(resultType, dataContext, expressions, queryFlags);

			while (true)
			{
				var version = Volatile.Read(ref _version);
				var bucket  = _cache.GetOrAdd(key, _ => new Bucket(version));

				var added = false;

				lock (bucket.SyncRoot)
				{
					if (bucket.Version != Volatile.Read(ref _version) || Volatile.Read(ref bucket.Removed) != 0)
					{
						var stale = Volatile.Read(ref bucket.Entries);
						RemoveBucketFromCache(key, bucket, stale.Length);
						continue;
					}

					now = Stopwatch.GetTimestamp();

					var current   = Volatile.Read(ref bucket.Entries);
					var survivors = new List<Entry>(current.Length + 1);
					Entry? duplicate = null;

					for (var i = 0; i < current.Length; i++)
					{
						var existing = current[i];

						ExtendDeadlineFromLastAccess(existing, now, includePendingHits: true);

						if (IsExpired(existing, now))
							continue;

						if (existing.QueryFlags == queryFlags
							&& existing.Query.Compare(dataContext, expressions, out _))
						{
							duplicate = existing;
						}

						survivors.Add(existing);
					}

					if (duplicate != null)
					{
						if (survivors.Count != current.Length)
						{
							Volatile.Write(ref bucket.Entries, survivors.ToArray());
							AdjustEntryCount(survivors.Count - current.Length);
						}

						// Another thread already added an equivalent query. Refresh its idle deadline,
						// but do not count this as a cache hit.
						RecordAccess(duplicate, now, countHit: false);

						return;
					}

					var removedEntries = current.Length - survivors.Count;

					while (survivors.Count >= BucketCap)
					{
						var victimIndex = FindBucketVictimIndex(survivors);
						survivors.RemoveAt(victimIndex);
						removedEntries++;
					}

					survivors.Add(new Entry
					{
						Query            = query,
						QueryFlags       = queryFlags,
						LastAccessTicks  = now,
						LastSweepTicks   = now,
						BaseTimeoutTicks = baseTimeoutTicks,
						ExpiresAtTicks   = SaturatingAdd(now, baseTimeoutTicks),
						HitsSinceSweep   = 0,
						HitsPerHour      = 0,
					});

					Volatile.Write(ref bucket.Entries, survivors.ToArray());
					AdjustEntryCount(1 - removedEntries);

					added = true;
				}

				if (added)
					MaybeTrimGlobal();

				return;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static CacheKey BuildKey(
			Type              resultType,
			IDataContext      dataContext,
			IQueryExpressions expressions,
			QueryFlags        queryFlags)
		{
			var chainHash = ComputeChainHash(expressions.MainExpression);

			return new CacheKey(
				resultType,
				dataContext.GetType(),
				dataContext.ConfigurationID,
				queryFlags,
				dataContext.InlineParameters,
				dataContext is IInterceptable<IEntityServiceInterceptor> { Interceptor: { } },
				chainHash);
		}

		/// <summary>
		/// Walks the LINQ source chain from <paramref name="main"/> for up to 8 levels and
		/// hashes each method/member identity.
		/// </summary>
		static int ComputeChainHash(Expression main)
		{
			var hash    = new HashCode();
			var current = (Expression?)main;

			for (var depth = 0; current != null && depth < 8; depth++)
			{
				switch (current)
				{
					case MethodCallExpression mc:
						hash.Add(mc.Method);
						current = mc.Arguments.Count > 0 ? mc.Arguments[0] : null;
						break;

					case MemberExpression me:
						hash.Add(me.Member);
						current = me.Expression;
						break;

					default:
						hash.Add(current.NodeType);
						hash.Add(current.Type);
						current = null;
						break;
				}
			}

			return hash.ToHashCode();
		}

		TimeSpan ResolveBaseTimeout(IDataContext dataContext)
			=> IdleTimeoutOverride ?? dataContext.Options.LinqOptions.CacheSlidingExpirationOrDefault;

		long ResolveSweepIntervalStopwatchTicks()
		{
			var interval = SweepIntervalOverride ?? TimeSpan.FromMilliseconds(DefaultSweepIntervalMs);
			return ToStopwatchTicks(interval);
		}

		int ResolveMaxEntries()
		{
			var value = MaxEntriesOverride ?? DefaultMaxEntries;
			return value < 0 ? 0 : value;
		}

		void MaybeSweepGlobal(long now)
		{
			var interval = ResolveSweepIntervalStopwatchTicks();
			var last     = Interlocked.Read(ref _lastSweepTicks);

			if (interval > 0 && now - last < interval)
				return;

			QueueGlobalMaintenance(now);
		}

		void MaybeTrimGlobal()
		{
			var maxEntries = ResolveMaxEntries();

			if (Interlocked.Read(ref _entryCount) <= maxEntries)
				return;

			QueueGlobalMaintenance(Stopwatch.GetTimestamp());
		}

		void QueueGlobalMaintenance(long now)
		{
			if (Interlocked.CompareExchange(ref _sweepRunning, 1, 0) != 0)
				return;

			Interlocked.Exchange(ref _lastSweepTicks, now);

			var queued = ThreadPool.UnsafeQueueUserWorkItem(static state =>
			{
				var cache = (QueryCache)state!;

				try
				{
					cache.SweepGlobal();
				}
				finally
				{
					Interlocked.Exchange(ref cache._sweepRunning, 0);
				}
			}, this);

			if (!queued)
				Interlocked.Exchange(ref _sweepRunning, 0);
		}

		void SweepGlobal()
		{
			var version = Volatile.Read(ref _version);

			foreach (var pair in _cache)
			{
				var bucket = pair.Value;

				lock (bucket.SyncRoot)
				{
					var current = Volatile.Read(ref bucket.Entries);

					if (bucket.Version != version || Volatile.Read(ref bucket.Removed) != 0)
					{
						RemoveBucketFromCache(pair.Key, bucket, current.Length);
						continue;
					}

					if (current.Length == 0)
					{
						RemoveBucketFromCache(pair.Key, bucket, 0);
						continue;
					}

					var now       = Stopwatch.GetTimestamp();
					var survivors = new List<Entry>(current.Length);

					for (var i = 0; i < current.Length; i++)
					{
						var entry = current[i];

						UpdateHitRate(entry, now);
						ExtendDeadlineFromLastAccess(entry, now, includePendingHits: false);

						if (IsExpired(entry, now))
							continue;

						survivors.Add(entry);
					}

					if (survivors.Count == current.Length)
						continue;

					if (survivors.Count == 0)
					{
						RemoveBucketFromCache(pair.Key, bucket, current.Length);
					}
					else
					{
						Volatile.Write(ref bucket.Entries, survivors.ToArray());
						AdjustEntryCount(survivors.Count - current.Length);
					}
				}
			}

			TrimGlobalToCapacity(ResolveMaxEntries());
		}

		void TrimGlobalToCapacity(int maxEntries)
		{
			if (maxEntries < 0)
				maxEntries = 0;

			var overage = Interlocked.Read(ref _entryCount) - maxEntries;

			if (overage <= 0)
				return;

			var version    = Volatile.Read(ref _version);
			var candidates = new List<TrimCandidate>();

			foreach (var pair in _cache)
			{
				var bucket = pair.Value;

				if (bucket.Version != version || Volatile.Read(ref bucket.Removed) != 0)
					continue;

				var entries = Volatile.Read(ref bucket.Entries);

				for (var i = 0; i < entries.Length; i++)
					candidates.Add(new TrimCandidate(pair.Key, bucket, entries[i]));
			}

			if (candidates.Count == 0)
				return;

			candidates.Sort(static (left, right) =>
			{
				var byExpiry = left.ExpiresAtTicks.CompareTo(right.ExpiresAtTicks);
				if (byExpiry != 0)
					return byExpiry;

				var byAccess = left.LastAccessTicks.CompareTo(right.LastAccessTicks);
				if (byAccess != 0)
					return byAccess;

				return left.HitsPerHour.CompareTo(right.HitsPerHour);
			});

			var target = overage > candidates.Count ? candidates.Count : (int)overage;
			var removed = 0;

			for (var i = 0; i < candidates.Count && removed < target; i++)
			{
				if (RemoveEntryFromBucket(candidates[i]))
					removed++;
			}
		}

		bool RemoveEntryFromBucket(TrimCandidate candidate)
		{
			var bucket = candidate.Bucket;

			lock (bucket.SyncRoot)
			{
				if (bucket.Version != Volatile.Read(ref _version) || Volatile.Read(ref bucket.Removed) != 0)
					return false;

				var current = Volatile.Read(ref bucket.Entries);
				var index   = Array.IndexOf(current, candidate.Entry);

				if (index < 0)
					return false;

				if (current.Length == 1)
					return RemoveBucketFromCache(candidate.Key, bucket, 1);

				var next = new Entry[current.Length - 1];

				if (index > 0)
					Array.Copy(current, 0, next, 0, index);

				if (index < current.Length - 1)
					Array.Copy(current, index + 1, next, index, current.Length - index - 1);

				Volatile.Write(ref bucket.Entries, next);
				AdjustEntryCount(-1);

				return true;
			}
		}

		bool RemoveBucketFromCache(CacheKey key, Bucket bucket, int removedEntryCount)
		{
			if (!TryRemovePair(key, bucket))
				return false;

			Volatile.Write(ref bucket.Removed, 1);
			Volatile.Write(ref bucket.Entries, Array.Empty<Entry>());
			AdjustEntryCount(-removedEntryCount);

			return true;
		}

		bool TryRemovePair(CacheKey key, Bucket bucket)
		{
			return ((ICollection<KeyValuePair<CacheKey, Bucket>>)_cache)
				.Remove(new KeyValuePair<CacheKey, Bucket>(key, bucket));
		}

		void AdjustEntryCount(long delta)
		{
			if (delta == 0)
				return;

			if (delta > 0)
			{
				Interlocked.Add(ref _entryCount, delta);
				return;
			}

			while (true)
			{
				var current = Interlocked.Read(ref _entryCount);
				var next    = current + delta;

				if (next < 0)
					next = 0;

				if (Interlocked.CompareExchange(ref _entryCount, next, current) == current)
					return;
			}
		}

		// Hot-path hits sample the heavyweight deadline-extension work to avoid hammering
		// LastAccessTicks / ExpiresAtTicks (and their cache lines) on every single hit.
		// 1 / 16 hits triggers the full update; the other 15 only bump HitsSinceSweep.
		// LastAccessTicks accuracy degrades to ~16 hits, which is irrelevant at the 1-hour
		// idle-timeout granularity. ExpiresAtTicks is monotonically extended, so missing
		// updates only delay extension — never shorten an earned deadline.
		const long HitSampleMask = 0xF;

		static void RecordAccess(Entry entry, long now, bool countHit)
		{
			if (countHit)
			{
				// Always increment — the rate metric needs every hit counted.
				var hits = Interlocked.Increment(ref entry.HitsSinceSweep);

				// Skip the heavyweight write path on most hits. First hit (hits == 1)
				// always triggers so a brand-new entry sees an updated deadline immediately.
				if ((hits & HitSampleMask) != 1)
					return;
			}

			Interlocked.Exchange(ref entry.LastAccessTicks, now);

			var hitsPerHour      = EffectiveHitsPerHourForDeadline(entry, now, includePendingHits: true);
			var baseTimeoutTicks = Interlocked.Read(ref entry.BaseTimeoutTicks);
			var timeoutTicks     = EffectiveTimeoutTicks(hitsPerHour, baseTimeoutTicks);

			ExtendExpiresAt(entry, SaturatingAdd(now, timeoutTicks));
		}

		static void ExtendDeadlineFromLastAccess(Entry entry, long now, bool includePendingHits)
		{
			var lastAccessTicks  = Interlocked.Read(ref entry.LastAccessTicks);
			var baseTimeoutTicks = Interlocked.Read(ref entry.BaseTimeoutTicks);
			var hitsPerHour      = EffectiveHitsPerHourForDeadline(entry, now, includePendingHits);
			var timeoutTicks     = EffectiveTimeoutTicks(hitsPerHour, baseTimeoutTicks);

			ExtendExpiresAt(entry, SaturatingAdd(lastAccessTicks, timeoutTicks));
		}

		static int EffectiveHitsPerHourForDeadline(Entry entry, long now, bool includePendingHits)
		{
			var hitsPerHour = Volatile.Read(ref entry.HitsPerHour);

			if (!includePendingHits)
				return hitsPerHour;

			var pendingHits = Interlocked.Read(ref entry.HitsSinceSweep);

			if (pendingHits < PendingHitPromotionMinHits)
				return hitsPerHour;

			var lastSweepTicks = Interlocked.Read(ref entry.LastSweepTicks);
			var elapsedTicks   = now - lastSweepTicks;

			if (elapsedTicks < MinimumHitRateWindowTicks)
				return hitsPerHour;

			var pendingRate = CalculateHitsPerHour(pendingHits, elapsedTicks);

			return pendingRate > hitsPerHour ? pendingRate : hitsPerHour;
		}

		static void ExtendExpiresAt(Entry entry, long candidateExpiresAtTicks)
		{
			while (true)
			{
				var current = Interlocked.Read(ref entry.ExpiresAtTicks);

				if (candidateExpiresAtTicks <= current)
					return;

				if (Interlocked.CompareExchange(ref entry.ExpiresAtTicks, candidateExpiresAtTicks, current) == current)
					return;
			}
		}

		static bool IsExpired(Entry entry, long now)
			=> now > Interlocked.Read(ref entry.ExpiresAtTicks);

		static void UpdateHitRate(Entry entry, long now)
		{
			var lastSweepTicks = Interlocked.Read(ref entry.LastSweepTicks);
			var elapsedTicks   = now - lastSweepTicks;

			if (elapsedTicks <= 0)
				return;

			var observedHits = Interlocked.Read(ref entry.HitsSinceSweep);

			// Do not let frequent capacity trims cause artificial decay/promotions on tiny windows.
			if (elapsedTicks < MinimumHitRateWindowTicks && observedHits < PendingHitPromotionMinHits)
				return;

			var hits = Interlocked.Exchange(ref entry.HitsSinceSweep, 0);

			var rateWindowTicks = elapsedTicks < MinimumHitRateWindowTicks
				? MinimumHitRateWindowTicks
				: elapsedTicks;

			var instantRate = CalculateHitsPerHour(hits, rateWindowTicks);
			var previous    = Volatile.Read(ref entry.HitsPerHour);

			var blended = previous == 0
				? instantRate
				: (int)(((long)previous + instantRate) / 2);

			Volatile.Write(ref entry.HitsPerHour, blended);
			Interlocked.Exchange(ref entry.LastSweepTicks, now);
		}

		static int CalculateHitsPerHour(long hits, long elapsedTicks)
		{
			if (hits <= 0)
				return 0;

			if (elapsedTicks <= 0)
				return hits >= int.MaxValue ? int.MaxValue : (int)hits;

			var rate = hits * 3600d * Stopwatch.Frequency / elapsedTicks;

			if (rate >= int.MaxValue)
				return int.MaxValue;

			if (rate <= 0)
				return 0;

			return (int)rate;
		}

		/// <summary>
		/// Effective idle timeout in stopwatch ticks.
		/// Tiers:
		/// &lt; 5/hr   => 1x base
		/// &lt; 50/hr  => 4x base
		/// &lt; 500/hr => 12x base
		/// >= 500/hr => 24x base
		/// </summary>
		static long EffectiveTimeoutTicks(int hitsPerHour, long baseTimeoutTicks)
		{
			if (baseTimeoutTicks <= 0)
				return 0;

			var multiplier = hitsPerHour switch
			{
				<   5 =>  1,
				<  50 =>  4,
				< 500 => 12,
				_     => 24,
			};

			if (baseTimeoutTicks > long.MaxValue / multiplier)
				return long.MaxValue;

			return baseTimeoutTicks * multiplier;
		}

		static long ToStopwatchTicks(TimeSpan value)
		{
			if (value <= TimeSpan.Zero)
				return 0;

			var ticks = value.TotalSeconds * Stopwatch.Frequency;

			if (ticks >= long.MaxValue)
				return long.MaxValue;

			if (ticks <= 0)
				return 0;

			return (long)Math.Ceiling(ticks);
		}

		static long SaturatingAdd(long left, long right)
		{
			if (right <= 0)
				return left;

			if (left > long.MaxValue - right)
				return long.MaxValue;

			return left + right;
		}

		static int FindBucketVictimIndex(List<Entry> entries)
		{
			var victimIndex = 0;

			for (var i = 1; i < entries.Count; i++)
			{
				if (CompareForEviction(entries[i], entries[victimIndex]) < 0)
					victimIndex = i;
			}

			return victimIndex;
		}

		static int CompareForEviction(Entry left, Entry right)
		{
			var leftExpires  = Interlocked.Read(ref left.ExpiresAtTicks);
			var rightExpires = Interlocked.Read(ref right.ExpiresAtTicks);

			var byExpiry = leftExpires.CompareTo(rightExpires);

			if (byExpiry != 0)
				return byExpiry;

			var leftAccess  = Interlocked.Read(ref left.LastAccessTicks);
			var rightAccess = Interlocked.Read(ref right.LastAccessTicks);

			var byAccess = leftAccess.CompareTo(rightAccess);

			if (byAccess != 0)
				return byAccess;

			var leftRate  = Volatile.Read(ref left.HitsPerHour);
			var rightRate = Volatile.Read(ref right.HitsPerHour);

			return leftRate.CompareTo(rightRate);
		}
	}
}
