using BenchmarkDotNet.Running;

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
	}
}
