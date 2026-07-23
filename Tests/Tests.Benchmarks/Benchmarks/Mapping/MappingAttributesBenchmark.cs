using System;
using System.Linq;
using System.Reflection;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

using LinqToDB.Mapping;
using LinqToDB.Metadata;

namespace LinqToDB.Benchmarks.Mapping
{
	/// <summary>
	/// MappingAttributesCache lookup cost + allocation profile, via the public <see cref="AttributeReader"/>.
	/// Uses only public APIs so the same file compiles on master and on the cache branch — diffs between
	/// runs are attributable to the #5692 negative-caching change.
	/// </summary>
	/// <remarks>
	/// The <see cref="EmptyLookup"/> / <see cref="ManyDistinctEmptyLookups"/> benchmarks are the #5692-relevant
	/// ones: negative (empty) results are no longer cached, so each lookup recomputes. <see cref="MemoryDiagnoser"/>
	/// quantifies both the ns/op and the allocation cost of that recompute (vs. the retained-forever growth the
	/// old code traded it for). Forces InProcessEmit so BDN doesn't spawn child processes (blocked by some AV).
	/// </remarks>
	[Config(typeof(BenchmarkConfig))]
	public class MappingAttributesBenchmark
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
			}
		}

		[Table]
		public sealed class Entity
		{
			[Column] public int     Id   { get; set; } // attributed member -> positive lookup
			public          string? Note { get; set; } // unmapped member   -> negative (empty) lookup
		}

		AttributeReader _reader      = null!;
		Type            _entityType  = null!;
		MemberInfo      _attributed  = null!;
		MemberInfo      _empty       = null!;
		MemberInfo[]    _bclMembers  = null!;

		[GlobalSetup]
		public void Setup()
		{
			_reader     = new AttributeReader();
			_entityType = typeof(Entity);
			_attributed = _entityType.GetProperty(nameof(Entity.Id))!;
			_empty      = _entityType.GetProperty(nameof(Entity.Note))!;

			_bclMembers = typeof(System.Uri).GetProperties()
				.Concat(typeof(System.Text.StringBuilder).GetProperties())
				.Concat(typeof(System.Version).GetProperties())
				.ToArray();

			// Warm the underlying AttributesExtensions layer so we measure MappingAttributesCache behavior,
			// not first-touch reflection.
			_ = _reader.GetAttributes(_entityType, _attributed);
			_ = _reader.GetAttributes(_entityType, _empty);
		}

		/// <summary>Repeated lookup of an unmapped member — post-#5692 this recomputes (not cached).</summary>
		[Benchmark]
		public int EmptyLookup() => _reader.GetAttributes(_entityType, _empty).Length;

		/// <summary>Repeated lookup of an attributed member — a cached hit.</summary>
		[Benchmark(Baseline = true)]
		public int AttributedLookup() => _reader.GetAttributes(_entityType, _attributed).Length;

		/// <summary>
		/// Many distinct unmapped members — the #5692 growth scenario. Post-fix these are recomputed and
		/// NOT retained (bounded); the allocation total is the recompute cost, not permanent growth.
		/// </summary>
		[Benchmark]
		public int ManyDistinctEmptyLookups()
		{
			var total = 0;

			foreach (var m in _bclMembers)
				total += _reader.GetAttributes(m.DeclaringType!, m).Length;

			return total;
		}

		// ----------------------------------------------------------------------------------
		// Manual runner — for when BDN's child-process toolchain is blocked. Stopwatch loop +
		// GC.GetTotalAllocatedBytes for a coarse alloc figure; less rigorous than BDN.
		// ----------------------------------------------------------------------------------
		[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030", Justification = "Benchmark output requires Console")]
		public static void RunManually(int warmups = 5, int iterations = 20)
		{
			if (warmups    < 0) warmups    = 0;
			if (iterations < 1) iterations = 1;

			var b = new MappingAttributesBenchmark();
			b.Setup();

			var methods = new (string Name, Func<int> Run)[]
			{
				("EmptyLookup",              b.EmptyLookup),
				("AttributedLookup",         b.AttributedLookup),
				("ManyDistinctEmptyLookups", b.ManyDistinctEmptyLookups),
			};

			Console.WriteLine();
			Console.WriteLine("=== MappingAttributesBenchmark manual run ===");
			Console.WriteLine();
			Console.WriteLine("| Benchmark | Mean (us) | Allocated (B/op) |");
			Console.WriteLine("|---|---:|---:|");

			foreach (var (name, run) in methods)
			{
				for (var i = 0; i < warmups; i++)
					run();

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

#if NETCOREAPP3_0_OR_GREATER
				var bytesBefore = GC.GetTotalAllocatedBytes(precise: true);
#else
				var bytesBefore = GC.GetTotalMemory(forceFullCollection: false);
#endif
				var sw = System.Diagnostics.Stopwatch.StartNew();

				for (var i = 0; i < iterations; i++)
					run();

				sw.Stop();

#if NETCOREAPP3_0_OR_GREATER
				var bytesAfter = GC.GetTotalAllocatedBytes(precise: true);
#else
				var bytesAfter = GC.GetTotalMemory(forceFullCollection: false);
#endif
				var us    = sw.Elapsed.TotalMilliseconds * 1000.0 / iterations;
				var alloc = Math.Max(0, bytesAfter - bytesBefore) / (double)iterations;

				Console.WriteLine($"| {name,-25} | {us,9:F2} | {alloc,16:F0} |");
			}

			Console.WriteLine();
		}
	}
}
