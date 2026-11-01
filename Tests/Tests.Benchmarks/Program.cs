using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BenchmarkDotNet.Running;

using LinqToDB.Benchmarks.Queries;
using LinqToDB.Benchmarks.QueryGeneration;

#if JETBRAINS
using JetBrains.Profiler.Api;
#endif

namespace LinqToDB.Benchmarks
{
	sealed class Program
	{
		internal static readonly string[] _defaultArguments = new[] { "--filter", "*.Queries.*", "*.QueryGeneration.*" };

		// Manual runners: they bypass BDN's child-process toolchain, which antivirus / endpoint protection can block.
		// Usage: <verb> [iterations] [warmups]; manual-deepjoin also takes [onlyDepth]. manual-render and manual-eager
		// tag their output with the RENDER_BENCH_TAG env var so branch and master runs can be diffed.
		static readonly Dictionary<string, Action<string[]>> _manualRunners = new(StringComparer.Ordinal)
		{
			["manual-cache"]      = a => CacheActivityBenchmark.RunManually (Arg(a, 2, 5),   Arg(a, 1, 20)),
			["manual-paramreuse"] = a => ParameterReuseBenchmark.RunManually(Arg(a, 2, 200), Arg(a, 1, 2000)),
			["manual-render"]     = a => RenderPipelineBenchmark.RunManually(Arg(a, 2, 5),   Arg(a, 1, 20)),
			["manual-eager"]      = a => EagerLoadingBenchmark.RunManually  (Arg(a, 2, 5),   Arg(a, 1, 20)),
			["manual-weakjoin"]   = a => WeakJoinScanBenchmark.RunManually  (Arg(a, 2, 2),   Arg(a, 1, 8)),
			["manual-deepjoin"]   = a => DeepJoinChainBenchmark.RunManually (Arg(a, 2, 1),   Arg(a, 1, 3), Arg(a, 3, 0)),
		};

		static int Arg(string[] args, int index, int defaultValue)
			=> args.Length > index && int.TryParse(args[index], out var value) ? value : defaultValue;

		static void Main(string[] args)
		{
			if (args.Length > 0 && _manualRunners.TryGetValue(args[0], out var run))
			{
				run(args);
				return;
			}

			// Usage: bdn <BenchmarkSwitcher args>, e.g. bdn --filter *EagerLoading*. Runs with each class's own [Config]
			// rather than Config.Instance, so the classes that force InProcessEmit spawn no child processes.
			if (args.Length > 0 && args[0] == "bdn")
			{
				BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args.Skip(1).ToArray());
				return;
			}

			//if (args.Length == 0)
			//{
			//	//	var b1 = new FetchGraphBenchmark();
			//	//	var b2 = new FetchIndividualBenchmark();
			//	//	var b3 = new FetchSetBenchmark();
			//	//	var b4 = new InsertSetBenchmark();
			//	//	var b5 = new SelectBenchmark();
			//	//	var b6 = new UpdateBenchmark();
			//	//	var b7 = new QueryGenerationBenchmark();
			//	var b8 = new Issue3253Benchmark();

			//	//	b1.Setup();
			//	//	b2.Setup();
			//	//	b3.Setup();
			//	//	b4.Setup();
			//	//	b5.Setup();
			//	//	b6.Setup();
			//	//	b7.Setup();
			//	b8.Setup();

			//	for (var i = 0; i < 10; i++)
			//	{
			//		//		b1.Compiled();
			//		//		b1.Linq();

			//		//		b2.Compiled();
			//		//		b2.Linq();

			//		//		b3.Compiled();
			//		//		b3.Linq();

			//		//		b4.Test();

			//		//		b5.Compiled();
			//		//		b5.Execute();
			//		//		b5.FromSql_Formattable();
			//		//		b5.FromSql_Interpolation();
			//		//		b5.Linq();
			//		//		b5.Query();

			//		//		b6.CompiledLinqObject();
			//		//		b6.CompiledLinqSet();
			//		//		b6.LinqObject();
			//		//		b6.LinqSet();
			//		//		b6.Object();

			//		//		b7.VwSalesByCategoryContains();
			//		//		b7.VwSalesByYear();
			//		//		b7.VwSalesByYearMutation();

			//		b8.Small_UpdateStatement_With_Variable_Parameters();
			//		//b8.Small_UpdateStatement_With_Variable_Parameters_Async();
			//		b8.Small_UpdateStatement_With_Static_Parameters();
			//		//b8.Small_UpdateStatement_With_Static_Parameters_Async();
			//		b8.Large_UpdateStatement_With_Variable_Parameters();
			//		//b8.Large_UpdateStatement_With_Variable_Parameters_Async();
			//		b8.Large_UpdateStatement_With_Static_Parameters();
			//		//b8.Large_UpdateStatement_With_Static_Parameters_Async();
			//		b8.Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches();
			//		//b8.Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async();

			//		b8.Small_InsertStatement_With_Variable_Parameters();
			//		//b8.Small_InsertStatement_With_Variable_Parameters_Async();
			//		b8.Small_InsertStatement_With_Static_Parameters();
			//		//b8.Small_InsertStatement_With_Static_Parameters_Async();
			//		b8.Large_InsertStatement_With_Variable_Parameters();
			//		//b8.Large_InsertStatement_With_Variable_Parameters_Async();
			//		b8.Large_InsertStatement_With_Static_Parameters();
			//		//b8.Large_InsertStatement_With_Static_Parameters_Async();
			//		b8.Large_InsertStatement_With_Variable_Parameters_With_ClearCaches();
			//		//b8.Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async();
			//		b8.Large_Compiled_InsertStatement_With_Variable_Parameters();
			//		b8.Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload();
			//		//b8.Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async();
			//	}

