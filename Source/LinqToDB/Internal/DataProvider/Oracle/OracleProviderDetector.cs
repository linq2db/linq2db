using System;
using System.Data.Common;
using System.Globalization;
using System.IO;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	sealed class OracleProviderDetector : ProviderDetectorBase<OracleProvider,OracleVersion>
	{
		public OracleProviderDetector() : base(OracleVersion.AutoDetect, OracleVersion.v12)
		{
		}

		static readonly Lazy<IDataProvider> _oracleNativeDataProvider11  = CreateDataProvider<OracleDataProviderNative11>();
		static readonly Lazy<IDataProvider> _oracleNativeDataProvider12  = CreateDataProvider<OracleDataProviderNative12>();

		static readonly Lazy<IDataProvider> _oracleManagedDataProvider11 = CreateDataProvider<OracleDataProviderManaged11>();
		static readonly Lazy<IDataProvider> _oracleManagedDataProvider12 = CreateDataProvider<OracleDataProviderManaged12>();

		static readonly Lazy<IDataProvider> _oracleDevartDataProvider11  = CreateDataProvider<OracleDataProviderDevart11>();
		static readonly Lazy<IDataProvider> _oracleDevartDataProvider12  = CreateDataProvider<OracleDataProviderDevart12>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			var provider = options.ProviderName switch
			{
				OracleProviderAdapter.ManagedClientNamespace => OracleProvider.Managed,
				OracleProviderAdapter.DevartClientNamespace  => OracleProvider.Devart,
				OracleProviderAdapter.NativeClientNamespace  => OracleProvider.Native,
				_                                            => DetectProvider(options.ConnectionString)
			};

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

					if (options.ConfigurationString?.Contains("Oracle") == true)
						goto case ProviderName.Oracle;
					break;
				case ProviderName.Oracle                         :
					if (provider == OracleProvider.AutoDetect)
					{
						if (options.ConfigurationString?.Contains("Native") == true || options.ProviderName?.Contains("Native") == true)
							provider = OracleProvider.Native;
						else if (options.ConfigurationString?.Contains("Devart") == true || options.ProviderName?.Contains("Devart") == true)
							provider = OracleProvider.Devart;
						else
							provider = OracleProvider.Managed;
					}

					if (options.ConfigurationString?.Contains("11") == true || options.ProviderName?.Contains("11") == true) return GetDataProvider(options, provider, OracleVersion.v11);
					if (options.ConfigurationString?.Contains("12") == true || options.ProviderName?.Contains("12") == true) return GetDataProvider(options, provider, OracleVersion.v12);
					if (options.ConfigurationString?.Contains("18") == true || options.ProviderName?.Contains("18") == true) return GetDataProvider(options, provider, OracleVersion.v12);
					if (options.ConfigurationString?.Contains("19") == true || options.ProviderName?.Contains("19") == true) return GetDataProvider(options, provider, OracleVersion.v12);
					if (options.ConfigurationString?.Contains("21") == true || options.ProviderName?.Contains("21") == true) return GetDataProvider(options, provider, OracleVersion.v12);

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

		public override IDataProvider GetDataProvider(ConnectionOptions options, OracleProvider provider, OracleVersion version)
		{
			if (provider == OracleProvider.AutoDetect)
			{
				var canBeDevart = options.ConnectionString?.IndexOf("SERVER", StringComparison.OrdinalIgnoreCase) != -1;
				var canBeOracle = options.ConnectionString?.IndexOf("DATA SOURCE", StringComparison.OrdinalIgnoreCase) != -1;
				provider = DetectProvider(options.ConnectionString);
			}

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

		private static OracleProvider DetectProvider(string? connectionString)
		{
			// as connection string for DevArt has own (and actually more sane) format
			// we cannot try to use incompatible provider
			var canBeDevart = connectionString?.IndexOf("SERVER", StringComparison.OrdinalIgnoreCase) != -1;
			var canBeOracle = connectionString?.IndexOf("DATA SOURCE", StringComparison.OrdinalIgnoreCase) != -1;

			var fileName = typeof(OracleProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			if (canBeOracle && File.Exists(Path.Combine(dirName ?? ".", OracleProviderAdapter.ManagedAssemblyName + ".dll")))
				return OracleProvider.Managed;

			if (canBeDevart && File.Exists(Path.Combine(dirName ?? ".", OracleProviderAdapter.DevartAssemblyName + ".dll")))
				return OracleProvider.Devart;

			if (canBeOracle && File.Exists(Path.Combine(dirName ?? ".", OracleProviderAdapter.NativeAssemblyName + ".dll")))
				return OracleProvider.Native;

			return canBeOracle ? OracleProvider.Managed : OracleProvider.Devart;
		}

		public override OracleVersion? DetectServerVersion(DbConnection connection)
		{
			var command = connection.CreateCommand();

			command.CommandText = "SELECT VERSION from PRODUCT_COMPONENT_VERSION WHERE ROWNUM = 1";

			if (command.ExecuteScalar() is string result)
			{
				var version = int.Parse(result.Split('.')[0], NumberStyles.Integer, NumberFormatInfo.InvariantInfo);

				if (version <= 11)
					return OracleVersion.v11;

				return OracleVersion.v12;
			}

			return DefaultVersion;
		}

		protected override DbConnection CreateConnection(OracleProvider provider, string connectionString)
		{
			if (provider == OracleProvider.AutoDetect)
				provider = DetectProvider(connectionString);

			return OracleProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
