using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;
using LinqToDB.Expressions.Types;

namespace LinqToDB.Benchmarks.Types
{
	// shows reasonable performance degradation and allcation due to wrapper instance creation
	public class WrapInstanceBenchmark
	{
		private TestClasses.Original.TestClass2 _originalInstance = null!;
		private TypeMapper          _typeMapper = null!;

		[GlobalSetup]
		public void Setup()
		{
			_typeMapper       = TestClasses.Wrapped.Helper.CreateTypeMapper();
			_originalInstance = new TestClasses.Original.TestClass2();
		}

		[Benchmark]
		public TestClasses.Wrapped.TestClass2 TypeMapper()
		{
			return _typeMapper.Wrap<TestClasses.Wrapped.TestClass2>(_originalInstance);
		}

		[Benchmark(Baseline = true)]
		public TestClasses.Original.TestClass2 DirectAccess()
		{
			return _originalInstance;
		}
	}
}
