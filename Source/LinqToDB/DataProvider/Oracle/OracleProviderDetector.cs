using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.Oracle
{
	using Configuration;
	using Data;
	using Extensions;

	sealed class OracleProviderDetector : ProviderDetectorBase<OracleProvider,OracleVersion,DbConnection>
	{
		public OracleProviderDetector() : base(OracleVersion.AutoDetect, OracleVersion.v12)
		{
		}

		static readonly Lazy<IDataProvider> _oracleNativeDataProvider11  = DataConnection.CreateDataProvider<OracleDataProviderNative11>();
		static readonly Lazy<IDataProvider> _oracleNativeDataProvider12  = DataConnection.CreateDataProvider<OracleDataProviderNative12>();

		static readonly Lazy<IDataProvider> _oracleManagedDataProvider11 = DataConnection.CreateDataProvider<OracleDataProviderManaged11>();
		static readonly Lazy<IDataProvider> _oracleManagedDataProvider12 = DataConnection.CreateDataProvider<OracleDataProviderManaged12>();

		static readonly Lazy<IDataProvider> _oracleDevartDataProvider11  = DataConnection.CreateDataProvider<OracleDataProviderDevart11>();
		static readonly Lazy<IDataProvider> _oracleDevartDataProvider12  = DataConnection.CreateDataProvider<OracleDataProviderDevart12>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			OracleProvider? provider = null;

			switch (options.ProviderName)
			{
				case OracleProviderAdapter.NativeAssemblyName    :
				case OracleProviderAdapter.NativeClientNamespace :
				case ProviderName.OracleNative                   :
				case ProviderName.Oracle11Native                 :
					provider = OracleProvider.Native;
					goto case ProviderName.Oracle;
				case OracleProviderAdapter.DevartAssemblyName    :
				case ProviderName.OracleDevart                   :
				case ProviderName.Oracle11Devart                 :
					provider = OracleProvider.Devart;
					goto case ProviderName.Oracle;
				case OracleProviderAdapter.ManagedAssemblyName   :
				case OracleProviderAdapter.ManagedClientNamespace:
				case "Oracle.ManagedDataAccess.Core"             :
				case ProviderName.OracleManaged                  :
				case ProviderName.Oracle11Managed                :
					provider = OracleProvider.Managed;
					goto case ProviderName.Oracle;
				case ""                                          :
				case null                                        :

					if (options.ConfigurationString?.ContainsEx("Oracle") == true)
						goto case ProviderName.Oracle;
					break;
				case ProviderName.Oracle                         :
					if (provider == null)
					{
						if (options.ConfigurationString?.ContainsEx("Native") == true || options.ProviderName?.ContainsEx("Native") == true)
							provider = OracleProvider.Native;
						else if (options.ConfigurationString?.ContainsEx("Devart") == true || options.ProviderName?.ContainsEx("Devart") == true)
							provider = OracleProvider.Devart;
						else
							provider = OracleProvider.Managed;
					}

					if (options.ConfigurationString?.ContainsEx("11") == true || options.ProviderName?.ContainsEx("11") == true) return GetDataProvider(options, provider.Value, OracleVersion.v11);
					if (options.ConfigurationString?.ContainsEx("12") == true || options.ProviderName?.ContainsEx("12") == true) return GetDataProvider(options, provider.Value, OracleVersion.v12);
					if (options.ConfigurationString?.ContainsEx("18") == true || options.ProviderName?.ContainsEx("18") == true) return GetDataProvider(options, provider.Value, OracleVersion.v12);
					if (options.ConfigurationString?.ContainsEx("19") == true || options.ProviderName?.ContainsEx("19") == true) return GetDataProvider(options, provider.Value, OracleVersion.v12);
					if (options.ConfigurationString?.ContainsEx("21") == true || options.ProviderName?.ContainsEx("21") == true) return GetDataProvider(options, provider.Value, OracleVersion.v12);

					if (AutoDetectProvider)
					{
						try
						{
							var dv = DetectServerVersion(options, provider.Value);

							return dv != null ? GetDataProvider(options, provider.Value, dv.Value) : null;
						}
						catch
						{
							// ignored
						}
					}

					return GetDataProvider(options, provider.Value, DefaultVersion);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, OracleProvider provider, OracleVersion version)
		{
			return (provider, version) switch
			{
				(_,                      OracleVersion.AutoDetect) => GetDataProvider(options, provider, DetectServerVersion(options, provider) ?? DefaultVersion),
				(OracleProvider.Native , OracleVersion.v11)        => _oracleNativeDataProvider11 .Value,
				(OracleProvider.Native , OracleVersion.v12)        => _oracleNativeDataProvider12 .Value,
				(OracleProvider.Managed, OracleVersion.v11)        => _oracleManagedDataProvider11.Value,
				(OracleProvider.Managed, OracleVersion.v12)        => _oracleManagedDataProvider12.Value,
				(OracleProvider.Devart , OracleVersion.v11)        => _oracleDevartDataProvider11 .Value,
				(OracleProvider.Devart , OracleVersion.v12)        => _oracleDevartDataProvider12 .Value,
				_                                                  => _oracleManagedDataProvider12.Value,
			};
		}

		public override OracleVersion? DetectServerVersion(DbConnection connection)
		{
			var command = connection.CreateCommand();

			command.CommandText = "SELECT VERSION from PRODUCT_COMPONENT_VERSION WHERE ROWNUM = 1";

			if (command.ExecuteScalar() is string result)
			{
				var version = int.Parse(result.Split('.')[0]);

				if (version <= 11)
					return OracleVersion.v11;

				return OracleVersion.v12;
			}

			return DefaultVersion;
		}

		protected override DbConnection CreateConnection(OracleProvider provider, string connectionString)
		{
			return OracleProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
