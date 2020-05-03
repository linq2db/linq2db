using BenchmarkDotNet.Running;
using LinqToDB.Benchmarks.Queries;

namespace LinqToDB.Benchmarks
{
	class Program
	{
		static void Main(string[] args)
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
			WarmUp(b);
			Measure(b);
		}

		private static void WarmUp(SelectBenchmark b)
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

		private static void Measure(SelectBenchmark b)
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
