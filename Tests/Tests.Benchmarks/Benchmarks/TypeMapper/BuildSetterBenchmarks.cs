using System;
using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// Notes:
	// benchmark shows big difference in performance and shows memory allocations due to use of Enum.Parse by wrapper
	// TODO: we should update enum mapper to use value cast for enums with fixed values and probably add some
	// optimizations for others (npgsql)
	public class BuildSetterBenchmarks
	{
		private Mapped.TestClass _classInstance = new Mapped.TestClass();
		private Action<ITestClass, Wrapped.TestEnum> _enumPropertySetter;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Mapped.TestClass), typeof(Mapped.TestEnum));
			typeMapper.RegisterWrapper<Wrapped.TestClass>();
			typeMapper.RegisterWrapper<Wrapped.TestEnum>();

			var enumPropertyBuilder = typeMapper.Type<Wrapped.TestClass>().Member(p => p.EnumProperty);
			_enumPropertySetter = enumPropertyBuilder.BuildSetter<ITestClass>();
		}

		[Benchmark]
		public void TypeMapper()
		{
			_enumPropertySetter(_classInstance, Wrapped.TestEnum.Three);
		}

		[Benchmark(Baseline = true)]
		public void DirectAccess()
		{
			_classInstance.EnumProperty = Mapped.TestEnum.Three;
		}
	}
}
