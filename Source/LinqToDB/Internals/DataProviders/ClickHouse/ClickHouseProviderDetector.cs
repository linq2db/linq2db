using System;
using System.Data.Common;
using System.IO;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Internals.DataProviders.MySql;

namespace LinqToDB.Internals.DataProviders.ClickHouse
{
	sealed class ClickHouseProviderDetector : ProviderDetectorBase<ClickHouseProvider, ClickHouseProviderDetector.Dialect>
	{
		internal enum Dialect { }

		public ClickHouseProviderDetector() : base()
		{
		}

		static readonly Lazy<IDataProvider> _octonicaDataProvider = CreateDataProvider<ClickHouseOctonicaDataProvider>();
		static readonly Lazy<IDataProvider> _clientDataProvider   = CreateDataProvider<ClickHouseClientDataProvider>();
		static readonly Lazy<IDataProvider> _mysqlDataProvider    = CreateDataProvider<ClickHouseMySqlDataProvider>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			if (options.ProviderName?.Contains("Octonica") == true && options.ProviderName?.Contains("ClickHouse") == true
				|| options.ConfigurationString?.Contains("Octonica") == true && options.ConfigurationString?.Contains("ClickHouse") == true)
				return _octonicaDataProvider.Value;

			if (options.ProviderName?.Contains("ClickHouse") == true && options.ProviderName?.Contains("MySql") == true
				|| options.ConfigurationString?.Contains("ClickHouse") == true && options.ConfigurationString?.Contains("MySql") == true)
				return _mysqlDataProvider.Value;

			if (options.ProviderName?.Contains("ClickHouse.Client") == true || options.ConfigurationString?.Contains("ClickHouse.Client") == true)
				return _clientDataProvider.Value;

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, ClickHouseProvider provider, Dialect version)
		{
			if (provider == ClickHouseProvider.AutoDetect)
				provider = DetectProvider();

			return provider switch
			{
				ClickHouseProvider.MySqlConnector => _mysqlDataProvider.Value,
				ClickHouseProvider.ClickHouseClient => _clientDataProvider.Value,
				_ => _octonicaDataProvider.Value,
			};
		}

		public static ClickHouseProvider DetectProvider()
		{
			var fileName = typeof(ClickHouseProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			if (File.Exists(Path.Combine(dirName ?? ".", ClickHouseProviderAdapter.OctonicaAssemblyName + ".dll")))
				return ClickHouseProvider.Octonica;
			if (File.Exists(Path.Combine(dirName ?? ".", ClickHouseProviderAdapter.ClientClientNamespace + ".dll")))
				return ClickHouseProvider.ClickHouseClient;
			if (File.Exists(Path.Combine(dirName ?? ".", MySqlProviderAdapter.MySqlConnectorAssemblyName + ".dll")))
				return ClickHouseProvider.MySqlConnector;
			return ClickHouseProvider.Octonica;
		}

		public override Dialect? DetectServerVersion(DbConnection connection)
		{
			return default(Dialect);
		}

		protected override DbConnection CreateConnection(ClickHouseProvider provider, string connectionString)
		{
			return ClickHouseProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
