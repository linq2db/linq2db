using System;
using System.Data;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.TestClasses;

namespace LinqToDB.Benchmarks.Types
{
	// shows small performance degradation due to indirect call
	// two enum benchmarks show extra allocations due to boxing of unknown value in enum converter (edge case)
	// (could be removed by using known enum value for test or define 0-value in enums)
	public class BuildGetterBenchmark
	{
		private TestClasses.Original.TestClass _classInstance = new TestClasses.Original.TestClass();

		private Func<ITestClass, TestClasses.Wrapped.TestEnum> _enumPropertyGetter = null!;
		private Func<ITestClass, object                      > _enumPropertyGetterAsObject = null!;
		private Func<object    , decimal                     > _decimalPropertyGetter = null!;
		private Func<object    , bool                        > _booleanPropertyGetter = null!;
		private Func<ITestClass, SqlDbType                   > _knownEnumPropertyGetter = null!;
		private Func<ITestClass, string?                     > _stringPropertyGetter = null!;
		private Func<ITestClass, bool                        > _boolPropertyGetter = null!;
		private Func<ITestClass, int                         > _intPropertyGetter = null!;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper              = TestClasses.Wrapped.Helper.CreateTypeMapper();
			var typeBuilder             = typeMapper.Type<TestClasses.Wrapped.TestClass>();
			var enumPropertyBuilder     = typeBuilder.Member(p => p.EnumProperty);
			_enumPropertyGetter         = enumPropertyBuilder.BuildGetter<ITestClass>();
			_enumPropertyGetterAsObject = p => _enumPropertyGetter(p);

			_decimalPropertyGetter      = typeBuilder.Member(p => p.DecimalProperty  ).BuildGetter<object>();
			_booleanPropertyGetter      = typeBuilder.Member(p => p.BooleanProperty  ).BuildGetter<object>();
			_knownEnumPropertyGetter    = typeBuilder.Member(p => p.KnownEnumProperty).BuildGetter<ITestClass>();
			_stringPropertyGetter       = typeBuilder.Member(p => p.StringProperty   ).BuildGetter<ITestClass>();
			_boolPropertyGetter         = typeBuilder.Member(p => p.BooleanProperty  ).BuildGetter<ITestClass>();
			_intPropertyGetter          = typeBuilder.Member(p => p.IntProperty      ).BuildGetter<ITestClass>();
		}

		[Benchmark]
		public TestClasses.Wrapped.TestEnum TypeMapperAsEnum()
		{
			return _enumPropertyGetter(_classInstance);
		}

		[Benchmark(Baseline = true)]
		public TestClasses.Original.TestEnum DirectAccessAsEnum()
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
		public string? TypeMapperAsString()
		{
			return _stringPropertyGetter(_classInstance);
		}

		[Benchmark]
		public string? DirectAccessAsString()
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
