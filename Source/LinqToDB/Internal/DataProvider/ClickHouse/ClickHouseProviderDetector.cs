using System;
using System.Data.Common;
using System.IO;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.MySql;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	public class ClickHouseProviderDetector() : ProviderDetectorBase<ClickHouseProvider>()
	{
		static readonly Lazy<IDataProvider> _octonicaDataProvider = CreateDataProvider<ClickHouseOctonicaDataProvider>();
		static readonly Lazy<IDataProvider> _clientDataProvider   = CreateDataProvider<ClickHouseDriverDataProvider>();
		static readonly Lazy<IDataProvider> _mysqlDataProvider    = CreateDataProvider<ClickHouseMySqlDataProvider>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			// don't merge DetectProvider and DetectProvider logic and later could return inconclusive
			if ((options.ProviderName?.Contains("Octonica") == true && options.ProviderName?.Contains("ClickHouse") == true)
				|| (options.ConfigurationString?.Contains("Octonica") == true && options.ConfigurationString?.Contains("ClickHouse") == true))
				return _octonicaDataProvider.Value;

			if ((options.ProviderName?.Contains("ClickHouse") == true && options.ProviderName?.Contains("MySql") == true)
				|| (options.ConfigurationString?.Contains("ClickHouse") == true && options.ConfigurationString?.Contains("MySql") == true))
				return _mysqlDataProvider.Value;

			if (options.ProviderName?.Contains("ClickHouse.Driver") == true || options.ConfigurationString?.Contains("ClickHouse.Driver") == true)
				return _clientDataProvider.Value;

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, ClickHouseProvider provider, NoDialect version)
		{
			provider = DetectProvider(options, provider);

			return provider switch
			{
				ClickHouseProvider.MySqlConnector   => _mysqlDataProvider.Value,
				ClickHouseProvider.ClickHouseDriver => _clientDataProvider.Value,
				_                                   => _octonicaDataProvider.Value,
			};
		}

		protected override DbConnection CreateConnection(ClickHouseProvider provider, string connectionString)
		{
			return ClickHouseProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}

		protected override ClickHouseProvider DetectProvider(ConnectionOptions options, ClickHouseProvider provider)
		{
			if (provider is ClickHouseProvider.ClickHouseDriver or ClickHouseProvider.MySqlConnector or ClickHouseProvider.Octonica)
				return provider;

			if ((options.ProviderName?.Contains("Octonica") == true && options.ProviderName?.Contains("ClickHouse") == true)
				|| (options.ConfigurationString?.Contains("Octonica") == true && options.ConfigurationString?.Contains("ClickHouse") == true))
				return ClickHouseProvider.Octonica;

			if ((options.ProviderName?.Contains("ClickHouse") == true && options.ProviderName?.Contains("MySql") == true)
				|| (options.ConfigurationString?.Contains("ClickHouse") == true && options.ConfigurationString?.Contains("MySql") == true))
				return ClickHouseProvider.MySqlConnector;

			if (options.ProviderName?.Contains("ClickHouse.Driver") == true || options.ConfigurationString?.Contains("ClickHouse.Driver") == true)
				return ClickHouseProvider.ClickHouseDriver;

			var fileName = typeof(ClickHouseProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", ClickHouseProviderAdapter.OctonicaAssemblyName + ".dll"))
				? ClickHouseProvider.Octonica
				: File.Exists(Path.Combine(dirName ?? ".", ClickHouseProviderAdapter.DriverAssemblyName + ".dll"))
					? ClickHouseProvider.ClickHouseDriver
					: File.Exists(Path.Combine(dirName ?? ".", MySqlProviderAdapter.MySqlConnectorAssemblyName + ".dll"))
						? ClickHouseProvider.MySqlConnector
						: ClickHouseProvider.Octonica;
		}
	}
}
