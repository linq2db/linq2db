using System.Linq;

using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// shows small performance degradation due to indirect call and allocation for wrapper instance
	// creation in one benchmark
	public class WrapBenchmark
	{
		private const string Parameter = "TestString";

		private Original.TestClass2 _originalInstance = null!;
		private Wrapped.TestClass2  _wrapperInstance = null!;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper    = Wrapped.Helper.CreateTypeMapper();

			_originalInstance = new Original.TestClass2();
			_wrapperInstance  = typeMapper.BuildWrappedFactory(() => new Wrapped.TestClass2())();
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
		public Wrapped.TestClass2 TypeMapperWrappedInstance()
		{
			return _wrapperInstance.Add(_wrapperInstance);
		}

		[Benchmark]
		public Original.TestClass2 DirectAccessWrappedInstance()
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
