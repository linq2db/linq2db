using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Queries
{
	/// <summary>
	/// Eager-loading (<c>LoadWith</c>) prepare/execute/retained-memory benchmarks. Unlike
	/// <see cref="RenderPipelineBenchmark"/> (which mocks the connection to isolate the single-statement render
	/// path), eager loading issues a main query plus a detail query and materializes both, so it needs REAL result
	/// sets — a mock cannot produce them. This runs over a real in-memory SQLite database (Microsoft provider),
	/// kept alive for the whole run via a shared-cache keep-alive connection.
	/// The eager cache (<c>QueryInfo.EagerCommandCache</c>) still retains the statement graph (it was not migrated to
	/// the statement-free <c>BakedQuery</c>), so the retained-footprint measurement here is the branch-vs-master memory
	/// signal for that path. Uses only public LinqToDB APIs, so the same file compiles on master and on the branch.
	/// </summary>
	/// <remarks>Forces the InProcessEmit toolchain so BDN doesn't spawn child processes (AV-safe).</remarks>
	[Config(typeof(BenchmarkConfig))]
	public class EagerLoadingBenchmark
	{
		sealed class BenchmarkConfig : ManualConfig
		{
			public BenchmarkConfig()
			{
				AddJob(Job.Default
					.WithToolchain(InProcessEmitToolchain.Instance)
					.WithWarmupCount  (6)
					.WithIterationCount(20));
				AddDiagnoser(MemoryDiagnoser.Default);
				AddExporter (MarkdownExporter.GitHub);
			}
		}

		[Table("EagerParent")]
		public sealed class Parent
		{
			[Column, PrimaryKey] public int     Id    { get; set; }
			[Column]             public string? Name  { get; set; }
			[Column]             public int     Value { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Child.ParentId))]
			public List<Child> Children { get; set; } = null!;
		}

		[Table("EagerChild")]
		public sealed class Child
		{
			[Column, PrimaryKey] public int Id       { get; set; }
			[Column]             public int ParentId { get; set; }
			[Column]             public int Value    { get; set; }
		}

		// A named shared-cache in-memory database: it lives as long as at least one connection to it is open, so the
		// keep-alive connection below anchors it while LinqToDB opens/closes its own connections from the same string.
		// Unique per instance so BenchmarkDotNet's per-benchmark GlobalSetup never collides on an already-created table
		// (a pooled connection can keep a shared in-memory DB alive between benchmark methods).
		readonly string _connectionString = "Data Source=EagerLoadingBenchmark_" + Guid.NewGuid().ToString("N") + ";Mode=Memory;Cache=Shared";

		const int Iterations       = 100;  // hot-loop repetitions (cached eager query re-executed)
		const int DistinctCount    = 32;   // number of structurally-distinct eager query shapes
		const int ParentCount      = 40;
		const int ChildrenPerParent = 5;

		IDataProvider  _provider  = null!;
		DbConnection   _keepAlive = null!;
		DataConnection _db        = null!;

		[GlobalSetup]
		public void Setup()
		{
			_provider = SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft);

			// Anchor the shared in-memory database for the whole run.
			_keepAlive = _provider.CreateConnection(_connectionString);
			_keepAlive.Open();

			_db = new DataConnection(new DataOptions().UseConnectionString(_provider, _connectionString));
			_db.CreateTable<Parent>();
			_db.CreateTable<Child>();

			for (var p = 0; p < ParentCount; p++)
			{
				_db.Insert(new Parent { Id = p, Name = "P" + p, Value = p });

				for (var c = 0; c < ChildrenPerParent; c++)
					_db.Insert(new Child { Id = p * ChildrenPerParent + c, ParentId = p, Value = c });
			}

			Query.ClearCaches();
		}

		[GlobalCleanup]
		public void Cleanup()
		{
			_db?.Dispose();
			_keepAlive?.Dispose();
		}

		// === Hot path: the same eager query executed repeatedly (EagerCommandCache reuse + interpreter + map) ===
		[Benchmark]
		public int Execute_Hot()
		{
			var total = 0;

			for (var i = 0; i < Iterations; i++)
				total += _db.GetTable<Parent>().LoadWith(p => p.Children).ToList().Count;

			return total;
		}

		// === Cold prepare: DistinctCount structurally-distinct eager queries (cache miss + full build + execute each) ===
		[Benchmark]
		public int Prepare_Cold()
		{
			Query.ClearCaches();

			var total = 0;

			for (var i = 0; i < DistinctCount; i++)
				total += BuildDistinct(i).ToList().Count;

			return total;
		}

		// Builds a structurally-distinct eager query for index i (literal predicates only, so each shape gets its own
		// cache entry). Every shape keeps the LoadWith so the eager scenario (EagerCommandCache) is what gets cached.
		IQueryable<Parent> BuildDistinct(int i)
		{
			IQueryable<Parent> q = _db.GetTable<Parent>().LoadWith(p => p.Children);

			if ((i & 1) != 0) q = q.Where(p => p.Value > 0);
			if ((i & 2) != 0) q = q.Where(p => p.Name != null);
			if ((i & 4) != 0) q = q.Where(p => p.Id    > 0);
			if ((i & 8) != 0) q = q.OrderBy(p => p.Id);

			return ((i >> 4) % 2) switch
			{
				0 => q.Where(p => p.Value < 1000),
				_ => q.Where(p => p.Name  != "z").OrderByDescending(p => p.Value),
			};
		}

		// ----------------------------------------------------------------------------------
		// Manual runner — for when BDN's child-process toolchain is blocked by antivirus.
		// Emits a markdown table (Mean/Median/Allocated per op) plus the retained-memory footprint of many distinct
		// cached eager queries, tagged via RENDER_BENCH_TAG so two runs (branch vs master) can be diffed.
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
			if (warmups    < 0) warmups    = 0;
			if (iterations < 1) iterations = 1;

			var b = new EagerLoadingBenchmark();
			b.Setup();

			try
			{
				var methods = new (string Name, Action Run)[]
				{
					("Execute_Hot",   () => b.Execute_Hot()),
					("Prepare_Cold",  () => b.Prepare_Cold()),
				};

				var tag = Environment.GetEnvironmentVariable("RENDER_BENCH_TAG") ?? "(unknown)";

				Console.WriteLine();
				Console.WriteLine("=== EagerLoadingBenchmark manual run | tag: " + tag + " ===");
				Console.WriteLine();
				Console.WriteLine("| Benchmark | Mean (ms) | Median (ms) | Allocated (KB/op) |");
				Console.WriteLine("|---|---:|---:|---:|");

				foreach (var (name, run) in methods)
				{
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
					var allocKb    = Math.Max(0, bytesAfter - bytesBefore) / 1024.0 / iterations;
					var mean       = samples.Average();

					Array.Sort(samples);
					var mid    = samples.Length / 2;
					var median = samples.Length % 2 == 0
						? (samples[mid - 1] + samples[mid]) / 2.0
						: samples[mid];

					Console.WriteLine($"| {name,-13} | {mean,9:F3} | {median,11:F3} | {allocKb,17:F1} |");
				}

				Console.WriteLine();

				// Retained-memory footprint: the heap held once DistinctCount structurally-distinct eager queries are
				// executed and cached. Each caches an EagerCommandCache (PreparedScenario) that still retains its
				// statement graph, so this is the memory signal for the (not-yet-migrated) eager path. Measured as a
				// delta over the empty-cache baseline (the seeded in-memory data is a fixed baseline that cancels out).
				Query.ClearCaches();
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var baseHeap = GC.GetTotalMemory(forceFullCollection: true);

				var sink = 0;

				for (var i = 0; i < DistinctCount; i++)
					sink += b.BuildDistinct(i).ToList().Count;

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var heldHeap   = GC.GetTotalMemory(forceFullCollection: true);
				var retainedKb = Math.Max(0, heldHeap - baseHeap) / 1024.0;

				Console.WriteLine($"Retained footprint: {retainedKb,10:F1} KB for {DistinctCount} distinct cached eager queries ({retainedKb / DistinctCount:F1} KB/query) [sink={sink}]");
				Console.WriteLine();
			}
			finally
			{
				b.Cleanup();
			}
		}
	}
}
