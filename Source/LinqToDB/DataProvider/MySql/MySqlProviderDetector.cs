using System;
using System.Data.Common;
using System.IO;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Data;

	sealed class MySqlProviderDetector : ProviderDetectorBase<MySqlProvider, MySqlProviderDetector.Dialect, MySqlProviderAdapter.MySqlConnection>
	{
		internal enum Dialect { }

		public MySqlProviderDetector() : base(default, default)
		{
		}

		static readonly Lazy<IDataProvider> _mySqlDataProvider          = DataConnection.CreateDataProvider<MySqlDataProviderMySqlOfficial>();
		static readonly Lazy<IDataProvider> _mySqlConnectorDataProvider = DataConnection.CreateDataProvider<MySqlDataProviderMySqlConnector>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			// ensure ClickHouse configuration over mysql protocol is not detected as mysql
			if (options.ProviderName?.Contains("ClickHouse") == true || options.ConfigurationString?.Contains("ClickHouse") == true)
				return null;

			var provider = options.ProviderName switch
			{
				MySqlProviderAdapter.MySqlConnectorNamespace  => MySqlProvider.MySqlConnector,
				MySqlProviderAdapter.MySqlDataClientNamespace => MySqlProvider.MySqlData,
				_                                             => DetectProvider()
			};

			switch (options.ProviderName)
			{
				case ProviderName.MySqlOfficial                :
				case MySqlProviderAdapter.MySqlDataAssemblyName: return _mySqlDataProvider.Value;
				case ProviderName.MariaDB                      :
				case ProviderName.MySqlConnector               : return _mySqlConnectorDataProvider.Value;

				case ""                         :
				case null                       :
					if (options.ConfigurationString?.Contains("MySql") == true)
						goto case ProviderName.MySql;
					break;
				case MySqlProviderAdapter.MySqlDataClientNamespace:
				case ProviderName.MySql                           :
					if (options.ConfigurationString?.Contains(MySqlProviderAdapter.MySqlConnectorAssemblyName) == true)
						return _mySqlConnectorDataProvider.Value;

					if (options.ConfigurationString?.Contains(MySqlProviderAdapter.MySqlDataAssemblyName) == true)
						return _mySqlDataProvider.Value;

					//if (AutoDetectProvider)
					//{
					//	try
					//	{
					//		var dv = DetectServerVersion(options, provider);

					//		return dv != null ? GetDataProvider(options, provider, dv.Value) : null;
					//	}
					//	catch
					//	{
					//		// ignored
					//	}
					//}

					return GetDataProvider(options, provider, DefaultVersion);
				case var providerName when providerName.Contains("MySql"):
					if (providerName.Contains(MySqlProviderAdapter.MySqlConnectorAssemblyName))
						return _mySqlConnectorDataProvider.Value;

					if (providerName.Contains(MySqlProviderAdapter.MySqlDataAssemblyName))
						return _mySqlDataProvider.Value;

					goto case ProviderName.MySql;
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, MySqlProvider provider, Dialect version)
		{
			if (provider == MySqlProvider.AutoDetect)
				provider = DetectProvider();

			return provider switch
			{
				MySqlProvider.MySqlData => _mySqlDataProvider.Value,
				_                       => _mySqlConnectorDataProvider.Value,
			};
		}

		public static MySqlProvider DetectProvider()
		{
			var fileName = typeof(MySqlProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", MySqlProviderAdapter.MySqlDataAssemblyName + ".dll"))
				? MySqlProvider.MySqlData
				: MySqlProvider.MySqlConnector;
		}

		public override Dialect? DetectServerVersion(MySqlProviderAdapter.MySqlConnection connection)
		{
			return default(Dialect);
		}

		protected override MySqlProviderAdapter.MySqlConnection CreateConnection(MySqlProvider provider, string connectionString)
		{
			return MySqlProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
