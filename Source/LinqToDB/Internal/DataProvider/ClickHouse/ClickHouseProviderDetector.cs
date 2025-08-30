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
	public class ClickHouseProviderDetector : ProviderDetectorBase<ClickHouseProvider>
	{
		public ClickHouseProviderDetector() : base()
		{
		}

		static readonly Lazy<IDataProvider> _octonicaDataProvider = CreateDataProvider<ClickHouseOctonicaDataProvider>();
		static readonly Lazy<IDataProvider> _clientDataProvider   = CreateDataProvider<ClickHouseDriverDataProvider>();
		static readonly Lazy<IDataProvider> _mysqlDataProvider    = CreateDataProvider<ClickHouseMySqlDataProvider>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
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
			if (provider == ClickHouseProvider.AutoDetect)
				provider = DetectProvider();

			return provider switch
			{
				ClickHouseProvider.MySqlConnector   => _mysqlDataProvider.Value,
				ClickHouseProvider.ClickHouseDriver => _clientDataProvider.Value,
				_                                   => _octonicaDataProvider.Value,
			};
		}

		public static ClickHouseProvider DetectProvider()
		{
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

		protected override DbConnection CreateConnection(ClickHouseProvider provider, string connectionString)
		{
			return ClickHouseProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
