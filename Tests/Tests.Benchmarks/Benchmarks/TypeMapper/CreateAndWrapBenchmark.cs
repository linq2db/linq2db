using System;

using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
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

		private readonly Original.TestClass _testClassInstance   = new Original.TestClass();
		private readonly Original.TestClass2 _testClass2Instance = new Original.TestClass2();

		private Func<Wrapped.TestClass2                                           > _factoryParameterless = null!;
		private Func<string, Wrapped.TestClass2                                   > _factoryOneParameterString = null!;
		private Func<TimeSpan, object                                             > _factoryOneParameterTimeSpanInstance = null!;
		private Func<int, string, Wrapped.TestClass2                              > _factoryTwoParametersIntString = null!;
		private Func<string, string, Wrapped.TestClass2                           > _factoryTwoParametersStringString = null!;
		private Func<ITestClass2, Wrapped.TestEnum, Wrapped.TestClass2            > _factoryThoParametersWrapperEnum = null!;
		private Func<ITestClass2, string, Wrapped.TestClass2                      > _factoryThoParametersWrapperString = null!;
		private Func<ITestClass2, Wrapped.TestEnum, ITestClass, Wrapped.TestClass2> _factoryThreeParameters = null!;
		private Func<DateTimeOffset, string, object                               > _tstsFactory = null!;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = Wrapped.Helper.CreateTypeMapper();

			_factoryParameterless                = typeMapper.BuildWrappedFactory(()                                                   => new Wrapped.TestClass2());
			_factoryOneParameterString           = typeMapper.BuildWrappedFactory((string connectionString)                            => new Wrapped.TestClass2(connectionString));
			_factoryOneParameterTimeSpanInstance = typeMapper.BuildFactory       ((TimeSpan timeSpan)                                  => new Wrapped.TestClass2(timeSpan));
			_factoryTwoParametersIntString       = typeMapper.BuildWrappedFactory((int src, string dest)                               => new Wrapped.TestClass2(src, dest));
			_factoryTwoParametersStringString    = typeMapper.BuildWrappedFactory((string src, string dest)                            => new Wrapped.TestClass2(src, dest));
			_factoryThoParametersWrapperEnum     = typeMapper.BuildWrappedFactory((ITestClass2 p1, Wrapped.TestEnum p2)                => new Wrapped.TestClass2((Wrapped.TestClass2)p1, p2));
			_factoryThoParametersWrapperString   = typeMapper.BuildWrappedFactory((ITestClass2 p1, string p2)                          => new Wrapped.TestClass2((Wrapped.TestClass2)p1, p2));
			_factoryThreeParameters              = typeMapper.BuildWrappedFactory((ITestClass2 p1, Wrapped.TestEnum p2, ITestClass p3) => new Wrapped.TestClass2((Wrapped.TestClass2)p1, p2, (Wrapped.TestClass)p3));
			_tstsFactory                         = typeMapper.BuildFactory       ((DateTimeOffset dto, string offset)                  => new Wrapped.TestClass2(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, GetDateTimeOffsetNanoseconds(dto), offset));
		}

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