			//	return;
			//}
			//TestConcurrent();
			//VwSalesByCategoryContainsPerf();
			//return;

			BenchmarkSwitcher
				.FromAssembly(typeof(Program).Assembly)
				.Run(
					//args.Length > 0 ? args : new[] { "--filter=*" },
					// don't run TypeMapper benchmarks by default as they hardly will change, but add a lot of running time
					args.Length > 0 ? args : _defaultArguments,
					Config.Instance);
		}

		#region Concurrent
		static void TestConcurrent()
		{
			var benchmark = new ConcurrentBenchmark();
			benchmark.Setup();
			benchmark.Linq();
			benchmark.Compiled();
		}
		#endregion
		#region QueryGeneration

		static void TestVwSalesByYear()
		{
			var benchmark = new QueryGenerationBenchmark();

			for (int i = 0; i < 100000; i++)
			{
				benchmark.VwSalesByYear();
			}
		}

		static void VwSalesByCategoryContainsPerf()
		{
			var benchmark = new QueryGenerationBenchmark();

#if JETBRAINS
			MeasureProfiler.StartCollectingData();
#endif
			benchmark.VwSalesByCategoryContains();
			for (int i = 0; i < 100; i++)
			{
				benchmark.VwSalesByCategoryContains();
			}
#if JETBRAINS
			MeasureProfiler.StopCollectingData();
			//			MeasureProfiler.StopCollectingData();
			MeasureProfiler.SaveData();
#endif
		}

		static void VwSalesByCategoryContainsMem()
		{
			var benchmark = new QueryGenerationBenchmark();

#if JETBRAINS
			MemoryProfiler.CollectAllocations(true);
#endif
			for (int c = 0; c < 5; c++)
			{
				for (int i = 0; i < 1000; i++)
				{
					benchmark.VwSalesByCategoryContains();
				}
#if JETBRAINS
				MemoryProfiler.GetSnapshot();
#endif
			}
		}

		#endregion

		#region InsertSet
		static async Task Main_FetchGraph(string[] args)
		//static async Task Main(string[] args)
		{
			var b = new FetchGraphBenchmark();
			b.Setup();
			await FetchGraph_WarmUp(b);
			await FetchGraph_Measure(b);
			b.Cleanup();
		}

		private static async Task FetchGraph_WarmUp(FetchGraphBenchmark b)
		{
			for (var i = 0; i < 100; i++)
			{
				b.Linq();
				await b.LinqAsync();
				b.Compiled();
				await b.CompiledAsync();
			}
		}

		private static async Task FetchGraph_Measure(FetchGraphBenchmark b)
		{
			b.Linq();
			await b.LinqAsync();
			b.Compiled();
			await b.CompiledAsync();
		}
		#endregion

		#region InsertSet
		static void Main_InsertSet(string[] args)
		//static void Main(string[] args)
		{
			var b = new InsertSetBenchmark();
			b.Setup();
			InsertSet_WarmUp(b);
			InsertSet_Measure(b);
		}

		private static void InsertSet_WarmUp(InsertSetBenchmark b)
		{
			for (var i = 0; i < 100; i++)
			{
				b.Test();
			}
		}

		private static void InsertSet_Measure(InsertSetBenchmark b)
		{
			b.Test();
		}
		#endregion

		#region FetchSet
		static void Main_FetchSetBenchmark_Memory(string[] args)
		//static void Main()
		{
			var b = new FetchSetBenchmark();
			b.Setup();
			FetchSetBenchmark_WarmUp(b);
			FetchSetBenchmark_Measure(b);
		}

		private static void FetchSetBenchmark_WarmUp(FetchSetBenchmark b)
		{
			for (var i = 0; i < 100; i++)
			{
				b.Linq();
				b.Compiled();
				b.RawAdoNet();
			}
		}

		private static void FetchSetBenchmark_Measure(FetchSetBenchmark b)
		{
			b.Linq();
			b.Compiled();
			b.RawAdoNet();
		}
		#endregion

		#region FetchIndividual
		static void Main_FetchIndividualBenchmark_Memory(string[] args)
		//static void Main()
		{
			var b = new FetchIndividualBenchmark();
			b.Setup();
			FetchIndividualBenchmark_WarmUp(b);
			FetchIndividualBenchmark_Measure(b);
		}

		private static void FetchIndividualBenchmark_WarmUp(FetchIndividualBenchmark b)
		{
			for (var i = 0; i < 100; i++)
			{
				b.Linq();
				b.Compiled();
				b.RawAdoNet();
			}
		}

		private static void FetchIndividualBenchmark_Measure(FetchIndividualBenchmark b)
		{
			b.Linq();
			b.Compiled();
			b.RawAdoNet();
		}
		#endregion

		#region Select
		static void Main_SelectBenchmark_Memory(string[] args)
		{
			var b = new SelectBenchmark();
			b.Setup();
			SelectBenchmark_WarmUp(b);
			SelectBenchmark_Measure(b);
		}

		private static void SelectBenchmark_WarmUp(SelectBenchmark b)
		{
			for (var i = 0; i < 100; i++)
			{
				b.Linq();
				b.Compiled();
				b.FromSql_Interpolation();
				b.FromSql_Formattable();
				b.Query();
				b.Execute();
				b.RawAdoNet();
			}
		}

		private static void SelectBenchmark_Measure(SelectBenchmark b)
		{
			b.Linq();
			b.Compiled();
			b.FromSql_Interpolation();
			b.FromSql_Formattable();
			b.Query();
			b.Execute();
			b.RawAdoNet();
		}
		#endregion
	}
}
