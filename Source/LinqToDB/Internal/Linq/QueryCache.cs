using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;

using LinqToDB.Interceptors;
using LinqToDB.Internal.Interceptors;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Global LINQ query cache. Buckets queries by a shallow composite key
	/// (<see cref="Type"/> of T, context type, configuration id, query flags, source-chain hash),
	/// then verifies candidates inside each bucket via <see cref="Query.Compare"/>.
	/// </summary>
	/// <remarks>
	/// Replaces the per-<c>Query&lt;T&gt;</c> single-bucket array. Lookup is O(1) at the bucket level;
	/// inside each bucket (typically 1–3 entries) the original <see cref="Query.Compare"/>
	/// equality check (driven by <c>EqualsToVisitor</c>) runs unchanged.
	///
	/// Eviction:
	/// - Idle entries are dropped on the next <see cref="TryAdd"/> into the same bucket.
	/// - A periodic global sweep (every <see cref="DefaultSweepIntervalMs"/>, triggered lazily on
	///   any Find/TryAdd) walks all buckets, drops expired entries, and removes empty buckets so
	///   the dictionary doesn't accumulate stale slots forever.
	/// - Effective idle timeout scales with hit count (cold queries evict in 1× base, hot queries get up to 24×)
	///   so business-hours-hot queries survive overnight idle.
	/// - Per-bucket cap (<see cref="BucketCap"/>) drops the oldest entry once full.
	///
	/// Threading:
	/// - <see cref="ConcurrentDictionary{TKey,TValue}"/> provides lock-free reads of the bucket map.
	/// - Each <see cref="Bucket"/> has its own lock for copy-on-write updates.
	/// - <see cref="Bucket.Entries"/> is replaced atomically; readers snapshot the reference and
	///   never observe a torn state.
	/// - The global sweep is single-flighted via a CAS on <see cref="_sweepRunning"/>; the work
	///   itself runs on a thread-pool worker so callers don't pay the O(buckets) cost.
	/// </remarks>
	internal sealed class QueryCache
	{
		/// <summary>Process-wide default cache used by <see cref="Query{T}"/>.</summary>
		public static readonly QueryCache Default = new();

		const int BucketCap = 16;

		/// <summary>Default interval between global sweeps that walk every bucket.</summary>
		const long DefaultSweepIntervalMs = 5L * 60 * 1000; // 5 minutes

		/// <summary>
		/// Conversion factor: <see cref="Stopwatch.Frequency"/> ticks per millisecond.
		/// Used everywhere we need a monotonic clock — <c>Environment.TickCount64</c>
		/// isn't available on net462 / netstandard2.0.
		/// </summary>
		static readonly long StopwatchTicksPerMs = Stopwatch.Frequency / 1000;

		readonly ConcurrentDictionary<CacheKey, Bucket>      _cache  = new();
		readonly ConcurrentDictionary<Type,     CounterBox>  _misses = new();

		long _lastSweepTicks = Stopwatch.GetTimestamp();
		int  _sweepRunning;          // 0 = idle, 1 = a background sweep is queued or running

		/// <summary>
		/// Per-instance override for the base idle timeout. When <see langword="null"/>,
		/// each call reads <see cref="LinqOptions.CacheSlidingExpirationOrDefault"/> from the
		/// invoking <see cref="IDataContext"/>. Setting it to a non-null value forces every
		/// sweep on this instance to use the override (useful for tests).
		/// </summary>
		public TimeSpan? IdleTimeoutOverride { get; set; }

		/// <summary>
		/// Per-instance override for the global-sweep interval. When <see langword="null"/>,
		/// the cache uses <see cref="DefaultSweepIntervalMs"/>. Useful for tests that want
		/// to trigger sweeps without waiting minutes.
		/// </summary>
		public TimeSpan? SweepIntervalOverride { get; set; }

		[DebuggerDisplay("{ResultType.Name}/{ContextType.Name} cfg={ConfigurationID} flags={Flags} chain={ChainHash}")]
		readonly struct CacheKey : IEquatable<CacheKey>
		{
			public readonly Type        ResultType;
			public readonly Type        ContextType;
			public readonly int         ConfigurationID;
			public readonly QueryFlags  Flags;
			public readonly bool        InlineParameters;
			public readonly bool        IsEntityServiceProvided;

			/// <summary>
			/// Hash derived from up to 8 levels of the LINQ source chain
			/// (<see cref="MethodCallExpression.Arguments"/>[0] for calls,
			///  <see cref="MemberExpression.Expression"/> for member access).
			/// Differentiates queries that share a top-level method but operate on different
			/// sources (e.g. <c>db.Users.Count()</c> vs <c>db.Posts.Count()</c>) so they go
			/// to separate buckets.
			/// </summary>
			public readonly int         ChainHash;

			readonly int _hash;

			public CacheKey(
				Type        resultType,
				Type        contextType,
				int         configurationID,
				QueryFlags  flags,
				bool        inlineParameters,
				bool        isEntityServiceProvided,
				int         chainHash)
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

			// Replaced wholesale (copy-on-write) under SyncRoot. Readers snapshot the field.
			public Entry[] Entries = [];
		}

		[DebuggerDisplay("HitsPerHour={HitsPerHour} HitsSinceSweep={HitsSinceSweep} Flags={QueryFlags}")]
		sealed class Entry
		{
			public Query      Query           = null!;
			public QueryFlags QueryFlags;
			public long       LastAccessTicks;     // updated on Find hit
			public long       LastSweepTicks;      // updated on each sweep that visits this entry
			public int        HitsSinceSweep;      // incremented atomically on Find hit; reset on sweep
			public int        HitsPerHour;         // smoothed rate from the last sweep, used for tier
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

		/// <summary>
		/// Counts queries that missed the cache (per result type), to mirror the legacy
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
				if (pair.Key.ResultType == resultType)
					_cache.TryRemove(pair.Key, out _);

			_misses.TryRemove(resultType, out _);
		}

		/// <summary>Empties the entire cache.</summary>
		public void ClearAll()
		{
			_cache .Clear();
			_misses.Clear();
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
			MaybeSweepGlobal(dataContext);

			var key = BuildKey(resultType, dataContext, expressions, queryFlags);

			if (!_cache.TryGetValue(key, out var bucket))
				return null;

			// Snapshot the entries array — readers must never see a torn state.
			var entries = bucket.Entries;

			for (var i = 0; i < entries.Length; i++)
			{
				var entry = entries[i];

				if (entry.QueryFlags != queryFlags)
					continue;

				if (entry.Query.Compare(dataContext, expressions, out var matched))
				{
					Interlocked.Exchange  (ref entry.LastAccessTicks, Stopwatch.GetTimestamp());
					Interlocked.Increment (ref entry.HitsSinceSweep);
					return new FindResult(entry.Query, matched);
				}
			}

			return null;
		}

		/// <summary>
		/// Adds <paramref name="query"/> to the cache if no equivalent entry exists.
		/// Sweeps the destination bucket of idle entries before adding; if the bucket
		/// is full, the oldest entry is evicted.
		/// </summary>
		public void TryAdd(
			Type              resultType,
			IDataContext      dataContext,
			Query             query,
			IQueryExpressions expressions,
			QueryFlags        queryFlags)
		{
			MaybeSweepGlobal(dataContext);

			var key    = BuildKey(resultType, dataContext, expressions, queryFlags);
			var bucket = _cache.GetOrAdd(key, static _ => new Bucket());

			lock (bucket.SyncRoot)
			{
				var current        = bucket.Entries;
				var now            = Stopwatch.GetTimestamp();
				var baseTimeout    = ResolveBaseTimeout(dataContext);
				var survivors      = new List<Entry>(current.Length + 1);

				for (var i = 0; i < current.Length; i++)
				{
					var existing = current[i];

					// Already present — another thread won the race.
					if (existing.QueryFlags == queryFlags
						&& existing.Query.Compare(dataContext, expressions, out _))
					{
						return;
					}

					if (!IsExpired(existing, now, baseTimeout))
						survivors.Add(existing);
				}

				if (survivors.Count >= BucketCap)
					survivors.RemoveAt(0); // drop oldest

				survivors.Add(new Entry
				{
					Query           = query,
					QueryFlags      = queryFlags,
					LastAccessTicks = now,
					LastSweepTicks  = now,
					HitsSinceSweep  = 0,
					HitsPerHour     = 0,
				});

				bucket.Entries = survivors.ToArray();
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
		/// hashes each method/member identity. The chain follows
		/// <see cref="MethodCallExpression.Arguments"/>[0] for calls and
		/// <see cref="MemberExpression.Expression"/> for member access; other node kinds
		/// terminate the walk and contribute their <see cref="ExpressionType"/> + <see cref="Type"/>.
		/// </summary>
		/// <remarks>
		/// Hash collisions are correctness-preserving: two structurally distinct queries
		/// that hash to the same bucket are still distinguished by <see cref="Query.Compare"/>
		/// inside the bucket — same fallback as Phase 1.
		/// </remarks>
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

		long ResolveSweepIntervalMs()
			=> SweepIntervalOverride is { } o ? (long)o.TotalMilliseconds : DefaultSweepIntervalMs;

		long ResolveSweepIntervalStopwatchTicks() => ResolveSweepIntervalMs() * StopwatchTicksPerMs;

		static bool IsExpired(Entry entry, long now, TimeSpan baseTimeout)
		{
			var elapsed     = now - Interlocked.Read(ref entry.LastAccessTicks);
			var thresholdMs = EffectiveTimeoutMs(entry.HitsPerHour, baseTimeout);
			return elapsed > thresholdMs * StopwatchTicksPerMs;
		}

		/// <summary>
		/// Triggers a global sweep at most once every <see cref="DefaultSweepIntervalMs"/>
		/// (or the per-instance <see cref="SweepIntervalOverride"/>). The work is offloaded
		/// to the thread pool so the calling <c>Find</c>/<c>TryAdd</c> does not pay the
		/// O(buckets) cost. Hot-path overhead is two atomic reads + one CAS, plus a single
		/// <c>ThreadPool.UnsafeQueueUserWorkItem</c> call when due.
		/// </summary>
		void MaybeSweepGlobal(IDataContext dataContext)
		{
			var now      = Stopwatch.GetTimestamp();
			var last     = Interlocked.Read(ref _lastSweepTicks);
			var interval = ResolveSweepIntervalStopwatchTicks();

			if (now - last < interval)
				return;

			// Skip if a sweep is already queued / in progress.
			if (Interlocked.CompareExchange(ref _sweepRunning, 1, 0) != 0)
				return;

			// Resolve timeout from the *triggering* context now — the data context may be
			// disposed by the time the queued work runs.
			var baseTimeout = ResolveBaseTimeout(dataContext);

			Interlocked.Exchange(ref _lastSweepTicks, now);

			// WaitCallback overload is available on every target framework (including net462 /
			// netstandard2.0). Boxing the tuple state allocates once per sweep — acceptable
			// at one-per-5-minutes cadence.
			ThreadPool.UnsafeQueueUserWorkItem(static state =>
			{
				var (cache, baseTimeout, now) = ((QueryCache, TimeSpan, long))state!;
				try
				{
					cache.SweepGlobal(baseTimeout, now);
				}
				finally
				{
					Interlocked.Exchange(ref cache._sweepRunning, 0);
				}
			}, (this, baseTimeout, now));
		}

		/// <summary>
		/// Walks every bucket, drops expired entries, and removes empty buckets atomically.
		/// Runs on a thread-pool worker; safe to overlap with concurrent <c>Find</c>/<c>TryAdd</c>
		/// because each bucket is mutated under its own <see cref="Bucket.SyncRoot"/>.
		/// </summary>
		void SweepGlobal(TimeSpan baseTimeout, long now)
		{
			foreach (var pair in _cache)
			{
				var bucket = pair.Value;

				lock (bucket.SyncRoot)
				{
					var current   = bucket.Entries;
					var survivors = new List<Entry>(current.Length);

					for (var i = 0; i < current.Length; i++)
					{
						var entry = current[i];

						if (IsExpired(entry, now, baseTimeout))
							continue;

						UpdateHitRate(entry, now);

						survivors.Add(entry);
					}

					if (survivors.Count == 0)
					{
						// Remove the empty bucket. Concurrent TryAdds that already obtained
						// this same bucket reference will lose their work (their entry stays
						// in an orphaned bucket). Acceptable: the loss is bounded to one
						// entry per rare race, and the next request rebuilds it.
						// (TryRemove(KeyValuePair) overload isn't available on net462 /
						// netstandard2.0, hence the simpler key-only form.)
						_cache.TryRemove(pair.Key, out _);
					}
					else if (survivors.Count != current.Length)
					{
						bucket.Entries = survivors.ToArray();
					}
				}
			}
		}

		/// <summary>
		/// Recomputes <see cref="Entry.HitsPerHour"/> from <see cref="Entry.HitsSinceSweep"/>
		/// (atomically reset to 0) and the wall-clock interval since this entry was last
		/// visited by a sweep. Result is blended 50/50 with the previous rate so a single
		/// quiet interval doesn't swing an entry across multiple tiers.
		/// </summary>
		/// <remarks>
		/// Decoupled from sweep cadence: tier reflects hits-per-hour regardless of how
		/// often sweeps actually run. A hot 1000/hr query stays at 1000/hr until calls
		/// stop; with no calls, the rate decays 1000 → 500 → 250 → ... per sweep.
		/// </remarks>
		static void UpdateHitRate(Entry entry, long now)
		{
			var elapsedTicks = now - entry.LastSweepTicks;
			var elapsedMs    = elapsedTicks / StopwatchTicksPerMs;
			var hits         = Interlocked.Exchange(ref entry.HitsSinceSweep, 0);

			// hits per hour = hits * (ms in 1 hour) / elapsedMs.
			// long math fits: hits ≤ int.MaxValue (~2e9), 3.6M as long, product ≤ ~7.2e15.
			var instantRate = elapsedMs > 0
				? (int)((long)hits * 3_600_000 / elapsedMs)
				: hits;

			// EMA blend (equal weight). Brand-new entry has HitsPerHour = 0 → first sweep
			// sets rate to half of observed; converges within 2-3 sweeps.
			entry.HitsPerHour    = (entry.HitsPerHour + instantRate) / 2;
			entry.LastSweepTicks = now;
		}

		/// <summary>
		/// Effective idle timeout in milliseconds. Tiered on observed hits-per-hour rate:
		/// &lt; 5/hr → 1× base, &lt; 50/hr → 4×, &lt; 500/hr → 12×, ≥ 500/hr → 24× (cap).
		/// </summary>
		static long EffectiveTimeoutMs(int hitsPerHour, TimeSpan baseTimeout)
		{
			var baseMs = (long)baseTimeout.TotalMilliseconds;

			return hitsPerHour switch
			{
				<   5 => baseMs,
				<  50 => baseMs *  4,
				< 500 => baseMs * 12,
				_     => baseMs * 24,
			};
		}
	}
}
