using System;
using System.Data;
using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// benchmark shows big performance degradation and memory allocations for enum accessors
	// due to use of Enum.Parse. Other types are fine
	// FIX:
	// we should update enum mapper to use value cast for enums with fixed values and probably add some
	// optimizations for others (npgsql)
	public class BuildSetterBenchmark
	{
		private static readonly string Parameter = "TestString";

		private Original.TestClass _classInstance = new Original.TestClass();
		private Action<ITestClass, Wrapped.TestEnum> _enumPropertySetter;
		private Action<ITestClass, SqlDbType> _knownEnumPropertySetter;
		private Action<ITestClass, string> _stringPropertySetter;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Original.TestClass), typeof(Original.TestEnum));
			typeMapper.RegisterWrapper<Wrapped.TestClass>();
			typeMapper.RegisterWrapper<Wrapped.TestEnum>();

			var typeBuilder = typeMapper.Type<Wrapped.TestClass>();
			_enumPropertySetter = typeBuilder.Member(p => p.EnumProperty).BuildSetter<ITestClass>();
			_knownEnumPropertySetter = typeBuilder.Member(p => p.KnownEnumProperty).BuildSetter<ITestClass>();
			_stringPropertySetter = typeBuilder.Member(p => p.StringProperty).BuildSetter<ITestClass>();
		}

		[Benchmark]
		public void TypeMapperAsEnum()
		{
			_enumPropertySetter(_classInstance, Wrapped.TestEnum.Three);
		}

		[Benchmark(Baseline = true)]
		public void DirectAccessAsEnum()
		{
			_classInstance.EnumProperty = Original.TestEnum.Three;
		}

		[Benchmark]
		public void TypeMapperAsKnownEnum()
		{
			_knownEnumPropertySetter(_classInstance, SqlDbType.BigInt);
		}

		[Benchmark]
		public void DirectAccessAsKnownEnum()
		{
			_classInstance.KnownEnumProperty = SqlDbType.BigInt;
		}

		[Benchmark]
		public void TypeMapperAsString()
		{
			_stringPropertySetter(_classInstance, Parameter);
		}

		[Benchmark]
		public void DirectAccessAsString()
		{
			_classInstance.StringProperty = Parameter;
		}
	}
}
