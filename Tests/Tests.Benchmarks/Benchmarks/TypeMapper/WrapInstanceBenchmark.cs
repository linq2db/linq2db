using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// FIX: benchmark shows huge performance and memory impact
	public class WrapInstanceBenchmark
	{
		private Original.TestClass2 _originalInstance;
		private TypeMapper _typeMapper;

		[GlobalSetup]
		public void Setup()
		{
			_typeMapper = new TypeMapper(typeof(Original.TestClass2), typeof(Original.TestEventHandler));
			_typeMapper.RegisterWrapper<Wrapped.TestClass2>();
			_typeMapper.RegisterWrapper<Wrapped.TestEventHandler>();

			_originalInstance = new Original.TestClass2();
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapper()
		{
			return _typeMapper.Wrap<Wrapped.TestClass2>(_originalInstance);
		}

		[Benchmark(Baseline = true)]
		public Original.TestClass2 DirectAccess()
		{
			return _originalInstance;
		}
	}
}
