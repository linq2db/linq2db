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
	public class OracleProviderDetector() : ProviderDetectorBase<OracleProvider,OracleVersion>(OracleVersion.AutoDetect, OracleVersion.v12)
	{
		static readonly Lazy<IDataProvider> _oracleNativeDataProvider11  = CreateDataProvider<OracleDataProviderNative11>();
		static readonly Lazy<IDataProvider> _oracleNativeDataProvider12  = CreateDataProvider<OracleDataProviderNative12>();

		static readonly Lazy<IDataProvider> _oracleManagedDataProvider11 = CreateDataProvider<OracleDataProviderManaged11>();
		static readonly Lazy<IDataProvider> _oracleManagedDataProvider12 = CreateDataProvider<OracleDataProviderManaged12>();

		static readonly Lazy<IDataProvider> _oracleDevartDataProvider11  = CreateDataProvider<OracleDataProviderDevart11>();
		static readonly Lazy<IDataProvider> _oracleDevartDataProvider12  = CreateDataProvider<OracleDataProviderDevart12>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			// don't merge this method and DetectProvider(provider type) logic because this method could return null
			// and other method returns default provider type
			switch (options.ProviderName)
			{
				case ""                                          :
				case null                                        :

					if (options.ConfigurationString?.Contains("Oracle", StringComparison.Ordinal) == true)
						goto case ProviderName.Oracle;
					break;
				case OracleProviderAdapter.NativeAssemblyName    :
				case OracleProviderAdapter.NativeClientNamespace :
				case ProviderName.OracleNative                   :
				case ProviderName.Oracle11Native                 :
				case OracleProviderAdapter.DevartAssemblyName    :
				case ProviderName.OracleDevart                   :
				case ProviderName.Oracle11Devart                 :
				case OracleProviderAdapter.ManagedAssemblyName   :
				case OracleProviderAdapter.ManagedClientNamespace:
				case "Oracle.ManagedDataAccess.Core"             :
				case ProviderName.OracleManaged                  :
				case ProviderName.Oracle11Managed                :
				case ProviderName.Oracle                         :
					var provider = DetectProvider(options, OracleProvider.AutoDetect);

					if (options.ConfigurationString?.Contains("11", StringComparison.Ordinal) == true || options.ProviderName?.Contains("11", StringComparison.Ordinal) == true) return GetDataProvider(options, provider, OracleVersion.v11);
					if (options.ConfigurationString?.Contains("12", StringComparison.Ordinal) == true || options.ProviderName?.Contains("12", StringComparison.Ordinal) == true) return GetDataProvider(options, provider, OracleVersion.v12);
					if (options.ConfigurationString?.Contains("18", StringComparison.Ordinal) == true || options.ProviderName?.Contains("18", StringComparison.Ordinal) == true) return GetDataProvider(options, provider, OracleVersion.v12);
					if (options.ConfigurationString?.Contains("19", StringComparison.Ordinal) == true || options.ProviderName?.Contains("19", StringComparison.Ordinal) == true) return GetDataProvider(options, provider, OracleVersion.v12);
					if (options.ConfigurationString?.Contains("21", StringComparison.Ordinal) == true || options.ProviderName?.Contains("21", StringComparison.Ordinal) == true) return GetDataProvider(options, provider, OracleVersion.v12);

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
			provider = DetectProvider(options, provider);

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

		protected override OracleVersion? DetectServerVersion(DbConnection connection, DbTransaction? transaction)
		{
			var command = connection.CreateCommand();

			command.CommandText = "SELECT VERSION from PRODUCT_COMPONENT_VERSION WHERE ROWNUM = 1";

			if (command.ExecuteScalar() is string result)
			{
				var version = int.Parse(result.Split('.')[0], NumberStyles.Integer, NumberFormatInfo.InvariantInfo);

				return version switch
				{
					<= 11 => OracleVersion.v11,
					_ => OracleVersion.v12,
				};
			}

			return DefaultVersion;
		}

		protected override DbConnection CreateConnection(OracleProvider provider, string connectionString)
		{
			return OracleProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}

		protected override OracleProvider DetectProvider(ConnectionOptions options, OracleProvider provider)
		{
			if (provider is OracleProvider.Devart or OracleProvider.Managed or OracleProvider.Native)
				return provider;

			switch (options.ProviderName)
			{
				case OracleProviderAdapter.NativeAssemblyName    :
				case OracleProviderAdapter.NativeClientNamespace :
				case ProviderName.OracleNative                   :
				case ProviderName.Oracle11Native                 :
					return OracleProvider.Native;

				case OracleProviderAdapter.DevartAssemblyName    :
				case ProviderName.OracleDevart                   :
				case ProviderName.Oracle11Devart                 :
					return OracleProvider.Devart;

				case OracleProviderAdapter.ManagedAssemblyName   :
				case OracleProviderAdapter.ManagedClientNamespace:
				case "Oracle.ManagedDataAccess.Core"             :
				case ProviderName.OracleManaged                  :
				case ProviderName.Oracle11Managed                :
					return OracleProvider.Managed;
			}

			if (options.ConfigurationString?.Contains("Native", StringComparison.Ordinal) == true || options.ProviderName?.Contains("Native", StringComparison.Ordinal) == true)
				return OracleProvider.Native;
			else if (options.ConfigurationString?.Contains("Devart", StringComparison.Ordinal) == true || options.ProviderName?.Contains("Devart", StringComparison.Ordinal) == true)
				return OracleProvider.Devart;

			// as connection string for DevArt has own (and actually more sane) format
			// we cannot try to use incompatible provider
			var canBeDevart = options.ConnectionString?.IndexOf("SERVER", StringComparison.OrdinalIgnoreCase) != -1;
			var canBeOracle = options.ConnectionString?.IndexOf("DATA SOURCE", StringComparison.OrdinalIgnoreCase) != -1;

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
	}
}
