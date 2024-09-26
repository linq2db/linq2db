using System;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.TestClasses;

namespace LinqToDB.Benchmarks.Types
{
	// shows small performance degradation due to indirect call
	// benchmaerks with result wrapping also show additional allocation for wrapper instance
	public class CreateAndWrapBenchmark
	{
		private const           int            IntParameter            = -1;
		private const           string         StringParameter         = "TestString";
		private static readonly TimeSpan       TimeSpanParameter       = TimeSpan.FromMinutes(5);
		private static readonly DateTimeOffset DateTimeOffsetParameter = DateTimeOffset.Now;
		private const           int            NanosecondsPerTick      = 100;

		private readonly TestClasses.Original.TestClass _testClassInstance   = new TestClasses.Original.TestClass();
		private readonly TestClasses.Original.TestClass2 _testClass2Instance = new TestClasses.Original.TestClass2();

		private Func<TestClasses.Wrapped.TestClass2                                           > _factoryParameterless = null!;
		private Func<string, TestClasses.Wrapped.TestClass2                                   > _factoryOneParameterString = null!;
		private Func<TimeSpan, object                                             > _factoryOneParameterTimeSpanInstance = null!;
		private Func<int, string, TestClasses.Wrapped.TestClass2                              > _factoryTwoParametersIntString = null!;
		private Func<string, string, TestClasses.Wrapped.TestClass2                           > _factoryTwoParametersStringString = null!;
		private Func<ITestClass2, TestClasses.Wrapped.TestEnum, TestClasses.Wrapped.TestClass2            > _factoryThoParametersWrapperEnum = null!;
		private Func<ITestClass2, string, TestClasses.Wrapped.TestClass2                      > _factoryThoParametersWrapperString = null!;
		private Func<ITestClass2, TestClasses.Wrapped.TestEnum, ITestClass, TestClasses.Wrapped.TestClass2> _factoryThreeParameters = null!;
		private Func<DateTimeOffset, string, object                               > _tstsFactory = null!;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = TestClasses.Wrapped.Helper.CreateTypeMapper();

			_factoryParameterless                = typeMapper.BuildWrappedFactory(()                                                   => new TestClasses.Wrapped.TestClass2());
			_factoryOneParameterString           = typeMapper.BuildWrappedFactory((string connectionString)                            => new TestClasses.Wrapped.TestClass2(connectionString));
			_factoryOneParameterTimeSpanInstance = typeMapper.BuildFactory       ((TimeSpan timeSpan)                                  => new TestClasses.Wrapped.TestClass2(timeSpan));
			_factoryTwoParametersIntString       = typeMapper.BuildWrappedFactory((int src, string dest)                               => new TestClasses.Wrapped.TestClass2(src, dest));
			_factoryTwoParametersStringString    = typeMapper.BuildWrappedFactory((string src, string dest)                            => new TestClasses.Wrapped.TestClass2(src, dest));
			_factoryThoParametersWrapperEnum     = typeMapper.BuildWrappedFactory((ITestClass2 p1, TestClasses.Wrapped.TestEnum p2)    => new TestClasses.Wrapped.TestClass2((TestClasses.Wrapped.TestClass2)p1, p2));
			_factoryThoParametersWrapperString   = typeMapper.BuildWrappedFactory((ITestClass2 p1, string p2)                          => new TestClasses.Wrapped.TestClass2((TestClasses.Wrapped.TestClass2)p1, p2));
			_factoryThreeParameters              = typeMapper.BuildWrappedFactory((ITestClass2 p1, TestClasses.Wrapped.TestEnum p2, ITestClass p3) => new TestClasses.Wrapped.TestClass2((TestClasses.Wrapped.TestClass2)p1, p2, (TestClasses.Wrapped.TestClass)p3));
			_tstsFactory                         = typeMapper.BuildFactory       ((DateTimeOffset dto, string offset)                  => new TestClasses.Wrapped.TestClass2(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, GetDateTimeOffsetNanoseconds(dto), offset));
		}

		private static int GetDateTimeOffsetNanoseconds(DateTimeOffset value)
		{
			var tmp = new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);

