using System;
using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// Notes:
	// benchmark shows big difference in performance and shows memory allocations due to use of Enum.Parse by wrapper
	// TODO: we should update enum mapper to use value cast for enums with fixed values and probably add some
	// optimizations for others (npgsql)
	public class BuildGetterBenchmarks
	{
		private Original.TestClass _classInstance = new Original.TestClass();

		private Func<ITestClass, Wrapped.TestEnum> _enumPropertyGetter;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Original.TestClass), typeof(Original.TestEnum));
			typeMapper.RegisterWrapper<Wrapped.TestClass>();
			typeMapper.RegisterWrapper<Wrapped.TestEnum>();

			var enumPropertyBuilder = typeMapper.Type<Wrapped.TestClass>().Member(p => p.EnumProperty);
			_enumPropertyGetter = enumPropertyBuilder.BuildGetter<ITestClass>();
		}

		[Benchmark]
		public Wrapped.TestEnum TypeMapper()
		{
			return _enumPropertyGetter(_classInstance);
		}

		[Benchmark(Baseline = true)]
		public Original.TestEnum DirectAccess()
		{
			return _classInstance.EnumProperty;
		}
	}
}
