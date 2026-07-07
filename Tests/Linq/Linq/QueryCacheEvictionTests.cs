#if BUGCHECK
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	/// <summary>
	/// Direct unit tests for <see cref="QueryCache"/> eviction strategies. Each test
	/// constructs an isolated <c>new QueryCache()</c> and drives it via the
	/// <c>#if BUGCHECK</c>-gated test hooks (<c>RunSweepNow</c>, <c>BucketCount</c>, etc.).
	///
	/// These tests verify eviction *mechanics* (sweep drops expired, cap trims oldest,
	/// ClearAll wipes). Find-correctness is covered by integration suites
	/// (<c>TestQueryCache</c>, <c>CachingTests</c>, <c>ParameterTests</c>).
	///
	/// Stub <see cref="Query{T}"/> instances created here have no <c>CompareInfo</c>,
	/// so <c>Query.Compare</c> always returns false. That's fine for tests of cap /
	/// sweep / trim — those code paths don't depend on Compare succeeding.
	/// </summary>
	[TestFixture]
	[Parallelizable(ParallelScope.All)]
	public class QueryCacheEvictionTests
	{
		// ---- Test scaffolding ---------------------------------------------------------

		sealed class T1 { public int Id; }
		sealed class T2 { public int Id; }

		[Table] sealed class TableA { [Column] public int Id; }
		[Table] sealed class TableB { [Column] public int Id; }

		static DataConnection NewContext()
		{
			// In-memory SQLite via the Microsoft provider — never actually executes anything,
			// only used for ConfigurationID / ContextType / InlineParameters metadata.
			var provider = SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft);
			var options  = new DataOptions().UseConnectionString(provider, "Data Source=:memory:");
			return new DataConnection(options);
		}

		static IQueryExpressions Expr(int seed) =>
			new RuntimeExpressionsContainer(Expression.Constant(seed));

		static Query AddStub(QueryCache cache, DataConnection db, int seed,
			QueryFlags flags = QueryFlags.None)
		{
			var query = new Query<int>(db);
			cache.TryAdd(typeof(int), db, query, Expr(seed), flags);
			return query;
		}

		static readonly Type[] SpreadResultTypes =
		{
			typeof(int), typeof(long), typeof(double), typeof(string),
			typeof(float), typeof(decimal), typeof(byte), typeof(short),
		};

		// Populates `count` entries spread across 8 result types. Each result type is a distinct bucket
		// key (ChainHash is identical for Constant<int>, but ResultType is part of the key), so the total
		// entry count can exceed a small cap while every bucket stays well under BucketCap = 16.
		static void PopulateSpread(QueryCache cache, DataConnection db, int count)
		{
			for (var i = 0; i < count; i++)
			{
				var resultType = SpreadResultTypes[i % SpreadResultTypes.Length];
				var query      = (Query)Activator.CreateInstance(typeof(Query<>).MakeGenericType(resultType), db)!;
				cache.TryAdd(resultType, db, query, Expr(i), QueryFlags.None);
			}
		}

		// ---- Eviction tests -----------------------------------------------------------

		[Test]
		public void IdleEviction_ColdEntryDroppedAfterTimeout()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride           = TimeSpan.FromMilliseconds(20),
				MemoryPressureEvictionEnabled = false, // isolate from the sweep's memory-pressure backstop
			};

			AddStub(cache, db, seed: 1);
			cache.CountEntries().ShouldBe(1, "entry should land");

			// Sleep at least the idle timeout, then poll RunSweepNow until eviction
			// completes. Tolerates timer-resolution jitter and scheduler delay on
			// contended runners — actual elapsed time can drift past 20ms, but the
			// 2s deadline keeps the test bounded on the happy path.
			Thread.Sleep(20);

			var sw = System.Diagnostics.Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < 2000)
			{
				cache.RunSweepNow();
				if (cache.CountEntries() == 0 && cache.BucketCount == 0)
					break;
				Thread.Sleep(5);
			}

			cache.CountEntries().ShouldBe(0, "cold entry should evict after timeout");
			cache.BucketCount   .ShouldBe(0, "empty bucket should be reaped");
		}

		[Test]
		public void IdleEviction_HotEntrySurvivesViaTier()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride           = TimeSpan.FromMilliseconds(50),
				// The memory-pressure backstop runs inside SweepGlobal; disable it here so a constrained
				// runner reporting high GC load can't trim the single hot entry out from under this test.
				MemoryPressureEvictionEnabled = false,
			};

			AddStub(cache, db, seed: 1);

			// Hammer the entry with simulated hits so the next sweep promotes it.
			cache.TrySimulateHits(typeof(int), db, Expr(1), QueryFlags.None, hits: 1000);

			// First sweep: HitsPerHour gets computed and stored. >= 500/hr → 24× tier.
			cache.RunSweepNow();

			// Sleep ~3× the base timeout — a 1× tier entry would have evicted.
			// 24× tier of 50ms = 1200ms; 150ms idle is fine.
			Thread.Sleep(150);
			cache.RunSweepNow();

			cache.CountEntries().ShouldBe(1,
				"hot entry should survive idle past the 1× base timeout");
		}

		[Test]
		public void BucketCap_OverflowEvictsOldest()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromHours(1), // don't let idle eviction interfere
			};

			// 20 distinct entries, all in the same bucket (same chain hash for Constant<int>).
			for (var i = 0; i < 20; i++)
				AddStub(cache, db, seed: i);

			var bucketCount = cache.CountEntriesInBucket(typeof(int), db, Expr(0), QueryFlags.None);

			bucketCount.ShouldBeLessThanOrEqualTo(16,
				"bucket cap should bound entries at BucketCap (16)");
		}

		[Test]
		public void GlobalCap_TrimsOldestFirst()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride           = TimeSpan.FromHours(1),
				MaxEntriesOverride            = 50,
				MemoryPressureEvictionEnabled = false, // isolate the cap-trim assertion from the pressure backstop
			};

			// Spread adds across multiple ResultType values. Each ResultType produces a
			// distinct bucket key (chain hash for Constant<int> is identical, but ResultType
			// is part of the key), so 8 distinct result types × N adds each = 8 buckets,
			// each well under BucketCap=16. Total entries grows past MaxEntries=50.
			Type[] resultTypes =
			{
				typeof(int), typeof(long), typeof(double), typeof(string),
				typeof(float), typeof(decimal), typeof(byte), typeof(short),
			};

			for (var i = 0; i < 100; i++)
			{
				var resultType = resultTypes[i % resultTypes.Length];
				var queryType  = typeof(Query<>).MakeGenericType(resultType);
				var query      = (Query)Activator.CreateInstance(queryType, db)!;
				cache.TryAdd(resultType, db, query, Expr(i), QueryFlags.None);
			}

			// Sanity: we crossed the cap before the trim.
			cache.ApproximateEntryCount.ShouldBeGreaterThan(50L,
				"workload should populate enough entries to exceed MaxEntriesOverride before trim");

			cache.RunSweepNow();

			cache.ApproximateEntryCount.ShouldBeLessThanOrEqualTo(50L,
				"global cap should trim back to MaxEntriesOverride");
		}

		[Test]
		public void ClearAll_RemovesAll()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromHours(1),
			};

			for (var i = 0; i < 5; i++)
				AddStub(cache, db, seed: i);

			cache.CountEntries().ShouldBeGreaterThan(0);

			cache.ClearAll();

			cache.ShouldSatisfyAllConditions(
				c => c.BucketCount          .ShouldBe(0),
				c => c.CountEntries()       .ShouldBe(0),
				c => c.ApproximateEntryCount.ShouldBe(0L));
		}

		[Test]
		public void ClearAll_VersionInvalidatesInFlightAdds()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromHours(1),
			};

			var stop = false;
			Exception? caught = null;

			var producer = Task.Run(() =>
			{
				try
				{
					for (var i = 0; !Volatile.Read(ref stop); i++)
						AddStub(cache, db, seed: i & 0xFFFF);
				}
				catch (Exception ex)
				{
					caught = ex;
				}
			});

			// Hammer ClearAll concurrently for a short window.
			var sw = System.Diagnostics.Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < 200)
				cache.ClearAll();

			Volatile.Write(ref stop, true);
			producer.Wait();

			caught.ShouldBeNull("concurrent TryAdd + ClearAll must not throw");

			// After both sides stop, the cache should be in a coherent state.
			cache.ClearAll();
			cache.CountEntries().ShouldBe(0);
		}

		[Test]
		public void DifferentResultTypes_GoToDifferentBuckets()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromHours(1),
			};

			IQueryExpressions exprA = new RuntimeExpressionsContainer(
				((IQueryable<TableA>)db.GetTable<TableA>().Where(x => x.Id == 1)).Expression);
			IQueryExpressions exprB = new RuntimeExpressionsContainer(
				((IQueryable<TableB>)db.GetTable<TableB>().Where(x => x.Id == 1)).Expression);

			cache.TryAdd(typeof(TableA), db, new Query<TableA>(db), exprA, QueryFlags.None);
			cache.TryAdd(typeof(TableB), db, new Query<TableB>(db), exprB, QueryFlags.None);

			cache.BucketCount.ShouldBe(2,
				"different ResultType (TableA vs TableB) should produce different bucket keys");
		}

		[Test]
		public void ChainHash_DifferentSourcesGoToDifferentBuckets()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromHours(1),
			};

			// Same ResultType (int), same ContextType / ConfigurationID / QueryFlags — the
			// only thing varying between the two adds is the source chain (TableA vs TableB).
			// The chain-hash walk visits the distinct MethodInfo / MemberInfo of each chain,
			// so this isolates chain-hash partitioning: if ComputeChainHash collapsed both
			// chains to the same value the two entries would share a bucket and this would
			// fail with BucketCount == 1.
			IQueryExpressions exprA = new RuntimeExpressionsContainer(
				((IQueryable<int>)db.GetTable<TableA>().Where(x => x.Id == 1).Select(x => x.Id)).Expression);
			IQueryExpressions exprB = new RuntimeExpressionsContainer(
				((IQueryable<int>)db.GetTable<TableB>().Where(x => x.Id == 1).Select(x => x.Id)).Expression);

			cache.TryAdd(typeof(int), db, new Query<int>(db), exprA, QueryFlags.None);
			cache.TryAdd(typeof(int), db, new Query<int>(db), exprB, QueryFlags.None);

			cache.BucketCount.ShouldBe(2,
				"identical ResultType but different source chains should hash to different buckets");
		}

		[Test]
		public void MaxEntriesOverride_ZeroDisablesCaching()
		{
			using var db = NewContext();

			var cache = new QueryCache { MaxEntriesOverride = 0 };

			AddStub(cache, db, seed: 1);

			cache.CountEntries().ShouldBe(0,
				"MaxEntriesOverride = 0 should reject all adds");
		}

		// ---- Memory-pressure eviction tests -------------------------------------------

		[Test]
		public void MemoryPressureTrim_RemovesColdestFraction()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromHours(1),
				MaxEntriesOverride  = 10000, // keep the count cap out of the way
			};

			PopulateSpread(cache, db, 100);
			cache.ApproximateEntryCount.ShouldBe(100L, "sanity: workload should populate 100 entries");

			cache.TrimForMemoryPressureNow(0.5);

			cache.ApproximateEntryCount.ShouldBe(50L, "half of the entries should be trimmed");
		}

		[Test]
		public void MemoryPressureTrim_PreservesHotEntries()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride           = TimeSpan.FromHours(1),
				MaxEntriesOverride            = 10000,
				// Drive the trim explicitly below; keep the bake sweep from trimming on its own.
				MemoryPressureEvictionEnabled = false,
			};

			// A dozen cold entries in the int bucket.
			for (var i = 0; i < 12; i++)
				AddStub(cache, db, seed: i);

			// One hot entry in its own (long) bucket: hammer it, then bake the hit-rate / extended
			// deadline in with a sweep so it sorts as the warmest entry.
			var hot = (Query)Activator.CreateInstance(typeof(Query<>).MakeGenericType(typeof(long)), db)!;
			cache.TryAdd(typeof(long), db, hot, Expr(0), QueryFlags.None);
			cache.TrySimulateHits(typeof(long), db, Expr(0), QueryFlags.None, hits: 1000);
			cache.RunSweepNow();

			// Trim almost everything: the coldest 12 (the int bucket) go, the hot entry stays.
			cache.TrimForMemoryPressureNow(0.9);

			cache.CountEntriesInBucket(typeof(long), db, Expr(0), QueryFlags.None)
				.ShouldBe(1, "the hot entry should survive an aggressive trim");
			cache.CountEntries().ShouldBe(1, "only the hot entry should remain");
		}

		[Test]
		public void Compact_PublicApi_TrimsRequestedFraction()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromHours(1),
				MaxEntriesOverride  = 10000,
			};

			PopulateSpread(cache, db, 100);

			cache.Compact(0.25);

			cache.ApproximateEntryCount.ShouldBe(75L, "Compact(0.25) should remove a quarter of the entries");
		}

		[Test]
		public void MemoryPressureTrim_FractionOne_ClearsAll()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromHours(1),
				MaxEntriesOverride  = 10000,
			};

			PopulateSpread(cache, db, 40);

			cache.TrimForMemoryPressureNow(1.0);

			cache.ShouldSatisfyAllConditions(
				c => c.CountEntries()       .ShouldBe(0),
				c => c.BucketCount          .ShouldBe(0),
				c => c.ApproximateEntryCount.ShouldBe(0L));
		}

		[Test]
		public void MemoryPressureTrim_PredicatePath_TrimsOnSweep()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride    = TimeSpan.FromHours(1),
				MaxEntriesOverride     = 10000,
				MemoryPressureOverride = () => true, // cross-platform "memory is high" signal
			};

			PopulateSpread(cache, db, 100);

			// The periodic backstop lives inside SweepGlobal; the predicate makes it fire deterministically.
			cache.RunSweepNow();

			cache.ApproximateEntryCount.ShouldBe(50L,
				"the memory-pressure predicate should drive the sweep backstop to trim the default half");
		}

		[Test]
		public void MemoryPressureTrim_Disabled_NoOp()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride           = TimeSpan.FromHours(1),
				MaxEntriesOverride            = 10000,
				MemoryPressureEvictionEnabled = false,
				MemoryPressureOverride        = () => true, // would trim if the feature were enabled
			};

			PopulateSpread(cache, db, 100);

			cache.RunSweepNow();
			cache.RunMemoryPressureCheckNow(nativeHigh: true);

			cache.ApproximateEntryCount.ShouldBe(100L, "disabled memory-pressure eviction must not trim");
		}

		[Test]
		public void MemoryPressureTrim_FractionOverride_Respected()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride                = TimeSpan.FromHours(1),
				MaxEntriesOverride                 = 10000,
				MemoryPressureTrimFractionOverride = 0.25,
			};

			PopulateSpread(cache, db, 100);

			cache.RunMemoryPressureCheckNow(nativeHigh: true);

			cache.ApproximateEntryCount.ShouldBe(75L,
				"the fraction override (0.25) should trim a quarter on a pressure event");
		}

		[Test]
		public void MemoryPressureTrim_EmptyCache_DoesNotThrow()
		{
			using var db = NewContext();

			var cache = new QueryCache();

			// Trimming an empty cache must be a safe no-op — an exception on any of these fails the test.
			cache.TrimForMemoryPressureNow(0.5);
			cache.Compact(0.5);
			cache.RunMemoryPressureCheckNow(nativeHigh: true);

			cache.CountEntries().ShouldBe(0);
		}

		[Test]
		public void MemoryPressureTrim_Gen2CallbackPath_TrimsAsync()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride    = TimeSpan.FromHours(1),
				MaxEntriesOverride     = 10000,
				MemoryPressureOverride = () => true, // force the gate so the real async path is deterministic
			};

			PopulateSpread(cache, db, 100);

			// Fire the real Gen2 callback: OnGen2Gc -> QueueGlobalMaintenance -> thread-pool sweep -> trim.
			cache.FireGen2CallbackNow();

			// The trim lands on the thread pool; poll until it completes (bounded).
			var sw = System.Diagnostics.Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < 2000 && cache.ApproximateEntryCount > 50L)
				Thread.Sleep(5);

			cache.ApproximateEntryCount.ShouldBeLessThanOrEqualTo(50L,
				"the Gen2 callback should asynchronously trim ~half under pressure");
			cache.ApproximateEntryCount.ShouldBeGreaterThan(0L, "the hot half should remain");
		}

		[Test]
		public void MemoryPressureTrim_ThrowingProbe_IsSwallowed()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride    = TimeSpan.FromHours(1),
				MaxEntriesOverride     = 10000,
				MemoryPressureOverride = () => throw new InvalidOperationException("probe boom"),
			};

			PopulateSpread(cache, db, 10);

			// The probe throws every time it is invoked; the shared PressureDetected gate (used by both the
			// sweep worker and this hook) must swallow it and treat it as "not under pressure". With the
			// native signal injected false, nothing is trimmed and no exception escapes.
			cache.RunMemoryPressureCheckNow(nativeHigh: false);

			cache.ApproximateEntryCount.ShouldBe(10L, "a throwing probe must be swallowed and must not trim");
		}
	}
}
#endif
