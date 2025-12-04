using System;
using System.Data.Common;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.DB2;

namespace LinqToDB.Internal.DataProvider.DB2
{
	public class DB2ProviderDetector() : ProviderDetectorBase<DB2ProviderDetector.Provider, DB2Version>(DB2Version.AutoDetect, DB2Version.LUW)
	{
		public enum Provider { }

		static readonly Lazy<IDataProvider> _db2DataProviderzOS = CreateDataProvider<DB2zOSDataProvider>();
		static readonly Lazy<IDataProvider> _db2DataProviderLUW = CreateDataProvider<DB2LUWDataProvider>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			// DB2 ODS provider could be used by Informix
			if (options.ConfigurationString?.Contains("Informix") == true)
				return null;

			switch (options.ProviderName)
			{
				case ProviderName.DB2LUW: return _db2DataProviderLUW.Value;
				case ProviderName.DB2zOS: return _db2DataProviderzOS.Value;

				case "":
				case null:

					if (options.ConfigurationString == "DB2")
						goto case ProviderName.DB2;
					break;

				case ProviderName.DB2:
				case DB2ProviderAdapter.NetFxClientNamespace:
				case DB2ProviderAdapter.CoreClientNamespace:

					if (options.ConfigurationString?.Contains("LUW") == true)
						return _db2DataProviderLUW.Value;
					if (options.ConfigurationString?.Contains("z/OS") == true || options.ConfigurationString?.Contains("zOS") == true)
						return _db2DataProviderzOS.Value;

					if (AutoDetectProvider)
					{
						try
						{
							var version = DetectServerVersion(options, default);

							return version != null ? GetDataProvider(options, default, version.Value) : null;
						}
						catch
						{
							return _db2DataProviderLUW.Value;
						}
					}

					return GetDataProvider(options, default, DefaultVersion);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, Provider provider, DB2Version version)
		{
			return version switch
			{
				DB2Version.AutoDetect => GetDataProvider(options, default, DetectServerVersion(options, default) ?? DefaultVersion),
				DB2Version.LUW        => _db2DataProviderLUW.Value,
				DB2Version.zOS        => _db2DataProviderzOS.Value,
				_                     => _db2DataProviderLUW.Value,
			};
		}

		protected override DB2Version? DetectServerVersion(DbConnection connection, DbTransaction? transaction)
		{
			return DB2ProviderAdapter.Instance.ConnectionWrapper(connection).eServerType switch
			{
				DB2ProviderAdapter.DB2ServerTypes.DB2_390 => DB2Version.zOS,
				_                                         => DB2Version.LUW
			};
		}

		protected override DbConnection CreateConnection(Provider provider, string connectionString)
		{
			return DB2ProviderAdapter.Instance.CreateConnection(connectionString);
		}
	}
}
