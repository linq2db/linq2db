﻿using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.Oracle
{
	using Configuration;
	using Data;

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

		public override IDataProvider? DetectProvider(IConnectionStringSettings css, string connectionString)
		{
			OracleProvider? provider = null;

			switch (css.ProviderName)
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

					if (css.Name.Contains("Oracle"))
						goto case ProviderName.Oracle;
					break;
				case ProviderName.Oracle                         :
					if (provider == null)
					{
						if (css.Name.Contains("Native") || css.ProviderName?.Contains("Native") == true)
							provider = OracleProvider.Native;
						else if (css.Name.Contains("Devart") || css.ProviderName?.Contains("Devart") == true)
							provider = OracleProvider.Devart;
						else
							provider = OracleProvider.Managed;
					}

					if (css.Name.Contains("11") || css.ProviderName?.Contains("11") == true) return GetDataProvider(provider.Value, OracleVersion.v11, null);
					if (css.Name.Contains("12") || css.ProviderName?.Contains("12") == true) return GetDataProvider(provider.Value, OracleVersion.v12, null);
					if (css.Name.Contains("18") || css.ProviderName?.Contains("18") == true) return GetDataProvider(provider.Value, OracleVersion.v12, null);
					if (css.Name.Contains("19") || css.ProviderName?.Contains("19") == true) return GetDataProvider(provider.Value, OracleVersion.v12, null);
					if (css.Name.Contains("21") || css.ProviderName?.Contains("21") == true) return GetDataProvider(provider.Value, OracleVersion.v12, null);

					if (AutoDetectProvider)
					{
						try
						{
							var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;
							var dv = DetectServerVersion(provider.Value, cs);

							return dv != null ? GetDataProvider(provider.Value, dv.Value, connectionString) : null;
						}
						catch
						{
							// ignored
						}
					}

					return GetDataProvider(provider.Value, DefaultVersion, connectionString);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(OracleProvider provider, OracleVersion version, string? connectionString)
		{
			return (provider, version) switch
			{
				(_,                      OracleVersion.AutoDetect) => AutoDetectProvider(),
				(OracleProvider.Native , OracleVersion.v11)        => _oracleNativeDataProvider11 .Value,
				(OracleProvider.Native , OracleVersion.v12)        => _oracleNativeDataProvider12 .Value,
				(OracleProvider.Managed, OracleVersion.v11)        => _oracleManagedDataProvider11.Value,
				(OracleProvider.Managed, OracleVersion.v12)        => _oracleManagedDataProvider12.Value,
				(OracleProvider.Devart , OracleVersion.v11)        => _oracleDevartDataProvider11 .Value,
				(OracleProvider.Devart , OracleVersion.v12)        => _oracleDevartDataProvider12 .Value,
				_                                                  => _oracleManagedDataProvider12.Value,
			};

			IDataProvider AutoDetectProvider()
			{
				if (connectionString == null)
					throw new InvalidOperationException("Connection string is not provided.");

				return GetDataProvider(provider, DetectServerVersion(provider, connectionString) ?? DefaultVersion, null);
			}
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
