using System;
using System.Data;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LinqToDB.Benchmarks.Models;
using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.Firebird;

namespace LinqToDB.Benchmarks.Benchmarks.QueryGeneration
{
	public class QueryGenerationBenchmark
	{
		public QueryGenerationBenchmark()
		{
			Setup();
		}

		[GlobalSetup]
		public void Setup()
		{
			if (Options != null)
				return;

			Options = new[]
			{
				new DataOptions().UseDataProvider(new AccessOleDbDataProvider()).UseConnection(new MockDbConnection(Array.Empty<QueryResult>(), ConnectionState.Open)),
				new DataOptions().UseDataProvider(new FirebirdDataProvider()).UseConnection(new MockDbConnection(Array.Empty<QueryResult>(), ConnectionState.Open)),
			};

			CurrentOptions = Options[0];

			/*
			_dataProviders.Add(ProviderName.SQLiteMS,              new SQLiteDataProvider(ProviderName.SQLiteMS));
			_dataProviders.Add(ProviderName.SQLiteClassic,         new SQLiteDataProvider(ProviderName.SQLiteClassic));
			//_dataProviders.Add(ProviderName.OracleManaged + ".11", new OracleDataProvider(ProviderName.OracleManaged, OracleVersion.v11));
			//_dataProviders.Add(ProviderName.OracleManaged + ".12", new OracleDataProvider(ProviderName.OracleManaged, OracleVersion.v12));
			_dataProviders.Add(ProviderName.MySqlConnector,        new MySqlDataProvider(ProviderName.MySqlConnector));
			//_dataProviders.Add(ProviderName.Informix,              new InformixDataProvider(ProviderName.Informix));
			//_dataProviders.Add(ProviderName.DB2LUW,                new DB2DataProvider(ProviderName.DB2, DB2Version.LUW));
			//_dataProviders.Add(ProviderName.DB2zOS,                new DB2DataProvider(ProviderName.DB2, DB2Version.zOS));
			_dataProviders.Add(ProviderName.PostgreSQL,            new PostgreSQLDataProvider(ProviderName.PostgreSQL, PostgreSQLVersion.v95));
			_dataProviders.Add(ProviderName.SqlServer2005,         new SqlServerDataProvider(ProviderName.SqlServer2005, SqlServerVersion.v2005));
			_dataProviders.Add(ProviderName.SqlServer2008,         new SqlServerDataProvider(ProviderName.SqlServer2008, SqlServerVersion.v2008));
			_dataProviders.Add(ProviderName.SqlServer2012,         new SqlServerDataProvider(ProviderName.SqlServer2012, SqlServerVersion.v2012));
			_dataProviders.Add(ProviderName.SqlServer2017,         new SqlServerDataProvider(ProviderName.SqlServer2017, SqlServerVersion.v2017));
		*/
		}

		public DataOptions[]? Options;

		[ParamsSource(nameof(Options))]
		public DataOptions CurrentOptions { get; set; } = default!;

		private NorthwindDB GetDataConnection(DataOptions options)
		{
			return new NorthwindDB(options);
		}

		[Benchmark]
		public void VwSalesByYear()
		{
			using var db = GetDataConnection(CurrentOptions);

			for (int i = 0; i < 2; i++)
			{
				var str = db.VwSalesByYear(2020).ToString();
			}
		}

		[Benchmark]
		public void VwSalesByYearMutation()
		{
			using var db = GetDataConnection(CurrentOptions);

			for (int i = 0; i < 2; i++)
			{
				var str = db.VwSalesByYear(2010 + i).Where(e => i == 1).ToString();
			}
		}

		[Benchmark]
		public void VwSalesByCategoryContains()
		{
			using var db = GetDataConnection(CurrentOptions);

			for (int i = 0; i < 2; i++)
			{
				var param = i.ToString();
				var str   = db.VwSalesByCategory(2010 + i).Where(e => e.CategoryName.Contains(param)).ToString();
			}
		}

	}
}
