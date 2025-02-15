using System;
using System.Data.Common;
using System.IO;

using LinqToDB.Common;
using LinqToDB.Data;

namespace LinqToDB.DataProvider.MySql
{
	sealed class MySqlProviderDetector : ProviderDetectorBase<MySqlProvider, MySqlVersion>
	{
		public MySqlProviderDetector() : base(MySqlVersion.AutoDetect, MySqlVersion.MySql57)
		{
		}

		static readonly Lazy<IDataProvider> _mySql57DataProvider            = CreateDataProvider<MySql57DataProviderMySqlData>();
		static readonly Lazy<IDataProvider> _mySql57ConnectorDataProvider   = CreateDataProvider<MySql57DataProviderMySqlConnector>();
		static readonly Lazy<IDataProvider> _mySql80DataProvider            = CreateDataProvider<MySql80DataProviderMySqlData>();
		static readonly Lazy<IDataProvider> _mySql80ConnectorDataProvider   = CreateDataProvider<MySql80DataProviderMySqlConnector>();
		static readonly Lazy<IDataProvider> _mariadb10DataProvider          = CreateDataProvider<MariaDB10DataProviderMySqlData>();
		static readonly Lazy<IDataProvider> _mariadb10ConnectorDataProvider = CreateDataProvider<MariaDB10DataProviderMySqlConnector>();

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
				case ""                      :
				case null                    :
					if (options.ConfigurationString?.Contains("MySql") == true)
						goto case ProviderName.MySql;
					if (options.ConfigurationString?.Contains("MariaDB") == true)
						goto case "MariaDB";
					break;
				case ProviderName.MySql57MySqlData:
					return GetDataProvider(options, MySqlProvider.MySqlData, MySqlVersion.MySql57);
				case ProviderName.MySql57MySqlConnector:
					return GetDataProvider(options, MySqlProvider.MySqlConnector, MySqlVersion.MySql57);
				case ProviderName.MySql80MySqlData:
					return GetDataProvider(options, MySqlProvider.MySqlData, MySqlVersion.MySql80);
				case ProviderName.MySql80MySqlConnector:
					return GetDataProvider(options, MySqlProvider.MySqlConnector, MySqlVersion.MySql80);
				case ProviderName.MariaDB10MySqlData:
					return GetDataProvider(options, MySqlProvider.MySqlData, MySqlVersion.MariaDB10);
				case ProviderName.MariaDB10MySqlConnector:
					return GetDataProvider(options, MySqlProvider.MySqlConnector, MySqlVersion.MariaDB10);
				case "MariaDB":
					return GetDataProvider(options, provider, MySqlVersion.MariaDB10);
				case MySqlProviderAdapter.MySqlDataAssemblyName:
				case MySqlProviderAdapter.MySqlConnectorNamespace:
				case MySqlProviderAdapter.MySqlDataClientNamespace:
				case ProviderName.MySql:
					if (options.ConfigurationString?.Contains("5.") == true
						|| options.ProviderName?    .Contains("55") == true
						|| options.ProviderName?    .Contains("56") == true
						|| options.ProviderName?    .Contains("57") == true) return GetDataProvider(options, provider, MySqlVersion.MySql57);
					if (options.ConfigurationString?.Contains("8.") == true
						|| options.ProviderName?    .Contains("80") == true
						|| options.ProviderName?    .Contains("81") == true
						|| options.ProviderName?    .Contains("82") == true
						|| options.ProviderName?    .Contains("83") == true) return GetDataProvider(options, provider, MySqlVersion.MySql80);

					if (options.ProviderName?   .Contains("MariaDB") == true
						|| options.ProviderName?.Contains("10")      == true
						|| options.ProviderName?.Contains("11")      == true) return GetDataProvider(options, provider, MySqlVersion.MariaDB10);

					if (AutoDetectProvider)
					{
						try
						{
							var dv = DetectServerVersion(options, provider);

							return dv != null ? GetDataProvider(options, provider, dv.Value) : null;
						}
						catch
						{
							// ignored
						}
					}

					return GetDataProvider(options, provider, DefaultVersion);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, MySqlProvider provider, MySqlVersion version)
		{
			if (provider == MySqlProvider.AutoDetect)
				provider = DetectProvider();

			return (provider, version) switch
			{
				(_,                            MySqlVersion.AutoDetect) => GetDataProvider(options, provider, DetectServerVersion(options, provider) ?? DefaultVersion),
				(MySqlProvider.MySqlData,      MySqlVersion.MySql57)    => _mySql57DataProvider.Value,
				(MySqlProvider.MySqlData,      MySqlVersion.MySql80)    => _mySql80DataProvider.Value,
				(MySqlProvider.MySqlData,      MySqlVersion.MariaDB10)  => _mariadb10DataProvider.Value,
				(MySqlProvider.MySqlConnector, MySqlVersion.MySql57)    => _mySql57ConnectorDataProvider.Value,
				(MySqlProvider.MySqlConnector, MySqlVersion.MySql80)    => _mySql80ConnectorDataProvider.Value,
				(MySqlProvider.MySqlConnector, MySqlVersion.MariaDB10)  => _mariadb10ConnectorDataProvider.Value,
				_                                                       => _mySql57ConnectorDataProvider.Value,
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

		public override MySqlVersion? DetectServerVersion(DbConnection connection)
		{
			using var cmd = connection.CreateCommand();

			cmd.CommandText = "SELECT VERSION()";
			var versionString = cmd.ExecuteScalar() as string;

			if (versionString == null)
				return null;

			// format
			// MySQL: X.Y.Z[-optionalsuffix]
			// MariaDB: X.Y.Z-MariaDB[-optionalsuffix]

			var isMariaDB = versionString.Contains("-MariaDB");

			var idx = versionString.IndexOf('-');
			if (idx != -1)
				versionString = versionString.Substring(0, idx);

			if (!Version.TryParse(versionString, out var version))
				return null;

			// note that it will also include MariaDB < 10 as pre-10 release of MariaDB is 5.x
			// because they are based on MySQL 5.x this is correct to use MySql57 dialect
			if (version.Major < 8)
				return MySqlVersion.MySql57;

			if (version.Major >= 10 && isMariaDB)
				return MySqlVersion.MariaDB10;

			return MySqlVersion.MySql80;
		}

		protected override DbConnection CreateConnection(MySqlProvider provider, string connectionString)
		{
			return MySqlProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
