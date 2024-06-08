using System.Collections.Generic;
using System.Data;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LinqToDB.Benchmarks.Models;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.Firebird;

namespace LinqToDB.Benchmarks.QueryGeneration
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
			if (_dataProviders.Count > 0)
				return;

			_dataProviders.Add(ProviderName.Access,   new AccessOleDbDataProvider());
			_dataProviders.Add(ProviderName.Firebird, FirebirdTools.GetDataProvider(FirebirdVersion.v5));

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

		Dictionary<string, IDataProvider> _dataProviders = new ();

		public IEnumerable<string> ValuesForDataProvider => _dataProviders.Keys;

		[ParamsSource(nameof(ValuesForDataProvider))]
		public string DataProvider { get; set; } = ProviderName.Access;

		private NorthwindDB GetDataConnection(string providerName)
		{
			return new NorthwindDB(_dataProviders[providerName]);
		}

		[Benchmark]
		public void VwSalesByYear()
		{
			using var db = GetDataConnection(DataProvider);

			for (int i = 0; i < 2; i++)
			{
				var str = db.VwSalesByYear(2020).ToString();
			}
		}

		[Benchmark]
		public void VwSalesByYearMutation()
		{
			using var db = GetDataConnection(DataProvider);

			for (int i = 0; i < 2; i++)
			{
				var str = db.VwSalesByYear(2010 + i).Where(e => i == 1).ToString();
			}
		}

		[Benchmark]
		public void VwSalesByCategoryContains()
		{
			using var db = GetDataConnection(DataProvider);

			for (int i = 0; i < 2; i++)
			{
				var param = i.ToString();
				var str   = db.VwSalesByCategory(2010 + i).Where(e => e.CategoryName.Contains(param)).ToString();
			}
		}

	}
}
