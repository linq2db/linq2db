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
		private Mapped.TestClass _classInstance = new Mapped.TestClass();

		private Func<ITestClass, Wrapped.TestEnum> _enumPropertyGetter;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Mapped.TestClass), typeof(Mapped.TestEnum));
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
		public Mapped.TestEnum DirectAccess()
		{
			return _classInstance.EnumProperty;
		}
	}
}
