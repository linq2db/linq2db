using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// shows small performance degradation due to indirect call and conversion logic
	// one benchmark shows extra allocation due to boxing in edge case
	public class EnumConvertBenchmark
	{
		private Original.TestClass2 _originalInstance;
		private Wrapped.TestClass2  _wrapperInstance;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper    = Wrapped.Helper.CreateTypeMapper();

			_originalInstance = new Original.TestClass2();
			_wrapperInstance  = typeMapper.BuildWrappedFactory(() => new Wrapped.TestClass2())();
		}

		[Benchmark]
		public Wrapped.TestEnum2 TestCastConvertTypeMapper()
		{
			return _wrapperInstance.TestEnum2Convert(Wrapped.TestEnum2.Four);
		}

		[Benchmark(Baseline = true)]
		public Original.TestEnum2 TestCastConvertDirectAccess()
		{
			return _originalInstance.TestEnum2Convert(Original.TestEnum2.Three);
		}

		[Benchmark]
		public Wrapped.TestEnum TestDictionaryConvertTypeMapper()
		{
			return _wrapperInstance.TestEnumConvert(Wrapped.TestEnum.Three);
		}

		[Benchmark]
		public Original.TestEnum TestDictionaryConvertDirectAccess()
		{
			return _originalInstance.TestEnumConvert(Original.TestEnum.Three);
		}

		[Benchmark]
		public Wrapped.TestEnum TestDictionaryCastConvertTypeMapper()
		{
			return _wrapperInstance.TestEnumConvert(Wrapped.TestEnum.Four);
		}

		[Benchmark]
		public Original.TestEnum TestDictionaryCastConvertDirectAccess()
		{
			return _originalInstance.TestEnumConvert((Original.TestEnum)4);
		}

		[Benchmark]
		public Wrapped.TestEnum3 TestFlagsCastConvertTypeMapper()
		{
			return _wrapperInstance.TestEnum3Convert(Wrapped.TestEnum3.One | Wrapped.TestEnum3.Two);
		}

		[Benchmark]
		public Original.TestEnum3 TestFlagsCastConvertDirectAccess()
		{
			return _originalInstance.TestEnum3Convert(Original.TestEnum3.One | Original.TestEnum3.Two);
		}
	}
}
