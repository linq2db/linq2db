using System;
using System.Data.Common;
using System.IO;

using LinqToDB.Common;
using LinqToDB.Data;

namespace LinqToDB.DataProvider.SapHana
{
	sealed class SapHanaProviderDetector : ProviderDetectorBase<SapHanaProvider, SapHanaProviderDetector.Dialect>
	{
		internal enum Dialect { }

		public SapHanaProviderDetector() : base()
		{
		}

		static readonly Lazy<IDataProvider> _hanaDataProvider     = CreateDataProvider<SapHanaNativeDataProvider>();
		static readonly Lazy<IDataProvider> _hanaOdbcDataProvider = CreateDataProvider<SapHanaOdbcDataProvider>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			if (options.ConnectionString?.IndexOf("HDBODBC", StringComparison.OrdinalIgnoreCase) >= 0)
				return _hanaOdbcDataProvider.Value;

			var provider = options.ProviderName switch
			{
				SapHanaProviderAdapter.UnmanagedClientNamespace => SapHanaProvider.Unmanaged,
				OdbcProviderAdapter.ClientNamespace             => SapHanaProvider.ODBC,
				_                                               => DetectProvider()
			};

			switch (options.ProviderName)
			{
				case SapHanaProviderAdapter.UnmanagedClientNamespace:
				case "Sap.Data.Hana.v4.5"                  :
				case "Sap.Data.Hana.Core"                  :
				case "Sap.Data.Hana.Core.v2.1"             :
				case "Sap.Data.Hana.Net"                   :
				case "Sap.Data.Hana.Net.v6.0"              :
				case "Sap.Data.Hana.Net.v8.0"              :
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

			foreach (var assemblyName in SapHanaProviderAdapter.UnmanagedAssemblyNames)
			{
				if (File.Exists(Path.Combine(dirName ?? ".", $"{assemblyName}.dll")))
					return SapHanaProvider.Unmanaged;
			}

			return SapHanaProvider.ODBC;
		}

		public override Dialect? DetectServerVersion(DbConnection connection)
		{
			return default(Dialect);
		}

		protected override DbConnection CreateConnection(SapHanaProvider provider, string connectionString)
		{
			return SapHanaProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
