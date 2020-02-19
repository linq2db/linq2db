using System;
using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// FIX: benchmark shows huge performance and memory impact due to current events implmentation
	public class CreateAndWrapBenchmark
	{
		private static readonly int IntParameter = -1;
		private static readonly string StringParameter = "TestString";
		private static readonly TimeSpan TimeSpanParameter = TimeSpan.FromMinutes(5);
		private static readonly DateTimeOffset DateTimeOffsetParameter = DateTimeOffset.Now;

		private readonly Original.TestClass _testClassInstance = new Original.TestClass();
		private readonly Original.TestClass2 _testClass2Instance = new Original.TestClass2();

		private Func<Wrapped.TestClass2> _factoryParameterless;
		private Func<string, Wrapped.TestClass2> _factoryOneParameterString;
		private Func<TimeSpan, object> _factoryOneParameterTimeSpanInstance;
		private Func<int, string, Wrapped.TestClass2> _factoryTwoParametersIntString;
		private Func<string, string, Wrapped.TestClass2> _factoryTwoParametersStringString;
		private Func<ITestClass2, Wrapped.TestEnum, Wrapped.TestClass2> _factoryThoParametersWrapperEnum;
		private Func<ITestClass2, string, Wrapped.TestClass2> _factoryThoParametersWrapperString;
		private Func<ITestClass2, Wrapped.TestEnum, ITestClass, Wrapped.TestClass2> _factoryThreeParameters;

		private Func<DateTimeOffset, string, object> _tstsFactory;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = Wrapped.Helper.CreateTypeMapper();

			_factoryParameterless = () => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2());
			_factoryOneParameterString = (string connectionString) => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2(connectionString));
			_factoryOneParameterTimeSpanInstance = (TimeSpan timeSpan) => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2(timeSpan)).instance_;
			_factoryTwoParametersIntString = (int src, string dest) => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2(src, dest));
			_factoryTwoParametersStringString = (string src, string dest) => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2(src, dest));
			_factoryThoParametersWrapperEnum = (ITestClass2 p1, Wrapped.TestEnum p2) => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2((Wrapped.TestClass2)p1, p2));
			_factoryThoParametersWrapperString = (ITestClass2 p1, string p2) => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2((Wrapped.TestClass2)p1, p2));
			_factoryThreeParameters = (ITestClass2 p1, Wrapped.TestEnum p2, ITestClass p3) => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2((Wrapped.TestClass2)p1, p2, (Wrapped.TestClass)p3));

			_tstsFactory = (DateTimeOffset dto, string offset)
				=> typeMapper.CreateAndWrap(() => new Wrapped.TestClass2(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, GetDateTimeOffsetNanoseconds(dto), offset)).instance_;
		}

		private const int NanosecondsPerTick = 100;
		private static int GetDateTimeOffsetNanoseconds(DateTimeOffset value)
		{
			var tmp = new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);

			return Convert.ToInt32((value.Ticks - tmp.Ticks) * NanosecondsPerTick);
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapperParameterless()
		{
			return _factoryParameterless();
		}

		[Benchmark(Baseline = true)]
		public Original.TestClass2 DirectAccessParameterless()
		{
			return new Original.TestClass2();
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapperOneParameterString()
		{
			return _factoryOneParameterString(StringParameter);
		}

		[Benchmark]
		public Original.TestClass2 DirectAccessOneParameterString()
		{
			return new Original.TestClass2(StringParameter);
		}

		[Benchmark]
		public object TypeMapperOneParameterTimeSpanUnwrap()
		{
			return _factoryOneParameterTimeSpanInstance(TimeSpanParameter);
		}

		[Benchmark]
		public object DirectAccessOneParameterTimeSpanUnwrap()
		{
			return new Original.TestClass2(TimeSpanParameter);
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapperTwoParametersIntString()
		{
			return _factoryTwoParametersIntString(IntParameter, StringParameter);
		}

		[Benchmark]
		public Original.TestClass2 DirectAccessTwoParametersIntString()
		{
			return new Original.TestClass2(IntParameter, StringParameter);
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapperTwoParametersStringString()
		{
			return _factoryTwoParametersStringString(StringParameter, StringParameter);
		}

		[Benchmark]
		public Original.TestClass2 DirectAccessTwoParametersStringString()
		{
			return new Original.TestClass2(StringParameter, StringParameter);
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapperTwoParametersWrapperEnum()
		{
			return _factoryThoParametersWrapperEnum(_testClass2Instance, Wrapped.TestEnum.Three);
		}

		[Benchmark]
		public Original.TestClass2 DirectAccessTwoParametersWrapperEnum()
		{
			return new Original.TestClass2(_testClass2Instance, Original.TestEnum.Three);
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapperTwoParametersWrapperString()
		{
			return _factoryThoParametersWrapperString(_testClass2Instance, StringParameter);
		}

		[Benchmark]
		public Original.TestClass2 DirectAccessTwoParametersWrapperString()
		{
			return new Original.TestClass2(_testClass2Instance, StringParameter);
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapperThreeParameters()
		{
			return _factoryThreeParameters(_testClass2Instance, Wrapped.TestEnum.Three, _testClassInstance);
		}

		[Benchmark]
		public Original.TestClass2 DirectAccessThreeParameters()
		{
			return new Original.TestClass2(_testClass2Instance, Original.TestEnum.Three, _testClassInstance);
		}

		[Benchmark]
		public object TypeMapperTSTZFactory()
		{
			return _tstsFactory(DateTimeOffsetParameter, StringParameter);
		}

		[Benchmark]
		public object DirectAccessTSTZFactory()
		{
			return new Original.TestClass2(DateTimeOffsetParameter.Year, DateTimeOffsetParameter.Month, DateTimeOffsetParameter.Day, DateTimeOffsetParameter.Hour, DateTimeOffsetParameter.Minute, DateTimeOffsetParameter.Second, GetDateTimeOffsetNanoseconds(DateTimeOffsetParameter), StringParameter);
		}
	}
}
