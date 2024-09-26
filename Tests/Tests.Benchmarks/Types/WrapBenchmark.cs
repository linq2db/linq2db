using System.Linq;
using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.Types
{
	// shows small performance degradation due to indirect call and allocation for wrapper instance
	// creation in one benchmark
	public class WrapBenchmark
	{
		private const string Parameter = "TestString";

		private TestClasses.Original.TestClass2 _originalInstance = null!;
		private TestClasses.Wrapped.TestClass2  _wrapperInstance = null!;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper    = TestClasses.Wrapped.Helper.CreateTypeMapper();

			_originalInstance = new TestClasses.Original.TestClass2();
			_wrapperInstance  = typeMapper.BuildWrappedFactory(() => new TestClasses.Wrapped.TestClass2())();
		}

		[Benchmark]
		public string TypeMapperString()
		{
			return _wrapperInstance.QuoteIdentifier(Parameter);
		}

		[Benchmark(Baseline = true)]
		public string DirectAccessString()
		{
			return _originalInstance.QuoteIdentifier(Parameter);
		}

		[Benchmark]
		public TestClasses.Wrapped.TestClass2 TypeMapperWrappedInstance()
		{
			return _wrapperInstance.Add(_wrapperInstance);
		}

		[Benchmark]
		public TestClasses.Original.TestClass2 DirectAccessWrappedInstance()
		{
			return _originalInstance.Add(_originalInstance);
		}

		[Benchmark]
		public object[] TypeMapperGetEnumerator()
		{
			return _wrapperInstance.GetEnumerator().Cast<object>().ToArray();
		}

		[Benchmark]
		public object[] DirectAccessGetEnumerator()
		{
			return _originalInstance.GetEnumerator().Cast<object>().ToArray();
		}
	}
}
