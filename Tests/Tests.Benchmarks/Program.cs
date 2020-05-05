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

		static void Main_SelectBenchmark_Memory(string[] args)
		{
			var b = new SelectBenchmark();
			b.Setup();
			SelectBenchmark_WarmUp(b);
			SelectBenchmark_Measure(b);
		}

		//static void Main_FetchIndividualBenchmark_Memory(string[] args)
		static void Main()
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
