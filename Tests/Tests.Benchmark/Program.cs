using BenchmarkDotNet.Running;
using System;
using System.Diagnostics;
using Tests.Benchmarks;

namespace Tests.Benchmark
{
	class Program
	{
		static void Main(string[] args)
		{
			if (Debugger.IsAttached)
			{
				QueryTests.VisitAllBigPredicateNonRecursive();
				Console.WriteLine("Finished...");
				Console.ReadLine();
			}
			else
			{
				BenchmarkRunner.Run<QueryTests>();
				BenchmarkRunner.Run<QueryCacheCollectionsBenchmark>();
			}
		}
	}
}
