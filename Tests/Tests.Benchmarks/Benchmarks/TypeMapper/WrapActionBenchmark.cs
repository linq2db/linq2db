using System;
using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// Notes:
	// benchmark shows huge performance and memory impact due to build of call expression on each call
	// TODO: after fix, add benchmark to test that wrapper reused between instances
	public class WrapActionBenchmark
	{
		private static readonly string Parameter = "TestString";

		private Original.TestClass2 _originalInstance;
		private Wrapped.TestClass2 _wrapperInstance;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Original.TestClass2));
			typeMapper.RegisterWrapper<Wrapped.TestClass2>();

			_originalInstance = new Original.TestClass2(Parameter);
			// TODO: FIXME: direct call crashes right now
			//_wrapperInstance = typeMapper.CreateAndWrap(() => new Wrapped.TestClass2(Parameter));
			_wrapperInstance = ((Func<string, Wrapped.TestClass2>)((string connectionString) => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2(connectionString))))(Parameter);
		}

		[Benchmark]
		public void TypeMapper()
		{
			_wrapperInstance.CreateDatabase();
		}

		[Benchmark(Baseline = true)]
		public void DirectAccess()
		{
			_originalInstance.CreateDatabase();
		}
	}
}
