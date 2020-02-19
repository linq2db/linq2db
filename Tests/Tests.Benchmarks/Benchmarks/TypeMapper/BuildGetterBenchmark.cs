using System;
using System.Data;
using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// benchmark shows big performance degradation and memory allocations for enum accessors
	// due to use of Enum.Parse and boxing. Other types are fine
	// FIX:
	// we should update enum mapper to use value cast for enums with fixed values and probably add some
	// optimizations for others (npgsql)
	public class BuildGetterBenchmark
	{
		private Original.TestClass _classInstance = new Original.TestClass();

		private Func<ITestClass, Wrapped.TestEnum> _enumPropertyGetter;
		private Func<ITestClass, object> _enumPropertyGetterAsObject;
		private Func<object, decimal> _decimalPropertyGetter;
		private Func<object, bool> _booleanPropertyGetter;
		private Func<ITestClass, SqlDbType> _knownEnumPropertyGetter;
		private Func<ITestClass, string> _stringPropertyGetter;
		private Func<ITestClass, bool> _boolPropertyGetter;
		private Func<ITestClass, int> _intPropertyGetter;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = Wrapped.Helper.CreateTypeMapper();

			var typeBuilder = typeMapper.Type<Wrapped.TestClass>();

			var enumPropertyBuilder = typeBuilder.Member(p => p.EnumProperty);
			_enumPropertyGetter = enumPropertyBuilder.BuildGetter<ITestClass>();
			_enumPropertyGetterAsObject = p => _enumPropertyGetter(p);

			_decimalPropertyGetter = typeBuilder.Member(p => p.DecimalProperty).BuildGetter<object>();
			_booleanPropertyGetter = typeBuilder.Member(p => p.BooleanProperty).BuildGetter<object>();

			_knownEnumPropertyGetter = typeBuilder.Member(p => p.KnownEnumProperty).BuildGetter<ITestClass>();
			_stringPropertyGetter = typeBuilder.Member(p => p.StringProperty).BuildGetter<ITestClass>();
			_boolPropertyGetter = typeBuilder.Member(p => p.BooleanProperty).BuildGetter<ITestClass>();
			_intPropertyGetter = typeBuilder.Member(p => p.IntProperty).BuildGetter<ITestClass>();
		}

		[Benchmark]
		public Wrapped.TestEnum TypeMapperAsEnum()
		{
			return _enumPropertyGetter(_classInstance);
		}

		[Benchmark(Baseline = true)]
		public Original.TestEnum DirectAccessAsEnum()
		{
			return _classInstance.EnumProperty;
		}

		[Benchmark]
		public object TypeMapperAsObject()
		{
			return _enumPropertyGetterAsObject(_classInstance);
		}

		[Benchmark]
		public object DirectAccessAsObject()
		{
			return _classInstance.EnumProperty;
		}

		[Benchmark]
		public decimal TypeMapperAsDecimal()
		{
			return _decimalPropertyGetter(_classInstance);
		}

		[Benchmark]
		public decimal DirectAccessAsDecimal()
		{
			return _classInstance.DecimalProperty;
		}

		[Benchmark]
		public bool TypeMapperAsBoolean()
		{
			return _booleanPropertyGetter(_classInstance);
		}

		[Benchmark]
		public bool DirectAccessAsBoolean()
		{
			return _classInstance.BooleanProperty;
		}

		[Benchmark]
		public string TypeMapperAsString()
		{
			return _stringPropertyGetter(_classInstance);
		}

		[Benchmark]
		public string DirectAccessAsString()
		{
			return _classInstance.StringProperty;
		}

		[Benchmark]
		public int TypeMapperAsInt()
		{
			return _intPropertyGetter(_classInstance);
		}
		
		[Benchmark]
		public int DirectAccessAsInt()
		{
			return _classInstance.IntProperty;
		}

		[Benchmark]
		public bool TypeMapperAsBool()
		{
			return _boolPropertyGetter(_classInstance);
		}

		[Benchmark]
		public bool DirectAccessAsBool()
		{
			return _classInstance.BooleanProperty;
		}

		[Benchmark]
		public SqlDbType TypeMapperAsKnownEnum()
		{
			return _knownEnumPropertyGetter(_classInstance);
		}

		[Benchmark]
		public SqlDbType DirectAccessAsKnownEnum()
		{
			return _classInstance.KnownEnumProperty;
		}
	}
}
