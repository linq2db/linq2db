using BenchmarkDotNet.Attributes;

using LinqToDB.Expressions.Types;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// shows reasonable performance degradation and allcation due to wrapper instance creation
	public class WrapInstanceBenchmark
	{
		private Original.TestClass2 _originalInstance = null!;
		private TypeMapper          _typeMapper = null!;

		[GlobalSetup]
		public void Setup()
		{
			_typeMapper       = Wrapped.Helper.CreateTypeMapper();
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
