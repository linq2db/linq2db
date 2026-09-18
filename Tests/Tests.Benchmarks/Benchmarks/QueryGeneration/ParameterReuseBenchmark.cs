using System;
using System.Linq;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.Models;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;

namespace LinqToDB.Benchmarks.QueryGeneration
{
	// Two occurrences of one expression share a SQL parameter only after registration confirms they
	// evaluate to the same value, which means running the user's expression at build time. These measure
	// what that costs across the shapes it can reach: nothing to merge, one pair that merges, many pairs
	// that merge, and a pair that disagrees so each occurrence keeps its own parameter.
	public class ParameterReuseBenchmark
	{
		sealed class Counter
		{
			public int Value;

			public int Next()
			{
				return Value++;
			}
		}

		IDataProvider _provider = null!;
		Counter       _counter  = new();

		public ParameterReuseBenchmark()
		{
			Setup();
		}

		[GlobalSetup]
		public void Setup()
		{
			_provider ??= AccessTools.GetDataProvider(provider: AccessProvider.OleDb, version: AccessVersion.Ace);
		}

		NorthwindDB GetDataConnection()
		{
			return new NorthwindDB(_provider);
		}

		/// <summary>
		/// Two distinct captured values: registration finds no structurally equal occurrence, so the
		/// equality check never runs. Baseline for what the check costs when there is nothing to merge.
		/// </summary>
		[Benchmark(Baseline = true)]
		public string NoDuplicates()
		{
			using var db = GetDataConnection();

			var first  = 1;
			var second = 2;

			return db.Order.Where(o => o.EmployeeID == first || o.ShipVia == second).ToString()!;
		}

		/// <summary>
		/// One captured value used twice: the pair is compared once and merges into a single parameter.
		/// </summary>
		[Benchmark]
		public string OneDuplicate()
		{
			using var db = GetDataConnection();

			var value = 1;

			return db.Order.Where(o => o.EmployeeID == value || o.ShipVia == value).ToString()!;
		}

		/// <summary>
		/// The same captured value across eight predicates - the shape where a per-registration scan over
		/// already-registered parameters costs the most.
		/// </summary>
		[Benchmark]
		public string ManyDuplicates()
		{
			using var db = GetDataConnection();

			var value = 1;

			return db.Order
				.Where(o => o.EmployeeID == value || o.ShipVia == value)
				.Where(o => o.EmployeeID >= value || o.ShipVia >= value)
				.Where(o => o.EmployeeID <= value || o.ShipVia <= value)
				.Where(o => o.OrderID    >= value || o.OrderID != value)
				.ToString()!;
		}

		/// <summary>
		/// Two occurrences that return a different value each time: the comparison runs, fails to prove
		/// them equal, and each occurrence keeps its own parameter. Repeated builds also exercise the
		/// cached query being rejected because the registered duplicate check no longer holds.
		/// </summary>
		[Benchmark]
		public string DivergingValues()
		{
			using var db = GetDataConnection();

			var counter = _counter;

			return db.Order.Where(o => o.EmployeeID == counter.Next() || o.ShipVia == counter.Next()).ToString()!;
		}

		// ----------------------------------------------------------------------------------
		// Manual runner - BenchmarkDotNet's child-process toolchain cannot build its generated
		// project when NuGet PackageSourceMapping is in effect, so this mirrors CacheActivityBenchmark's
		// fallback. Usage: manual-paramreuse [iterations] [warmups]
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
		public static void RunManually(int warmups = 200, int iterations = 2000)
		{
			if (warmups    < 0) warmups    = 0;
			if (iterations < 1) iterations = 1;

			var b = new ParameterReuseBenchmark();
			b.Setup();

			var methods = new (string Name, Func<string> Run)[]
			{
				("NoDuplicates",    b.NoDuplicates),
				("OneDuplicate",    b.OneDuplicate),
				("ManyDuplicates",  b.ManyDuplicates),
				("DivergingValues", b.DivergingValues),
			};

			var tag = Environment.GetEnvironmentVariable("PARAM_BENCH_TAG") ?? "(unknown)";

			Console.WriteLine();
			Console.WriteLine("=== ParameterReuseBenchmark manual run | tag: " + tag + " ===");
			Console.WriteLine();
			Console.WriteLine("| Benchmark | Mean (us) | Median (us) | Allocated (KB/op) |");
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
					samples[i] = sw.Elapsed.TotalMilliseconds * 1000.0;
				}

				var bytesAfter = GetAllocatedBytes();
				var allocKb    = Math.Max(0, bytesAfter - bytesBefore) / 1024.0 / iterations;
				var mean       = samples.Average();

				Array.Sort(samples);
				var mid    = samples.Length / 2;
				var median = samples.Length % 2 == 0
					? (samples[mid - 1] + samples[mid]) / 2.0
					: samples[mid];

				Console.WriteLine($"| {name,-16} | {mean,9:F2} | {median,11:F2} | {allocKb,17:F1} |");
			}

			Console.WriteLine();
		}
	}
}
