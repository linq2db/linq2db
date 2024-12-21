using System;
using System.Data;

using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// shows small performance degradation due to indirect call
	public class BuildFuncBenchmark
	{
		private Original.TestClass _classInstance = new Original.TestClass();

		private Func<ITestClass, Guid, object[]?, DataTable> _functionCall = null!;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = Wrapped.Helper.CreateTypeMapper();

			_functionCall = typeMapper.BuildFunc<ITestClass, Guid, object[]?, DataTable>(typeMapper.MapLambda((Wrapped.TestClass conn, Guid schema, object[] restrictions) => conn.GetOleDbSchemaTable(schema, restrictions)));
		}

		[Benchmark]
		public DataTable BuildFunc()
		{
			return _functionCall(_classInstance, Guid.Empty, null);
		}

		[Benchmark(Baseline = true)]
		public DataTable DirectAccess()
		{
			return _classInstance.GetOleDbSchemaTable(Guid.Empty, null);
		}
	}
}
