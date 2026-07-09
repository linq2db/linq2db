using System;
using System.Data;
using System.Data.Common;
using System.Linq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

using LinqToDB;
using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.Linq;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Queries
{
	/// <summary>
	/// Query prepare/render pipeline benchmarks (the area reworked by the render-pipeline redesign:
	/// <c>DataConnection.QueryRunner.GetCommand</c> + the per-query render cache on <c>QueryInfo</c>).
	/// Uses only public LinqToDB APIs over a mock connection, so the same file compiles on master and on
	/// the redesign branch — diffs between two tagged runs are attributable to the pipeline change.
	/// Measures: the render hot path (cache reuse), cold prepare (cache miss + build), a parameter-value
	/// dependent query (rebuilt per call), an executed DML statement, and the retained-memory footprint of
	/// many distinct cached queries (the statement-drop memory claim).
	/// </summary>
	/// <remarks>Forces the InProcessEmit toolchain so BDN doesn't spawn child processes (AV-safe).</remarks>
	[Config(typeof(BenchmarkConfig))]
	public class RenderPipelineBenchmark
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

		[Table("Entity")]
		public sealed class Entity
		{
			[Column] public int      Id;
			[Column] public string?  Name;
			[Column] public int      Value;
			[Column] public DateTime Created;
		}

		const int Iterations   = 200;   // hot-loop repetitions (cache-hit path)
		const int DistinctCount = 48;    // number of structurally-distinct query shapes

		DbConnection   _cn      = null!;
		DataConnection _db      = null!;
		Entity         _entity  = null!;
		int[]          _inList  = null!;

		// Trigger LINQ translation + render (GetCommand) + cache lookup without executing on the mock;
		// return SQL length so the JIT can't elide the call.
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		static int Render<T>(IQueryable<T> q) => q.ToSqlQuery().Sql.Length;

		[GlobalSetup]
		public void Setup()
		{
			// One row so an executed SELECT materializes; Return so an executed INSERT reports rows-affected.
			var schema = new DataTable();
			schema.Columns.Add("AllowDBNull", typeof(bool));
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);

			var result = new QueryResult
			{
				Schema     = schema,
				Names      = new[] { "Id", "Name", "Value", "Created" },
				FieldTypes = new[] { typeof(int), typeof(string), typeof(int), typeof(DateTime) },
				DbTypes    = new[] { "int", "text", "int", "datetime" },
				Data       = new object?[][] { new object?[] { 1, "x", 42, new DateTime(2020, 1, 1) } },
				Return     = 1,
			};

			_cn     = new MockDbConnection(result, ConnectionState.Open);
			_db     = new DataConnection(new DataOptions().UseConnection(SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft), _cn));
			_entity = new Entity { Id = 1, Name = "x", Value = 42, Created = new DateTime(2020, 1, 1) };
			_inList = Enumerable.Range(0, 16).ToArray();

			Query.ClearCaches();
		}

		// === Render hot path: same non-parameter-dependent query re-rendered (cache hit) ===
		// First call builds + caches the render (QueryInfo.Prepared); the rest reuse it.
		[Benchmark]
		public int Render_Hot()
		{
			var total = 0;

			for (var i = 0; i < Iterations; i++)
				total += Render(_db.GetTable<Entity>().Where(x => x.Value > 0).OrderBy(x => x.Id));

			return total;
		}

		// === Execute hot path: same non-parameter-dependent SELECT executed (render reuse + interpreter + map) ===
		[Benchmark]
		public int Execute_Select()
		{
			var total = 0;

			for (var i = 0; i < Iterations; i++)
				total += _db.GetTable<Entity>().Where(x => x.Value > 0).OrderBy(x => x.Id).ToList().Count;

			return total;
		}

		// === Execute hot path: DML INSERT (render reuse + non-query execute) ===
		[Benchmark]
		public int Execute_Insert()
		{
			var total = 0;

			for (var i = 0; i < Iterations; i++)
				total += _db.Insert(_entity);

			return total;
		}

		// === Parameter-value-dependent query: Contains over a collection (SQL varies with values ⇒ rebuilt per call) ===
		[Benchmark]
		public int Render_ValueDependent()
		{
			var total = 0;

			for (var i = 0; i < Iterations; i++)
				total += Render(_db.GetTable<Entity>().Where(x => _inList.Contains(x.Id)));

			return total;
		}

		// === Cold prepare: DistinctCount structurally-distinct non-parameter-dependent queries (cache miss + full build each) ===
		[Benchmark]
		public int Prepare_Cold()
		{
			Query.ClearCaches();

			var total = 0;

			for (var i = 0; i < DistinctCount; i++)
				total += Render(BuildDistinct(i));

			return total;
		}

		// Builds a structurally-distinct, non-parameter-dependent query for index i (literal predicates only, so the
		// SQL is stable ⇒ the query is non-parameter-dependent and gets its own cache entry).
		IQueryable<Entity> BuildDistinct(int i)
		{
			IQueryable<Entity> q = _db.GetTable<Entity>();

			if ((i & 1) != 0) q = q.Where(x => x.Value > 0);
			if ((i & 2) != 0) q = q.Where(x => x.Name != null);
			if ((i & 4) != 0) q = q.Where(x => x.Id   > 0);
			if ((i & 8) != 0) q = q.OrderBy(x => x.Id);

			return ((i >> 4) % 3) switch
			{
				0 => q.Where(x => x.Created > new DateTime(2000, 1, 1)),
				1 => q.Where(x => x.Name    != "z").OrderByDescending(x => x.Value),
				_ => q.Where(x => x.Value   < 1000).OrderBy(x => x.Name),
			};
		}

		// ----------------------------------------------------------------------------------
		// Manual runner — for when BDN's child-process toolchain is blocked by antivirus.
		// Emits a markdown table (Mean/Median/Allocated per op) tagged via RENDER_BENCH_TAG so
		// two runs (branch vs master) can be diffed. Mirrors CacheActivityBenchmark.RunManually.
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

			var b = new RenderPipelineBenchmark();
			b.Setup();

			var methods = new (string Name, Action Run)[]
			{
				("Render_Hot",            () => b.Render_Hot()),
				("Execute_Select",        () => b.Execute_Select()),
				("Execute_Insert",        () => b.Execute_Insert()),
				("Render_ValueDependent", () => b.Render_ValueDependent()),
				("Prepare_Cold",          () => b.Prepare_Cold()),
			};

			var tag = Environment.GetEnvironmentVariable("RENDER_BENCH_TAG") ?? "(unknown)";

			Console.WriteLine();
			Console.WriteLine("=== RenderPipelineBenchmark manual run | tag: " + tag + " ===");
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

				Console.WriteLine($"| {name,-21} | {mean,9:F3} | {median,11:F3} | {allocKb,17:F1} |");
			}

			Console.WriteLine();

			// Retained-memory footprint: the heap held once DistinctCount structurally-distinct,
			// non-parameter-dependent queries are cached. Measured as a delta over the empty-cache
			// baseline, so it isolates what the render cache retains per query. This is the memory
			// signal for the statement-drop claim — compare between the branch and master tags.
			Query.ClearCaches();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var baseHeap = GC.GetTotalMemory(forceFullCollection: true);

			var sink = 0;

			for (var i = 0; i < DistinctCount; i++)
				sink += Render(b.BuildDistinct(i));

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var heldHeap   = GC.GetTotalMemory(forceFullCollection: true);
			var retainedKb = Math.Max(0, heldHeap - baseHeap) / 1024.0;

			Console.WriteLine($"Retained footprint: {retainedKb,10:F1} KB for {DistinctCount} distinct cached queries ({retainedKb / DistinctCount:F1} KB/query) [sink={sink}]");
			Console.WriteLine();
		}
	}
}
