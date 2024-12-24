using System;

using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// shows small performance degradation due to indirect call
	public class BuildActionBenchmark
	{
		private Action _action = null!;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = Wrapped.Helper.CreateTypeMapper();

			_action = typeMapper.BuildAction(typeMapper.MapActionLambda(() => Wrapped.TestClass.ClearAllPools()));
		}

		[Benchmark]
		public void BuildAction()
		{
			_action();
		}

		[Benchmark(Baseline = true)]
		public void DirectAccess()
		{
			Original.TestClass.ClearAllPools();
		}
	}
}
