using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.Types
{
	// shows small performance degradation due to indirect call and conversion logic
	// one benchmark shows extra allocation due to boxing in edge case
	public class EnumConvertBenchmark
	{
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
		public TestClasses.Wrapped.TestEnum2 TestCastConvertTypeMapper()
		{
			return _wrapperInstance.TestEnum2Convert(TestClasses.Wrapped.TestEnum2.Four);
		}

		[Benchmark(Baseline = true)]
		public TestClasses.Original.TestEnum2 TestCastConvertDirectAccess()
		{
			return _originalInstance.TestEnum2Convert(TestClasses.Original.TestEnum2.Three);
		}

		[Benchmark]
		public TestClasses.Wrapped.TestEnum TestDictionaryConvertTypeMapper()
		{
			return _wrapperInstance.TestEnumConvert(TestClasses.Wrapped.TestEnum.Three);
		}

		[Benchmark]
		public TestClasses.Original.TestEnum TestDictionaryConvertDirectAccess()
		{
			return _originalInstance.TestEnumConvert(TestClasses.Original.TestEnum.Three);
		}

		[Benchmark]
		public TestClasses.Wrapped.TestEnum TestDictionaryCastConvertTypeMapper()
		{
			return _wrapperInstance.TestEnumConvert(TestClasses.Wrapped.TestEnum.Four);
		}

		[Benchmark]
		public TestClasses.Original.TestEnum TestDictionaryCastConvertDirectAccess()
		{
			return _originalInstance.TestEnumConvert((TestClasses.Original.TestEnum)4);
		}

		[Benchmark]
		public TestClasses.Wrapped.TestEnum3 TestFlagsCastConvertTypeMapper()
		{
			return _wrapperInstance.TestEnum3Convert(TestClasses.Wrapped.TestEnum3.One | TestClasses.Wrapped.TestEnum3.Two);
		}

		[Benchmark]
		public TestClasses.Original.TestEnum3 TestFlagsCastConvertDirectAccess()
		{
			return _originalInstance.TestEnum3Convert(TestClasses.Original.TestEnum3.One | TestClasses.Original.TestEnum3.Two);
		}
	}
}
