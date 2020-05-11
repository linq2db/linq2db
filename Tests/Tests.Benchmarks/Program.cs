using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using LinqToDB.Benchmarks.Queries;

namespace LinqToDB.Benchmarks
{
	class Program
	{
		static void Main_(string[] args)
		{
			BenchmarkSwitcher
				.FromAssembly(typeof(Program).Assembly)
				.Run(
					args.Length > 0 ? args : new [] { "--filter=*" },
					Config.Instance);
		}

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
		//static void Main_InsertSet(string[] args)
		static void Main(string[] args)
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

		static void Main_SelectBenchmark_Memory(string[] args)
		{
			var b = new SelectBenchmark();
			b.Setup();
			SelectBenchmark_WarmUp(b);
			SelectBenchmark_Measure(b);
		}

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
	}
}
