﻿using System;
using System.Data.Common;
using System.IO;

namespace LinqToDB.DataProvider.SapHana
{
	using Common;
	using Data;

	sealed class SapHanaProviderDetector : ProviderDetectorBase<SapHanaProvider, SapHanaProviderDetector.Dialect, DbConnection>
	{
		internal enum Dialect { }

		public SapHanaProviderDetector() : base(default, default)
		{
		}

		static readonly Lazy<IDataProvider> _hanaDataProvider     = DataConnection.CreateDataProvider<SapHanaDataProvider>();
		static readonly Lazy<IDataProvider> _hanaOdbcDataProvider = DataConnection.CreateDataProvider<SapHanaOdbcDataProvider>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			if (options.ConnectionString?.IndexOf("HDBODBC", StringComparison.OrdinalIgnoreCase) >= 0)
				return _hanaOdbcDataProvider.Value;

			var provider = options.ProviderName switch
			{
				SapHanaProviderAdapter.ClientNamespace => SapHanaProvider.Unmanaged,
				OdbcProviderAdapter.ClientNamespace    => SapHanaProvider.ODBC,
				_                                      => DetectProvider()
			};

			switch (options.ProviderName)
			{
				case SapHanaProviderAdapter.ClientNamespace:
				case "Sap.Data.Hana.v4.5"                  :
				case "Sap.Data.Hana.Core"                  :
				case "Sap.Data.Hana.Core.v2.1"             :
				case ProviderName.SapHanaNative            : return _hanaDataProvider.Value;
				case ProviderName.SapHanaOdbc              : return _hanaOdbcDataProvider.Value;
				case ""                                    :
				case null                                  :
					if (options.ConfigurationString?.Contains("Hana") == true)
						goto case ProviderName.SapHana;
					break;
				case ProviderName.SapHana                  :
					if (options.ConfigurationString?.IndexOf("ODBC", StringComparison.OrdinalIgnoreCase) >= 0)
						return _hanaOdbcDataProvider.Value;

					return GetDataProvider(options, provider, default);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, SapHanaProvider provider, Dialect version)
		{
			if (provider == SapHanaProvider.AutoDetect)
				provider = DetectProvider();

			return provider switch
			{
				SapHanaProvider.Unmanaged => _hanaDataProvider.Value,
				_                         => _hanaOdbcDataProvider.Value,
			};
		}

		public static SapHanaProvider DetectProvider()
		{
			var fileName = typeof(SapHanaProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", SapHanaProviderAdapter.AssemblyName + ".dll"))
				? SapHanaProvider.Unmanaged
				: SapHanaProvider.ODBC;
		}

		public override Dialect? DetectServerVersion(DbConnection connection)
		{
			return default(Dialect);
		}

		protected override DbConnection CreateConnection(SapHanaProvider provider, string connectionString)
		{
			return (provider == SapHanaProvider.Unmanaged
				? (IConnectionWrapper)SapHanaProviderAdapter.GetInstance().CreateConnection(connectionString)
				: OdbcProviderAdapter.GetInstance().CreateConnection(connectionString)).Connection;
		}
	}
}
