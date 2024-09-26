using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.Types
{
	// shows small performance degradation due to indirect call
	public class WrapSetterBenchmark
	{
		private const string StringParameter  = "TestString";
		private const int    IntParameter     = 11;
		private const bool   BooleanParameter = true;

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
