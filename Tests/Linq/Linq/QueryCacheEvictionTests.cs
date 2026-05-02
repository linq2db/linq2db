#if BUGCHECK
using System;
using System.Data.Common;
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

		// ---- Eviction tests -----------------------------------------------------------

		[Test]
		public void IdleEviction_ColdEntryDroppedAfterTimeout()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromMilliseconds(20),
			};

			AddStub(cache, db, seed: 1);
			cache.CountEntries().ShouldBe(1, "entry should land");

			Thread.Sleep(60);
			cache.RunSweepNow();

			cache.CountEntries().ShouldBe(0, "cold entry should evict after timeout");
			cache.BucketCount   .ShouldBe(0, "empty bucket should be reaped");
		}

		[Test]
		public void IdleEviction_HotEntrySurvivesViaTier()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromMilliseconds(50),
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
				IdleTimeoutOverride = TimeSpan.FromHours(1),
				MaxEntriesOverride  = 50,
			};

			// Each TryAdd uses a unique chain shape (different seed → different Constant value).
			// All same-chain-hash → same bucket. Bucket cap kicks in before we hit the global cap.
			// Use REAL distinct chain shapes via different ResultType to spread across buckets.
			for (var i = 0; i < 60; i++)
			{
				var query = new Query<int>(db);
				cache.TryAdd(typeof(int), db, query, Expr(i), QueryFlags.None);

				var query2 = new Query<long>(db);
				cache.TryAdd(typeof(long), db, query2, Expr(i), QueryFlags.None);
			}

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
		public void ChainHash_DifferentSourcesGoToDifferentBuckets()
		{
			using var db = NewContext();

			var cache = new QueryCache
			{
				IdleTimeoutOverride = TimeSpan.FromHours(1),
			};

			// Real LINQ chains so the chain-hash walk visits actual MethodInfo + MemberInfo.
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
		public void MaxEntriesOverride_ZeroDisablesCaching()
		{
			using var db = NewContext();

			var cache = new QueryCache { MaxEntriesOverride = 0 };

			AddStub(cache, db, seed: 1);

			cache.CountEntries().ShouldBe(0,
				"MaxEntriesOverride = 0 should reject all adds");
		}
	}
}
#endif
