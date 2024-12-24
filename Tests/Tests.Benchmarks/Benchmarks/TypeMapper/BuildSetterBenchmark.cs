using System;
using System.Data;

using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// shows small performance degradation due to indirect call
	public class BuildSetterBenchmark
	{
		private const string Parameter = "TestString";

		private Original.TestClass _classInstance = new Original.TestClass();

		private Action<ITestClass, Wrapped.TestEnum> _enumPropertySetter = null!;
		private Action<ITestClass, SqlDbType       > _knownEnumPropertySetter = null!;
		private Action<ITestClass, string          > _stringPropertySetter = null!;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper           = Wrapped.Helper.CreateTypeMapper();
			var typeBuilder          = typeMapper.Type<Wrapped.TestClass>();

			_enumPropertySetter      = typeBuilder.Member(p => p.EnumProperty     ).BuildSetter<ITestClass>();
			_knownEnumPropertySetter = typeBuilder.Member(p => p.KnownEnumProperty).BuildSetter<ITestClass>();
			_stringPropertySetter    = typeBuilder.Member(p => p.StringProperty   ).BuildSetter<ITestClass>();
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
