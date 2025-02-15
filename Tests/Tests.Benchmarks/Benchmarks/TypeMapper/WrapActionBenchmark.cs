using System.Data;

using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// shows small performance degradation due to indirect call
	public class WrapActionBenchmark
	{
		private const           string      Parameter            = "TestString";
		private static readonly IDataReader IDataReaderParameter = null!;

		private Original.TestClass2 _originalInstance = null!;
		private Wrapped.TestClass2  _wrapperInstance = null!;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper    = Wrapped.Helper.CreateTypeMapper();

			_originalInstance = new Original.TestClass2(Parameter);
			_wrapperInstance  = typeMapper.BuildWrappedFactory((string p) => new Wrapped.TestClass2(p))(Parameter);
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
