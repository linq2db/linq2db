using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.Linq;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Queries
{
	/// <summary>
	/// Realistic-workload cache benchmarks. Uses only public LinqToDB APIs so the same
	/// file compiles on master and on the cache refactor branch — diffs between runs are
	/// attributable to the cache change.
	/// </summary>
	[MemoryDiagnoser]
	public class CacheActivityBenchmark
	{
		[Table] public sealed class T1 { [Column] public int    Id; [Column] public string?   Name;    [Column] public int     Value; }
		[Table] public sealed class T2 { [Column] public int    Id; [Column] public DateTime  Created; [Column] public bool    Flag;  }
		[Table] public sealed class T3 { [Column] public Guid   Id; [Column] public string?   Code;    [Column] public decimal Amount;}
		[Table] public sealed class T4 { [Column] public long   Id; [Column] public string?   Tag;     [Column] public double  Score; }

		DbConnection                    _cn          = null!;
		DataConnection                  _db          = null!;
		MappingSchema                   _baseSchema  = null!;
		DataOptions                     _baseOptions = null!;
		Func<DataConnection, int, int>? _compiled;

		[GlobalSetup]
		public void Setup()
		{
			_cn          = new MockDbConnection(new QueryResult { Return = 1 }, ConnectionState.Open);
			_baseSchema  = new MappingSchema();
			_baseOptions = new DataOptions().UseConnection(SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft), _cn);
			_db          = new DataConnection(_baseOptions);

			Query.ClearCaches();

			_compiled = CompiledQuery.Compile<DataConnection, int, int>(
				(db, id) => db.GetTable<T1>().Count(x => x.Id == id));
		}

		// === Bucket fill: many predicate combinations, same root method ===
		[Benchmark]
		public int DynamicWhere()
		{
			var total = 0;

			for (var i = 0; i < 200; i++)
			{
				IQueryable<T1> q = _db.GetTable<T1>();

				var includeName  = (i & 1) == 1;
				var includeValue = (i & 2) == 2;
				var includeId    = (i & 4) == 4;

				if (includeName)  q = q.Where(x => x.Name == "x");
				if (includeValue) q = q.Where(x => x.Value > i);
				if (includeId)    q = q.Where(x => x.Id < 1000);

				total += q.Count();
			}

			return total;
		}

		// === Chain-hash partitioning by ResultType ===
		[Benchmark]
		public int HeterogeneousTypes()
		{
			var total = 0;

			for (var i = 0; i < 200; i++)
			{
				total += (i & 3) switch
				{
					0 => _db.GetTable<T1>().Count(x => x.Id     == i),
					1 => _db.GetTable<T2>().Count(x => x.Flag),
					2 => _db.GetTable<T3>().Count(x => x.Amount > 0m),
					_ => _db.GetTable<T4>().Count(x => x.Score  > 0.5),
				};
			}

			return total;
		}

		// === New MappingSchema → new ConfigurationID → new bucket key ===
		[Benchmark]
		public int MappingSchemaChurn()
		{
			var total   = 0;
			var schemas = new List<MappingSchema> { _baseSchema };

			for (var i = 0; i < 200; i++)
			{
				if (i % 20 == 0)
					schemas.Add(new MappingSchema());

				var schema = schemas[i % schemas.Count];
				var opts   = _baseOptions.UseMappingSchema(schema);

				using var db = new DataConnection(opts);

				total += db.GetTable<T1>().Count(x => x.Id == i);
			}

			return total;
		}

		// === Realistic mix: 70 % hot hits, 20 % param churn, 10 % cold ===
		[Benchmark]
		public int RealisticMix()
		{
			var total = 0;

			for (var i = 0; i < 500; i++)
			{
				var bucket = i % 10;

				if (bucket < 7)
					total += _db.GetTable<T1>().Count(x => x.Id == i);
				else if (bucket < 9)
					total += _db.GetTable<T2>().Count(x => x.Created > new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
				else
				{
					var threshold = i * 7;
					total += _db.GetTable<T3>().Count(x => x.Code == "C" + threshold);
				}
			}

			return total;
		}

		// === Chain hash truncates at 8 levels — collisions handled by Compare ===
		[Benchmark]
		public int LongChainQueries()
		{
			var total = 0;

			for (var i = 0; i < 100; i++)
			{
				total += _db.GetTable<T1>()
					.Where  (x => x.Id    == i)
					.Where  (x => x.Name  != null)
					.Where  (x => x.Value > 0)
					.Select (x => new { x.Id, x.Name })
					.Where  (x => x.Id    < 1000)
					.OrderBy(x => x.Id)
					.Take   (10)
					.Count();
			}

			return total;
		}

		// === DoNotCache short-circuit ===
		[Benchmark]
		public int NoLinqCacheScope()
		{
			using var _ = NoLinqCache.Scope();

			var total = 0;

			for (var i = 0; i < 100; i++)
				total += _db.GetTable<T1>().Count(x => x.Id == i);

			return total;
		}

		// === Tier promotion + decay ===
		[Benchmark]
		public int HotThenColdThenHot()
		{
			var total = 0;

			// 100 rapid hits — push HitsPerHour into the high tier.
			for (var i = 0; i < 100; i++)
				total += _db.GetTable<T1>().Count(x => x.Id == 42);

			// "Cold period" — exercise unrelated queries; the hot one above should remain
			// cached via the rate-tier deadline extension.
			for (var i = 0; i < 50; i++)
				total += _db.GetTable<T2>().Count(x => x.Id == i);

			// Hot query again — should still hit the cache.
			for (var i = 0; i < 100; i++)
				total += _db.GetTable<T1>().Count(x => x.Id == 42);

			return total;
		}

		// === Per-bucket cap (16) — eviction priority (expiry → access → rate) ===
		[Benchmark]
		public int BucketCapEviction()
		{
			var total = 0;

			// 30 distinct queries with same root method → all land in the same bucket;
			// triggers the cap eviction repeatedly.
			for (var i = 0; i < 30; i++)
			{
				var threshold = i;
				total += _db.GetTable<T1>().Count(x => x.Id == threshold);
			}

			return total;
		}

		// === Global cap (DefaultMaxEntries) — TrimGlobalToCapacity ===
		[Benchmark]
		public int GlobalCapPressure()
		{
			var total = 0;

			// 4 000 distinct queries via parameter rotation across 4 types.
			for (var i = 0; i < 1000; i++)
			{
				total += _db.GetTable<T1>().Count(x => x.Value  == i);
				total += _db.GetTable<T2>().Count(x => x.Created > new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i));
				total += _db.GetTable<T3>().Count(x => x.Amount  > i);
				total += _db.GetTable<T4>().Count(x => x.Score   > i * 0.1);
			}

			return total;
		}

		// === CompiledQuery (Phase 1B per-instance cache) ===
		[Benchmark]
		public int CompiledQueryReuse()
		{
			var total = 0;

			for (var i = 0; i < 500; i++)
				total += _compiled!(_db, i);

			return total;
		}

		// === Compiled + regular interleaved — separate caches ===
		[Benchmark]
		public int CompiledVsRegularMixed()
		{
			var total = 0;

			for (var i = 0; i < 500; i++)
			{
				total += (i & 1) == 1
					? _compiled!(_db, i)
					: _db.GetTable<T1>().Count(x => x.Id == i);
			}

			return total;
		}

		// === Concurrent reads — lock-free reader path ===
		[Benchmark]
		public int ConcurrentHits()
		{
			// Pre-populate so all threads hit.
			_db.GetTable<T1>().Count(x => x.Id == 1);

			var total = 0;

			Parallel.For(0, 8, _ =>
			{
				for (var i = 0; i < 100; i++)
					Interlocked.Add(ref total, _db.GetTable<T1>().Count(x => x.Id == 1));
			});

			return total;
		}

		// === Concurrent writes to *different* buckets — per-bucket lock granularity ===
		// Each thread uses a unique chain shape → different ChainHash → different bucket key.
		// Tests that per-bucket locks let parallel threads add without cross-bucket contention.
		[Benchmark]
		public int ConcurrentDistinctTryAdds()
		{
			Query.ClearCaches();

			var total = 0;

			Parallel.For(0, 8, threadId =>
			{
				for (var i = 0; i < 50; i++)
				{
					var v = i;

					var local = (threadId & 7) switch
					{
						0 => _db.GetTable<T1>().Count    (x => x.Id      == v),
						1 => _db.GetTable<T2>().Count    (x => x.Id      == v),
						2 => _db.GetTable<T3>().Count    (x => x.Amount  >  v),
						3 => _db.GetTable<T4>().Count    (x => x.Score   >  v),
						4 => _db.GetTable<T1>().Where    (x => x.Name    != null).Count(),
						5 => _db.GetTable<T2>().Where    (x => x.Flag).Count(),
						6 => (int)_db.GetTable<T3>().LongCount(x => x.Code    == "x"),
						_ => _db.GetTable<T4>().Where    (x => x.Tag     == "y").Count(),
					};

					Interlocked.Add(ref total, local);
				}
			});

			return total;
		}

		// === Concurrent writes to same bucket — bucket-lock contention worst case ===
		[Benchmark]
		public int ConcurrentSameBucketTryAdds()
		{
			Query.ClearCaches();

			var total = 0;

			Parallel.For(0, 8, threadId =>
			{
				for (var i = 0; i < 50; i++)
					Interlocked.Add(ref total, _db.GetTable<T1>().Count(x => x.Id == threadId * 100 + i));
			});

			return total;
		}

		// === Mixed reader / writer load — realistic concurrent contention ===
		// 4 threads constantly hitting hot keys (cache hits, lock-free reader path) +
		// 4 threads constantly adding novel queries (TryAdd, per-bucket lock writes).
		// Tests that readers don't block on writers and vice versa.
		[Benchmark]
		public int ConcurrentMixedLoad()
		{
			// Pre-populate the hot keys.
			_db.GetTable<T1>().Count(x => x.Id == 1);
			_db.GetTable<T2>().Count(x => x.Flag);

			var total = 0;

			Parallel.For(0, 8, threadId =>
			{
				if ((threadId & 1) == 0)
				{
					// Reader threads — hot keys.
					for (var i = 0; i < 100; i++)
					{
						Interlocked.Add(ref total, _db.GetTable<T1>().Count(x => x.Id == 1));
						Interlocked.Add(ref total, _db.GetTable<T2>().Count(x => x.Flag));
					}
				}
				else
				{
					// Writer threads — novel queries each iteration.
					var rng = new Random(threadId);
					for (var i = 0; i < 100; i++)
					{
						var v = rng.Next(10_000);
						Interlocked.Add(ref total, _db.GetTable<T1>().Count(x => x.Value == v));
					}
				}
			});

			return total;
		}

		// === Concurrent cap pressure — multiple threads all driving the global cap ===
		// Stresses TrimGlobalToCapacity under contention: many threads adding entries
		// past DefaultMaxEntries, single-flighted sweep claims the work, threads keep
		// running while it trims.
		[Benchmark]
		public int ConcurrentCapPressure()
		{
			Query.ClearCaches();

			var total = 0;

			Parallel.For(0, 8, threadId =>
			{
				var baseV = threadId * 2_000;
				for (var i = 0; i < 500; i++)
				{
					var v = baseV + i;
					Interlocked.Add(ref total, _db.GetTable<T1>().Count(x => x.Value == v));
				}
			});

			return total;
		}

		// === ClearAll under load — version-bracket safety ===
		[Benchmark]
		public int ClearAllUnderLoad()
		{
			var total = 0;

			Parallel.For(0, 4, threadId =>
			{
				for (var i = 0; i < 50; i++)
				{
					if (threadId == 0 && i % 10 == 0)
						Query.ClearCaches();

					Interlocked.Add(ref total, _db.GetTable<T1>().Count(x => x.Id == i));
				}
			});

			return total;
		}

		// === Memory footprint ===
		[Benchmark]
		public long CacheFootprint()
		{
			Query.ClearCaches();

			for (var i = 0; i < 500; i++)
			{
				_db.GetTable<T1>().Count(x => x.Id     == i);
				_db.GetTable<T2>().Count(x => x.Flag);
				_db.GetTable<T3>().Count(x => x.Amount > i);
				_db.GetTable<T4>().Count(x => x.Score  > i * 0.1);
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			return GC.GetTotalMemory(forceFullCollection: true);
		}
	}
}
