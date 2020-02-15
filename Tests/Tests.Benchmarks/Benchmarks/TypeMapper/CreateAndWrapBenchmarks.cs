using System;
using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// Notes:
	// benchmark shows huge performance and memory impact due to build of constructor expression on each call
	public class CreateAndWrapBenchmarks
	{
		private static readonly string Parameter = "TestString";

		private Func<string, Wrapped.TestClass2> _factory;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Original.TestClass2));
			typeMapper.RegisterWrapper<Wrapped.TestClass2>();

			_factory = (string connectionString) => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2(connectionString));
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapper()
		{
			return _factory(Parameter);
		}

		[Benchmark(Baseline = true)]
		public Original.TestClass2 DirectAccess()
		{
			return new Original.TestClass2(Parameter);
		}
	}
}
