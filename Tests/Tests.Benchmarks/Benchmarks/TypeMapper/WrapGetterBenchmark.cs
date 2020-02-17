using System;
using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// FIX: benchmark shows huge performance and memory impact due to build of expression recompilation
	public class WrapGetterBenchmark
	{
		private Original.TestClass2 _originalInstance;
		private Wrapped.TestClass2 _wrapperInstance;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Original.TestClass2), typeof(Original.TestEventHandler));
			typeMapper.RegisterWrapper<Wrapped.TestClass2>();
			typeMapper.RegisterWrapper<Wrapped.TestEventHandler>();

			_originalInstance = new Original.TestClass2();
			_wrapperInstance = typeMapper.CreateAndWrap(() => new Wrapped.TestClass2());
		}

		[Benchmark]
		public string TypeMapperString()
		{
			return _wrapperInstance.StringProperty;
		}

		[Benchmark(Baseline = true)]
		public string DirectAccessString()
		{
			return _originalInstance.StringProperty;
		}

		[Benchmark]
		public int TypeMapperInt()
		{
			return _wrapperInstance.IntProperty;
		}

		[Benchmark]
		public int DirectAccessInt()
		{
			return _originalInstance.IntProperty;
		}

		[Benchmark]
		public long TypeMapperLong()
		{
			return _wrapperInstance.LongProperty;
		}

		[Benchmark]
		public long DirectAccessLong()
		{
			return _originalInstance.LongProperty;
		}

		[Benchmark]
		public bool TypeMapperBoolean()
		{
			return _wrapperInstance.BooleanProperty;
		}

		[Benchmark]
		public bool DirectAccessBoolean()
		{
			return _originalInstance.BooleanProperty;
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapperWrapper()
		{
			return _wrapperInstance.WrapperProperty;
		}

		[Benchmark]
		public Original.TestClass2 DirectAccessWrapper()
		{
			return _originalInstance.WrapperProperty;
		}

		[Benchmark]
		public Wrapped.TestEnum TypeMapperEnum()
		{
			return _wrapperInstance.EnumProperty;
		}

		[Benchmark]
		public Original.TestEnum DirectAccessEnum()
		{
			return _originalInstance.EnumProperty;
		}

		[Benchmark]
		public Version TypeMapperVersion()
		{
			return _wrapperInstance.VersionProperty;
		}

		[Benchmark]
		public Version DirectAccessVersion()
		{
			return _originalInstance.VersionProperty;
		}
	}
}
