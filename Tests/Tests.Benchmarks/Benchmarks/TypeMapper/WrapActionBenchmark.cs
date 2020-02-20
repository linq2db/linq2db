using System;
using System.Data;
using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// benchmark shows expected slight performance degradation due to indirect call
	public class WrapActionBenchmark
	{
		private static readonly string Parameter = "TestString";
		private static readonly IDataReader IDataReaderParameter = null;

		private Original.TestClass2 _originalInstance;
		private Wrapped.TestClass2  _wrapperInstance;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = Wrapped.Helper.CreateTypeMapper();

			_originalInstance = new Original.TestClass2(Parameter);
			// TODO: FIXME: direct call crashes right now
			//_wrapperInstance = typeMapper.CreateAndWrap(() => new Wrapped.TestClass2(Parameter));
			_wrapperInstance = ((Func<string, Wrapped.TestClass2>)((string connectionString) => typeMapper.CreateAndWrap(() => new Wrapped.TestClass2(connectionString))))(Parameter);
		}

		[Benchmark]
		public void TypeMapperAction()
		{
			_wrapperInstance.CreateDatabase();
		}

		[Benchmark(Baseline = true)]
		public void DirectAccessAction()
		{
			_originalInstance.CreateDatabase();
		}

		[Benchmark]
		public void TypeMapperActionWithCast()
		{
			_wrapperInstance.Dispose();
		}

		[Benchmark]
		public void DirectAccessActionWithCast()
		{
			_originalInstance.Dispose();
		}

		[Benchmark]
		public void TypeMapperActionWithParameter()
		{
			_wrapperInstance.WriteToServer(IDataReaderParameter);
		}

		[Benchmark]
		public void DirectAccessActionWithParameter()
		{
			_originalInstance.WriteToServer(IDataReaderParameter);
		}
	}
}
