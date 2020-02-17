using System.Linq;
using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// FIX: benchmark shows huge performance and memory impact due to build of expression recompilation
	public class WrapBenchmark
	{
		private static readonly string Parameter = "TestString";

		private Original.TestClass2 _originalInstance;
		private Wrapped.TestClass2 _wrapperInstance;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Original.TestClass2), typeof(Original.TestEventHandler));
			typeMapper.RegisterWrapper<Wrapped.TestClass2>();
			typeMapper.RegisterWrapper<Wrapped.TestEventHandler>();

			_originalInstance = new Original.TestClass2();
			_wrapperInstance = typeMapper.CreateAndWrap(() => new Wrapped.TestClass2());
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
