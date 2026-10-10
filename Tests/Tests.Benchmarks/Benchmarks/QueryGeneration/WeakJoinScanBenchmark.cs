using System;
using System.Data;
using System.Linq;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.QueryGeneration
{
	// Query-generation cost of shapes that produce many removable (weak) association joins:
	// a deep LoadWith chain (the https://github.com/linq2db/linq2db/issues/5265 shape) and a wide
	// entity whose one-to-one associations are all loaded.
	public class WeakJoinScanBenchmark
	{
		[Table]
		public sealed class ChainNode
		{
			[PrimaryKey] public int Id    { get; set; }
			[Column]     public int FK    { get; set; }
			[Column]     public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(FK))] public ChainNode? Next { get; set; }
		}

		[Table]
		public sealed class WideChild
		{
			[PrimaryKey] public int Id    { get; set; }
			[Column]     public int FK    { get; set; }
			[Column]     public int Field { get; set; }
		}

		[Table]
		public sealed class WideRoot
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C01 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C02 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C03 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C04 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C05 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C06 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C07 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C08 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C09 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C10 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C11 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C12 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C13 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C14 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C15 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C16 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C17 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C18 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C19 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(WideChild.FK))] public WideChild? C20 { get; set; }
		}

		DataConnection _db = null!;

#pragma warning disable CA2000 // Dispose objects before losing scope
		[GlobalSetup]
		public void Setup()
		{
			_db = new DataConnection(new DataOptions()
				.UseConnection(FirebirdTools.GetDataProvider(FirebirdVersion.v5), new MockDbConnection(Array.Empty<QueryResult>(), ConnectionState.Open))
				// the point of the benchmark is building the query, so it must not be served from the cache
				.UseDisableQueryCache(true));
		}
#pragma warning restore CA2000 // Dispose objects before losing scope

		[GlobalCleanup]
		public void Cleanup()
		{
			_db.Dispose();
		}

		[Benchmark]
		public string DeepChain13()
		{
			return _db.GetTable<ChainNode>()
				.LoadWith(x => x.Next!.Next!.Next!.Next!.Next!.Next!.Next!.Next!.Next!.Next!.Next!.Next!.Next)
				.ToSqlQuery().Sql;
		}

		[Benchmark]
		public string Wide20()
		{
			return _db.GetTable<WideRoot>()
				.LoadWith(x => x.C01).LoadWith(x => x.C02).LoadWith(x => x.C03).LoadWith(x => x.C04)
				.LoadWith(x => x.C05).LoadWith(x => x.C06).LoadWith(x => x.C07).LoadWith(x => x.C08)
				.LoadWith(x => x.C09).LoadWith(x => x.C10).LoadWith(x => x.C11).LoadWith(x => x.C12)
				.LoadWith(x => x.C13).LoadWith(x => x.C14).LoadWith(x => x.C15).LoadWith(x => x.C16)
				.LoadWith(x => x.C17).LoadWith(x => x.C18).LoadWith(x => x.C19).LoadWith(x => x.C20)
				.ToSqlQuery().Sql;
		}

		// Manual runner, mirroring CacheActivityBenchmark.RunManually: BDN's child-process toolchain
		// cannot restore its auto-generated project in this repo layout (NU1101), and a single
		// operation here costs seconds, so the auto-iteration toolchain is impractical for A/B runs.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030", Justification = "Benchmark output requires Console")]
		public static void RunManually(int warmups = 2, int iterations = 8)
		{
			if (warmups    < 0) warmups    = 0;
			if (iterations < 1) iterations = 1;

			var b = new WeakJoinScanBenchmark();
			b.Setup();

			var methods = new (string Name, Func<string> Run)[]
			{
				("DeepChain13", b.DeepChain13),
				("Wide20",      b.Wide20),
			};

			var tag = Environment.GetEnvironmentVariable("WEAKJOIN_BENCH_TAG") ?? "(unknown)";

			Console.WriteLine();
			Console.WriteLine("=== WeakJoinScanBenchmark manual run | tag: " + tag + " ===");
			Console.WriteLine();
			Console.WriteLine("| Benchmark | Mean (ms) | Median (ms) | Min (ms) | SQL len |");
			Console.WriteLine("|---|---:|---:|---:|---:|");

			foreach (var (name, run) in methods)
			{
				for (var i = 0; i < warmups; i++)
					run();

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var samples = new double[iterations];
				var sw      = new System.Diagnostics.Stopwatch();
				var sqlLen  = 0;

				for (var i = 0; i < iterations; i++)
				{
					sw.Restart();
					var sql = run();
					sw.Stop();
					samples[i] = sw.Elapsed.TotalMilliseconds;
					sqlLen     = sql.Length;
				}

				var mean = samples.Average();

				Array.Sort(samples);
				var mid    = samples.Length / 2;
				var median = samples.Length % 2 == 0
					? (samples[mid - 1] + samples[mid]) / 2.0
					: samples[mid];

				Console.WriteLine($"| {name,-12} | {mean,9:F1} | {median,11:F1} | {samples[0],8:F1} | {sqlLen,7} |");
			}

			Console.WriteLine();

			b.Cleanup();
		}
	}
}
