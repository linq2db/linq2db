using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

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
				var summary = BenchmarkRunner.Run<QueryTests>();
			}
		}
	}
}
