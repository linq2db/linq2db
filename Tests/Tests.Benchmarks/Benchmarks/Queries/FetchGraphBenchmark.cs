using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using LinqToDB.Async;
using LinqToDB.Benchmarks.Mappings;
using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;

namespace LinqToDB.Benchmarks.Queries
{
	public class FetchGraphBenchmark
	{
		Func<Db,IQueryable<SalesOrderHeader>> _compiled = null!;
		QueryResult[]                         _results  = null!;
		IDataProvider                         _provider = SqlServerTools.GetDataProvider(SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient);

		[GlobalSetup]
		public void Setup()
		{
			_results = new QueryResult[]
			{
				new QueryResult()
				{
					Schema       = EagerLoad.SchemaTable_SalesOrderDetails,
					Names        = EagerLoad.Names_SalesOrderDetails,
					FieldTypes   = EagerLoad.FieldTypes_SalesOrderDetails,
					DbTypes      = EagerLoad.DbTypes_SalesOrderDetails,
					Data         = Enumerable.Range(0, 4768).Select(_ => EagerLoad.SampleRow_SalesOrderDetails(_ % 1000)).ToArray(),
					//Data         = Enumerable.Range(0, 47).Select(_ => EagerLoad.SampleRow_SalesOrderDetails(_ % 1000)).ToArray(),
					Match        = sql => sql.Contains("[SalesOrderDetail]")
				},
				new QueryResult()
				{
					Schema       = EagerLoad.SchemaTable_HeaderCustomer,
					Names        = EagerLoad.Names_HeaderCustomer,
					FieldTypes   = EagerLoad.FieldTypes_HeaderCustomer,
					DbTypes      = EagerLoad.DbTypes_HeaderCustomer,
					Data         = Enumerable.Range(0, 1000).Select(_ => EagerLoad.SampleRow_HeaderCustomer(_ % 1000)).ToArray(),
					//Data         = Enumerable.Range(0, 10).Select(_ => EagerLoad.SampleRow_HeaderCustomer(_ % 1000)).ToArray(),
					Match        = sql => sql.Contains("LEFT JOIN [Sales].[Customer]")
				}
			};

			_compiled = CompiledQuery.Compile(
				(Db db) => 
					from soh in db.SalesOrderHeaders
						.LoadWith(x => x.SalesOrderDetails)
						.LoadWith(x => x.Customer)
					where soh.SalesOrderID > 50000 && soh.SalesOrderID <= 51000
					select soh
			);
		}

		[GlobalCleanup]
		public void Cleanup()
		{
		}

		[Benchmark]
		public List<SalesOrderHeader> Linq()
		{
			using var db = new Db(_provider, _results);
			return (
				from soh in db.SalesOrderHeaders
					.LoadWith(x => x.SalesOrderDetails)
					.LoadWith(x => x.Customer)
				where soh.SalesOrderID > 50000 && soh.SalesOrderID <= 51000
				select soh
			)
				.ToList();
		}

		[Benchmark]
		public async Task<List<SalesOrderHeader>> LinqAsync()
		{
			using var db = new Db(_provider, _results);
			return await (
				from soh in db.SalesOrderHeaders
					.LoadWith(x => x.SalesOrderDetails)
					.LoadWith(x => x.Customer)
				where soh.SalesOrderID > 50000 && soh.SalesOrderID <= 51000
				select soh
			)
				.ToListAsync();
		}

		[Benchmark(Baseline = true)]
		public List<SalesOrderHeader> Compiled()
		{
			using var db = new Db(_provider, _results);
			return _compiled(db).ToList();
		}

		[Benchmark]
		public async Task<List<SalesOrderHeader>> CompiledAsync()
		{
			using var db = new Db(_provider, _results);
			return await _compiled(db).ToListAsync();
		}
	}
}
