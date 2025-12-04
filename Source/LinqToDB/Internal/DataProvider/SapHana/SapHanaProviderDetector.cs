using System;
using System.Data.Common;
using System.IO;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SapHana;
using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	public sealed class SapHanaProviderDetector() : ProviderDetectorBase<SapHanaProvider>()
	{
		static readonly Lazy<IDataProvider> _hanaDataProvider     = CreateDataProvider<SapHanaNativeDataProvider>();
		static readonly Lazy<IDataProvider> _hanaOdbcDataProvider = CreateDataProvider<SapHanaOdbcDataProvider>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			if (options.ConnectionString?.IndexOf("HDBODBC", StringComparison.OrdinalIgnoreCase) >= 0)
				return _hanaOdbcDataProvider.Value;

			// don't merge DetectProvider and DetectProvider logic and later could return inconclusive
			switch (options.ProviderName)
			{
				case SapHanaProviderAdapter.UnmanagedClientNamespace:
				case "Sap.Data.Hana.v4.5"                           :
				case "Sap.Data.Hana.Core"                           :
				case "Sap.Data.Hana.Core.v2.1"                      :
				case "Sap.Data.Hana.Net"                            :
				case "Sap.Data.Hana.Net.v6.0"                       :
				case "Sap.Data.Hana.Net.v8.0"                       :
				case ProviderName.SapHanaNative                     : return _hanaDataProvider.Value;
				case ProviderName.SapHanaOdbc                       : return _hanaOdbcDataProvider.Value;
				case ""                                             :
				case null                                           :
					if (options.ConfigurationString?.Contains("Hana") == true)
						goto case ProviderName.SapHana;
					break;
				case ProviderName.SapHana                           :
					if (options.ConfigurationString?.IndexOf("ODBC", StringComparison.OrdinalIgnoreCase) >= 0)
						return _hanaOdbcDataProvider.Value;

					return GetDataProvider(options, DetectProvider(options, SapHanaProvider.AutoDetect), default);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, SapHanaProvider provider, NoDialect version)
		{
			provider = DetectProvider(options, provider);

			return provider switch
			{
				SapHanaProvider.Unmanaged => _hanaDataProvider.Value,
				_                         => _hanaOdbcDataProvider.Value,
			};
		}

		protected override DbConnection CreateConnection(SapHanaProvider provider, string connectionString)
		{
			return SapHanaProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
		protected override SapHanaProvider DetectProvider(ConnectionOptions options, SapHanaProvider provider)
		{
			if (provider is SapHanaProvider.ODBC or SapHanaProvider.Unmanaged)
				return provider;

			switch (options.ProviderName)
			{
				case SapHanaProviderAdapter.UnmanagedClientNamespace:
				case "Sap.Data.Hana.v4.5"                           :
				case "Sap.Data.Hana.Core"                           :
				case "Sap.Data.Hana.Core.v2.1"                      :
				case "Sap.Data.Hana.Net"                            :
				case "Sap.Data.Hana.Net.v6.0"                       :
				case "Sap.Data.Hana.Net.v8.0"                       :
				case ProviderName.SapHanaNative                     :
					return SapHanaProvider.Unmanaged;

				case OdbcProviderAdapter.ClientNamespace            :
				case ProviderName.SapHanaOdbc                       :
					return SapHanaProvider.ODBC;
			}

			if (options.ConfigurationString?.IndexOf("ODBC", StringComparison.OrdinalIgnoreCase) >= 0)
				return SapHanaProvider.ODBC;

			var fileName = typeof(SapHanaProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			foreach (var assemblyName in SapHanaProviderAdapter.UnmanagedAssemblyNames)
			{
				if (File.Exists(Path.Combine(dirName ?? ".", $"{assemblyName}.dll")))
					return SapHanaProvider.Unmanaged;
			}

			return SapHanaProvider.ODBC;
		}
	}
}
