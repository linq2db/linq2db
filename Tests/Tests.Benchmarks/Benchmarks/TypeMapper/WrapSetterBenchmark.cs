using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// FIX: benchmark shows huge performance and memory impact (not yet migrated to new wrapper infra)
	public class WrapSetterBenchmark
	{
		private static readonly string StringParameter = "TestString";
		private static readonly int IntParameter = 11;
		private static readonly bool BooleanParameter = true;

		private Original.TestClass2 _originalInstance;
		private Wrapped.TestClass2 _wrapperInstance;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = Wrapped.Helper.CreateTypeMapper();

			_originalInstance = new Original.TestClass2();
			_wrapperInstance = typeMapper.CreateAndWrap(() => new Wrapped.TestClass2());
		}

		[Benchmark]
		public void TypeMapperString()
		{
			_wrapperInstance.StringProperty = StringParameter;
		}

		[Benchmark(Baseline = true)]
		public void DirectAccessString()
		{
			_originalInstance.StringProperty = StringParameter;
		}

		[Benchmark]
		public void TypeMapperInt()
		{
			_wrapperInstance.IntProperty = IntParameter;
		}

		[Benchmark]
		public void DirectAccessInt()
		{
			_originalInstance.IntProperty = IntParameter;
		}

		[Benchmark]
		public void TypeMapperBoolean()
		{
			_wrapperInstance.BooleanProperty = BooleanParameter;
		}

		[Benchmark]
		public void DirectAccessBoolean()
		{
			_originalInstance.BooleanProperty = BooleanParameter;
		}

		[Benchmark]
		public void TypeMapperWrapper()
		{
			_wrapperInstance.WrapperProperty = _wrapperInstance;
		}

		[Benchmark]
		public void DirectAccessWrapper()
		{
			_originalInstance.WrapperProperty = _originalInstance;
		}
	}
}
