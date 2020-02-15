using System;
using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// Notes:
	// benchmark shows expected performance degradation due to indirect call
	public class BuildActionBenchmark
	{
		private Action _action;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Original.TestClass));
			typeMapper.RegisterWrapper<Wrapped.TestClass>();

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