			return Convert.ToInt32((value.Ticks - tmp.Ticks) * NanosecondsPerTick);
		}

		[Benchmark]
		public TestClasses.Wrapped.TestClass2 TypeMapperParameterless()
		{
			return _factoryParameterless();
		}

		[Benchmark(Baseline = true)]
		public TestClasses.Original.TestClass2 DirectAccessParameterless()
		{
			return new TestClasses.Original.TestClass2();
		}

		[Benchmark]
		public TestClasses.Wrapped.TestClass2 TypeMapperOneParameterString()
		{
			return _factoryOneParameterString(StringParameter);
		}

		[Benchmark]
		public TestClasses.Original.TestClass2 DirectAccessOneParameterString()
		{
			return new TestClasses.Original.TestClass2(StringParameter);
		}

		[Benchmark]
		public object TypeMapperOneParameterTimeSpanUnwrap()
		{
			return _factoryOneParameterTimeSpanInstance(TimeSpanParameter);
		}

		[Benchmark]
		public object DirectAccessOneParameterTimeSpanUnwrap()
		{
			return new TestClasses.Original.TestClass2(TimeSpanParameter);
		}

		[Benchmark]
		public TestClasses.Wrapped.TestClass2 TypeMapperTwoParametersIntString()
		{
			return _factoryTwoParametersIntString(IntParameter, StringParameter);
		}

		[Benchmark]
		public TestClasses.Original.TestClass2 DirectAccessTwoParametersIntString()
		{
			return new TestClasses.Original.TestClass2(IntParameter, StringParameter);
		}

		[Benchmark]
		public TestClasses.Wrapped.TestClass2 TypeMapperTwoParametersStringString()
		{
			return _factoryTwoParametersStringString(StringParameter, StringParameter);
		}

		[Benchmark]
		public TestClasses.Original.TestClass2 DirectAccessTwoParametersStringString()
		{
			return new TestClasses.Original.TestClass2(StringParameter, StringParameter);
		}

		[Benchmark]
		public TestClasses.Wrapped.TestClass2 TypeMapperTwoParametersWrapperEnum()
		{
			return _factoryThoParametersWrapperEnum(_testClass2Instance, TestClasses.Wrapped.TestEnum.Three);
		}

		[Benchmark]
		public TestClasses.Original.TestClass2 DirectAccessTwoParametersWrapperEnum()
		{
			return new TestClasses.Original.TestClass2(_testClass2Instance, TestClasses.Original.TestEnum.Three);
		}

		[Benchmark]
		public TestClasses.Wrapped.TestClass2 TypeMapperTwoParametersWrapperString()
		{
			return _factoryThoParametersWrapperString(_testClass2Instance, StringParameter);
		}

		[Benchmark]
		public TestClasses.Original.TestClass2 DirectAccessTwoParametersWrapperString()
		{
			return new TestClasses.Original.TestClass2(_testClass2Instance, StringParameter);
		}

		[Benchmark]
		public TestClasses.Wrapped.TestClass2 TypeMapperThreeParameters()
		{
			return _factoryThreeParameters(_testClass2Instance, TestClasses.Wrapped.TestEnum.Three, _testClassInstance);
		}

		[Benchmark]
		public TestClasses.Original.TestClass2 DirectAccessThreeParameters()
		{
			return new TestClasses.Original.TestClass2(_testClass2Instance, TestClasses.Original.TestEnum.Three, _testClassInstance);
		}

		[Benchmark]
		public object TypeMapperTSTZFactory()
		{
			return _tstsFactory(DateTimeOffsetParameter, StringParameter);
		}

		[Benchmark]
		public object DirectAccessTSTZFactory()
		{
			return new TestClasses.Original.TestClass2(DateTimeOffsetParameter.Year, DateTimeOffsetParameter.Month, DateTimeOffsetParameter.Day, DateTimeOffsetParameter.Hour, DateTimeOffsetParameter.Minute, DateTimeOffsetParameter.Second, GetDateTimeOffsetNanoseconds(DateTimeOffsetParameter), StringParameter);
		}
	}
}
