using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

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
	/// <remarks>
	/// Forces the InProcessEmit toolchain so BDN doesn't spawn child processes.
	/// Required when antivirus / endpoint-protection blocks dynamic child-process exec.
	/// </remarks>
	[Config(typeof(BenchmarkConfig))]
	[MemoryDiagnoser]
	public class CacheActivityBenchmark
	{
		sealed class BenchmarkConfig : ManualConfig
		{
			public BenchmarkConfig()
			{
				AddJob(Job.Default
					.WithToolchain(InProcessEmitToolchain.Instance)
					.WithWarmupCount  (3)
					.WithIterationCount(5));
				AddDiagnoser(MemoryDiagnoser.Default);
				AddExporter (MarkdownExporter.GitHub);
			}
		}

		[Table] public sealed class T1 { [Column] public int    Id; [Column] public string?   Name;    [Column] public int     Value; }
		[Table] public sealed class T2 { [Column] public int    Id; [Column] public DateTime  Created; [Column] public bool    Flag;  }
		[Table] public sealed class T3 { [Column] public Guid   Id; [Column] public string?   Code;    [Column] public decimal Amount;}
		[Table] public sealed class T4 { [Column] public long   Id; [Column] public string?   Tag;     [Column] public double  Score; }

		// Helper: trigger LINQ translation + cache lookup without executing on the mock.
		// Returns SQL length so the JIT can't elide the call.
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		static int Translate<T>(IQueryable<T> q) => q.ToSqlQuery().Sql.Length;

		DbConnection                                _cn          = null!;
		DataConnection                              _db          = null!;
		MappingSchema                               _baseSchema  = null!;
		DataOptions                                 _baseOptions = null!;
		Func<DataConnection, int, IQueryable<T1>>?  _compiled;

		[GlobalSetup]
		public void Setup()
		{
			_cn          = new MockDbConnection(new QueryResult { Return = 1 }, ConnectionState.Open);
			_baseSchema  = new MappingSchema();
			_baseOptions = new DataOptions().UseConnection(SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft), _cn);
			_db          = new DataConnection(_baseOptions);

			Query.ClearCaches();

			_compiled = CompiledQuery.Compile<DataConnection, int, IQueryable<T1>>(
				(db, id) => db.GetTable<T1>().Where(x => x.Id == id));
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

				total += Translate(q);
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
					0 => Translate(_db.GetTable<T1>().Where(x => x.Id     == i)),
					1 => Translate(_db.GetTable<T2>().Where(x => x.Flag)),
					2 => Translate(_db.GetTable<T3>().Where(x => x.Amount > 0m)),
					_ => Translate(_db.GetTable<T4>().Where(x => x.Score  > 0.5)),
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

				total += Translate(db.GetTable<T1>().Where(x => x.Id == i));
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
					total += Translate(_db.GetTable<T1>().Where(x => x.Id == i));
				else if (bucket < 9)
					total += Translate(_db.GetTable<T2>().Where(x => x.Created > new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
				else
				{
					var threshold = i * 7;
					total += Translate(_db.GetTable<T3>().Where(x => x.Code == "C" + threshold));
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
				total += Translate(_db.GetTable<T1>()
					.Where  (x => x.Id    == i)
					.Where  (x => x.Name  != null)
					.Where  (x => x.Value > 0)
					.Select (x => new { x.Id, x.Name })
					.Where  (x => x.Id    < 1000)
					.OrderBy(x => x.Id)
					.Take   (10));
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
				total += Translate(_db.GetTable<T1>().Where(x => x.Id == i));

			return total;
		}

		// === Tier promotion + decay ===
		[Benchmark]
		public int HotThenColdThenHot()
		{
			var total = 0;

			// 100 rapid hits — push HitsPerHour into the high tier.
			for (var i = 0; i < 100; i++)
				total += Translate(_db.GetTable<T1>().Where(x => x.Id == 42));

			// "Cold period" — exercise unrelated queries; the hot one above should remain
			// cached via the rate-tier deadline extension.
			for (var i = 0; i < 50; i++)
				total += Translate(_db.GetTable<T2>().Where(x => x.Id == i));

			// Hot query again — should still hit the cache.
			for (var i = 0; i < 100; i++)
				total += Translate(_db.GetTable<T1>().Where(x => x.Id == 42));

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
				total += Translate(_db.GetTable<T1>().Where(x => x.Id == threshold));
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
				total += Translate(_db.GetTable<T1>().Where(x => x.Value  == i));
				total += Translate(_db.GetTable<T2>().Where(x => x.Created > new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i)));
				total += Translate(_db.GetTable<T3>().Where(x => x.Amount  > i));
				total += Translate(_db.GetTable<T4>().Where(x => x.Score   > i * 0.1));
			}

			return total;
		}

		// === CompiledQuery (Phase 1B per-instance cache) ===
		[Benchmark]
		public int CompiledQueryReuse()
		{
			var total = 0;

			for (var i = 0; i < 500; i++)
				total += Translate(_compiled!(_db, i));

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
					? Translate(_compiled!(_db, i))
					: Translate(_db.GetTable<T1>().Where(x => x.Id == i));
			}

			return total;
		}

		// === Concurrent reads — lock-free reader path ===
		[Benchmark]
		public int ConcurrentHits()
		{
			// Pre-populate so all threads hit.
			Translate(_db.GetTable<T1>().Where(x => x.Id == 1));

			var total = 0;

			Parallel.For(0, 8, _ =>
			{
				for (var i = 0; i < 100; i++)
					Interlocked.Add(ref total, Translate(_db.GetTable<T1>().Where(x => x.Id == 1)));
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
						0 => Translate(_db.GetTable<T1>().Where(x => x.Id     == v)),
						1 => Translate(_db.GetTable<T2>().Where(x => x.Id     == v)),
						2 => Translate(_db.GetTable<T3>().Where(x => x.Amount >  v)),
						3 => Translate(_db.GetTable<T4>().Where(x => x.Score  >  v)),
						4 => Translate(_db.GetTable<T1>().Where(x => x.Name   != null).Where(x => x.Value == v)),
						5 => Translate(_db.GetTable<T2>().Where(x => x.Flag).Where(x => x.Id == v)),
						6 => Translate(_db.GetTable<T3>().Where(x => x.Code   == "x").Where(x => x.Amount > v)),
						_ => Translate(_db.GetTable<T4>().Where(x => x.Tag    == "y").Where(x => x.Score > v)),
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
					Interlocked.Add(ref total, Translate(_db.GetTable<T1>().Where(x => x.Id == threadId * 100 + i)));
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
			Translate(_db.GetTable<T1>().Where(x => x.Id == 1));
			Translate(_db.GetTable<T2>().Where(x => x.Flag));

			var total = 0;

			Parallel.For(0, 8, threadId =>
			{
				if ((threadId & 1) == 0)
				{
					// Reader threads — hot keys.
					for (var i = 0; i < 100; i++)
					{
						Interlocked.Add(ref total, Translate(_db.GetTable<T1>().Where(x => x.Id == 1)));
						Interlocked.Add(ref total, Translate(_db.GetTable<T2>().Where(x => x.Flag)));
					}
				}
				else
				{
					// Writer threads — novel queries each iteration.
					var rng = new Random(threadId);
					for (var i = 0; i < 100; i++)
					{
						var v = rng.Next(10_000);
						Interlocked.Add(ref total, Translate(_db.GetTable<T1>().Where(x => x.Value == v)));
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
					Interlocked.Add(ref total, Translate(_db.GetTable<T1>().Where(x => x.Value == v)));
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

					Interlocked.Add(ref total, Translate(_db.GetTable<T1>().Where(x => x.Id == i)));
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
				Translate(_db.GetTable<T1>().Where(x => x.Id     == i));
				Translate(_db.GetTable<T2>().Where(x => x.Flag));
				Translate(_db.GetTable<T3>().Where(x => x.Amount > i));
				Translate(_db.GetTable<T4>().Where(x => x.Score  > i * 0.1));
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			return GC.GetTotalMemory(forceFullCollection: true);
		}

		// ----------------------------------------------------------------------------------
		// Manual runner — for when BDN's child-process toolchain is blocked by antivirus.
		// Use a small, deterministic Stopwatch loop and emit a markdown table to stdout.
		// Less statistical rigor than BDN but gives stable enough numbers for branch-vs-master.
		// ----------------------------------------------------------------------------------

		static long GetAllocatedBytes()
		{
#if NETCOREAPP3_0_OR_GREATER
			return GC.GetTotalAllocatedBytes(precise: true);
#else
			return GC.GetTotalMemory(forceFullCollection: false);
#endif
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030", Justification = "Benchmark output requires Console")]
		public static void RunManually(int warmups = 5, int iterations = 20)
		{
			var b = new CacheActivityBenchmark();
			b.Setup();

			var methods = new (string Name, Action Run)[]
			{
				("DynamicWhere",                () => b.DynamicWhere()),
				("HeterogeneousTypes",          () => b.HeterogeneousTypes()),
				("MappingSchemaChurn",          () => b.MappingSchemaChurn()),
				("RealisticMix",                () => b.RealisticMix()),
				("LongChainQueries",            () => b.LongChainQueries()),
				("NoLinqCacheScope",            () => b.NoLinqCacheScope()),
				("HotThenColdThenHot",          () => b.HotThenColdThenHot()),
				("BucketCapEviction",           () => b.BucketCapEviction()),
				("GlobalCapPressure",           () => b.GlobalCapPressure()),
				("CompiledQueryReuse",          () => b.CompiledQueryReuse()),
				("CompiledVsRegularMixed",      () => b.CompiledVsRegularMixed()),
				("ConcurrentHits",              () => b.ConcurrentHits()),
				("ConcurrentDistinctTryAdds",   () => b.ConcurrentDistinctTryAdds()),
				("ConcurrentSameBucketTryAdds", () => b.ConcurrentSameBucketTryAdds()),
				("ConcurrentMixedLoad",         () => b.ConcurrentMixedLoad()),
				("ConcurrentCapPressure",       () => b.ConcurrentCapPressure()),
				("ClearAllUnderLoad",           () => b.ClearAllUnderLoad()),
				("CacheFootprint",              () => b.CacheFootprint()),
			};

			var commitSha = Environment.GetEnvironmentVariable("CACHE_BENCH_TAG") ?? "(unknown)";

			Console.WriteLine();
			Console.WriteLine("=== CacheActivityBenchmark manual run | tag: " + commitSha + " ===");
			Console.WriteLine();
			Console.WriteLine("| Benchmark | Mean (ms) | Median (ms) | Allocated (KB/op) |");
			Console.WriteLine("|---|---:|---:|---:|");

			foreach (var (name, run) in methods)
			{
				// Warmup
				for (var i = 0; i < warmups; i++)
					run();

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var bytesBefore = GetAllocatedBytes();
				var samples     = new double[iterations];
				var sw          = new System.Diagnostics.Stopwatch();

				for (var i = 0; i < iterations; i++)
				{
					sw.Restart();
					run();
					sw.Stop();
					samples[i] = sw.Elapsed.TotalMilliseconds;
				}

				var bytesAfter = GetAllocatedBytes();
				var allocKb    = (bytesAfter - bytesBefore) / 1024.0 / iterations;
				var mean       = samples.Average();

				Array.Sort(samples);
				var median = samples[samples.Length / 2];

				Console.WriteLine($"| {name,-30} | {mean,9:F2} | {median,11:F2} | {allocKb,17:F1} |");
			}

			Console.WriteLine();
		}
	}
}
