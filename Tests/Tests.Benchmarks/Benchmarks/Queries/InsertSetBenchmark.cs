using System;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.Mappings;
using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;

namespace LinqToDB.Benchmarks.Queries
{
	public class InsertSetBenchmark
	{
		readonly int            _batchSize = 100;
		IEnumerable<CreditCard> _data      = null!;
		QueryResult             _result    = null!;
		IDataProvider           _provider  = SqlServerTools.GetDataProvider(SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient);

		[GlobalSetup]
		public void Setup()
		{
			_data = Enumerable.Range(0, 1000).Select(_ => new CreditCard()
			{
				CreditCardID = _,
				CardNumber   = $"card #{_}",
				CardType     = $"card type {_}",
				ExpMonth     = (byte)(_ % 12),
				ExpYear      = (short)(_ % 1000),
				ModifiedDate = DateTime.Now,

			}).ToArray();

			_result = new QueryResult()
			{
				Return = _batchSize,
			};
		}

		[Benchmark(Baseline = true)]
		public BulkCopyRowsCopied Test()
		{
			using var db = new Db(_provider, _result);
			return db.BulkCopy(new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows, MaxBatchSize = _batchSize }, _data);
		}
	}
}
