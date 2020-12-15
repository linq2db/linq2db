using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using LinqToDB.Benchmarks.Benchmarks.QueryGeneration;
using LinqToDB.Benchmarks.Queries;
using LinqToDB.Expressions;
using LinqToDB.Extensions;

#if JETBRAINS
using JetBrains.Profiler.Api;
#endif

namespace LinqToDB.Benchmarks
{
	class Program
	{
		static void Main(string[] args)
		{

			/*
			VwSalesByCategoryContainsPerf();
			return;
			*/

			BenchmarkSwitcher
				.FromAssembly(typeof(Program).Assembly)
				.Run(
					args.Length > 0 ? args : new [] { "--filter=*" },
					Config.Instance);
		}

#region QueryGeneration

		static void TestVwSalesByYear()
		{
			var benchmark = new QueryGenerationBenchmark();
			benchmark.DataProvider = ProviderName.MySqlConnector;

			for (int i = 0; i < 100000; i++)
			{
				benchmark.VwSalesByYear();
			}
		}

		static void VwSalesByCategoryContainsPerf()
		{
			var benchmark = new QueryGenerationBenchmark();
			benchmark.DataProvider = ProviderName.Access;

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
			benchmark.DataProvider = ProviderName.Access;

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
