using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

using LinqToDB.Interceptors;
using LinqToDB.Internal.Interceptors;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Global LINQ query cache. Buckets queries by a shallow composite key
	/// (<see cref="Type"/> of T, context type, configuration id, query flags, root member),
	/// then verifies candidates inside each bucket via <see cref="Query.Compare"/>.
	/// </summary>
	/// <remarks>
	/// Replaces the per-<c>Query&lt;T&gt;</c> single-bucket array. Lookup is O(1) at the bucket level;
	/// inside each bucket (typically 1–3 entries) the original <see cref="Query.Compare"/>
	/// equality check (driven by <c>EqualsToVisitor</c>) runs unchanged.
	///
	/// Eviction:
	/// - Idle entries are dropped on the next <see cref="TryAdd"/> into the same bucket.
	/// - Effective idle timeout scales with hit count (cold queries evict in 1× base, hot queries get up to 24×)
	///   so business-hours-hot queries survive overnight idle.
	/// - Per-bucket cap (<see cref="BucketCap"/>) drops the oldest entry once full.
	///
	/// Threading:
	/// - <see cref="ConcurrentDictionary{TKey,TValue}"/> provides lock-free reads of the bucket map.
	/// - Each <see cref="Bucket"/> has its own lock for copy-on-write updates.
	/// - <see cref="Bucket.Entries"/> is replaced atomically; readers snapshot the reference and
	///   never observe a torn state.
	/// </remarks>
	internal sealed class QueryCache
	{
		/// <summary>Process-wide default cache used by <see cref="Query{T}"/>.</summary>
		public static readonly QueryCache Default = new();

		const int BucketCap = 16;

		readonly ConcurrentDictionary<CacheKey, Bucket>      _cache  = new();
		readonly ConcurrentDictionary<Type,     CounterBox>  _misses = new();

		/// <summary>
		/// Per-instance override for the base idle timeout. When <see langword="null"/>,
		/// each call reads <see cref="LinqOptions.CacheSlidingExpirationOrDefault"/> from the
		/// invoking <see cref="IDataContext"/>. Setting it to a non-null value forces every
		/// sweep on this instance to use the override (useful for tests).
		/// </summary>
		public TimeSpan? IdleTimeoutOverride { get; set; }

		[DebuggerDisplay("{ResultType.Name}/{ContextType.Name} cfg={ConfigurationID} flags={Flags} root={RootMember == null ? \"<none>\" : RootMember.Name}")]
		readonly struct CacheKey : IEquatable<CacheKey>
		{
			public readonly Type        ResultType;
			public readonly Type        ContextType;
			public readonly int         ConfigurationID;
			public readonly QueryFlags  Flags;
			public readonly bool        InlineParameters;
			public readonly bool        IsEntityServiceProvided;
			public readonly MemberInfo? RootMember;

			readonly int _hash;

			public CacheKey(
				Type        resultType,
				Type        contextType,
				int         configurationID,
				QueryFlags  flags,
				bool        inlineParameters,
				bool        isEntityServiceProvided,
				MemberInfo? rootMember)
			{
				ResultType              = resultType;
				ContextType             = contextType;
				ConfigurationID         = configurationID;
				Flags                   = flags;
				InlineParameters        = inlineParameters;
				IsEntityServiceProvided = isEntityServiceProvided;
				RootMember              = rootMember;

				_hash = HashCode.Combine(
					resultType,
					contextType,
					configurationID,
					(int)flags,
					inlineParameters,
					isEntityServiceProvided,
					rootMember);
			}

			public bool Equals(CacheKey other)
			{
				return ConfigurationID         == other.ConfigurationID
					&& Flags                   == other.Flags
					&& InlineParameters        == other.InlineParameters
					&& IsEntityServiceProvided == other.IsEntityServiceProvided
					&& ResultType              == other.ResultType
					&& ContextType             == other.ContextType
					&& RootMember              == other.RootMember;
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

		[DebuggerDisplay("Hits={HitCount} LastAccessTicks={LastAccessTicks} Flags={QueryFlags}")]
		sealed class Entry
		{
			public Query      Query           = null!;
			public QueryFlags QueryFlags;
			public long       LastAccessTicks;
			public int        HitCount;
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
					Interlocked.Exchange  (ref entry.LastAccessTicks, Environment.TickCount64);
					Interlocked.Increment (ref entry.HitCount);
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
			var key    = BuildKey(resultType, dataContext, expressions, queryFlags);
			var bucket = _cache.GetOrAdd(key, static _ => new Bucket());

			lock (bucket.SyncRoot)
			{
				var current        = bucket.Entries;
				var now            = Environment.TickCount64;
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
					HitCount        = 0,
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
			var main       = expressions.MainExpression;
			var rootMember = main.NodeType switch
			{
				ExpressionType.Call         => (MemberInfo?)((MethodCallExpression)main).Method,
				ExpressionType.MemberAccess => ((MemberExpression)main).Member,
				_                           => null,
			};

			return new CacheKey(
				resultType,
				dataContext.GetType(),
				dataContext.ConfigurationID,
				queryFlags,
				dataContext.InlineParameters,
				dataContext is IInterceptable<IEntityServiceInterceptor> { Interceptor: { } },
				rootMember);
		}

		TimeSpan ResolveBaseTimeout(IDataContext dataContext)
			=> IdleTimeoutOverride ?? dataContext.Options.LinqOptions.CacheSlidingExpirationOrDefault;

		static bool IsExpired(Entry entry, long now, TimeSpan baseTimeout)
		{
			var elapsed   = now - Interlocked.Read(ref entry.LastAccessTicks);
			var threshold = EffectiveTimeoutMs(entry.HitCount, baseTimeout);
			return elapsed > threshold;
		}

		/// <summary>
		/// Effective idle timeout in milliseconds. Tiered on hit count:
		/// &lt; 5 → 1× base, &lt; 50 → 4×, &lt; 500 → 12×, ≥ 500 → 24× (cap).
		/// </summary>
		static long EffectiveTimeoutMs(int hitCount, TimeSpan baseTimeout)
		{
			var baseMs = (long)baseTimeout.TotalMilliseconds;

			return hitCount switch
			{
				<   5 => baseMs,
				<  50 => baseMs *  4,
				< 500 => baseMs * 12,
				_     => baseMs * 24,
			};
		}
	}
}
