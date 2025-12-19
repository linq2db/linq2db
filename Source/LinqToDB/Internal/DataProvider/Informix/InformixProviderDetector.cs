using System;
using System.Data.Common;
using System.IO;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Informix;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.DB2;

namespace LinqToDB.Internal.DataProvider.Informix
{
	public class InformixProviderDetector : ProviderDetectorBase<InformixProvider>
	{
		static readonly Lazy<IDataProvider> _informixDataProvider    = CreateDataProvider<InformixDataProviderInformix>();
		static readonly Lazy<IDataProvider> _informixDB2DataProvider = CreateDataProvider<InformixDataProviderDB2>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			// don't merge this method and DetectProvider(provider type) logic because this method could return null
			// and other method returns default provider type
			switch (options.ProviderName)
			{
				case ProviderName.InformixDB2                  :
					return _informixDB2DataProvider.Value;
				case InformixProviderAdapter.IfxClientNamespace:
					return _informixDataProvider.Value;
				case ""                                        :
				case null                                      :
				case DB2ProviderAdapter.NetFxClientNamespace   :
				case DB2ProviderAdapter.CoreClientNamespace    :

					// this check used by both Informix and DB2 providers to avoid conflicts
					if (options.ConfigurationString?.Contains("Informix", StringComparison.Ordinal) == true)
						goto case ProviderName.Informix;
					break;
				case ProviderName.Informix:
					if (options.ConfigurationString?.Contains("DB2", StringComparison.Ordinal) == true)
						return _informixDB2DataProvider.Value;

#if NETFRAMEWORK
					return _informixDataProvider.Value;
#else
					return _informixDB2DataProvider.Value;
#endif
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, InformixProvider provider, NoDialect version)
		{
			provider = DetectProvider(options, provider);

			return provider switch
			{
				InformixProvider.Informix => _informixDataProvider.Value,
				_                         => _informixDB2DataProvider.Value,
			};
		}

		protected override DbConnection CreateConnection(InformixProvider provider, string connectionString)
		{
			return InformixProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}

		protected override InformixProvider DetectProvider(ConnectionOptions options, InformixProvider provider)
		{
			if (provider is InformixProvider.DB2 or InformixProvider.Informix)
				return provider;

			switch (options.ProviderName)
			{
#if !NETFRAMEWORK
				case DB2ProviderAdapter.ClientNamespaceOld
					when options.ProviderName is not null      :
				case DB2ProviderAdapter.ClientNamespace        :
#endif
				case DB2ProviderAdapter.NetFxClientNamespace   :
				case DB2ProviderAdapter.CoreClientNamespace    :
				case ProviderName.InformixDB2                  :
					return InformixProvider.DB2;

				case InformixProviderAdapter.IfxClientNamespace:
					return InformixProvider.Informix;

				default:
					if (options.ConfigurationString?.Contains("DB2", StringComparison.Ordinal) == true)
						return InformixProvider.DB2;

					break;
			}

			var fileName = typeof(InformixProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", InformixProviderAdapter.IfxAssemblyName + ".dll"))
				? InformixProvider.Informix
#if !NETFRAMEWORK
				: InformixProvider.DB2;
#else
				: File.Exists(Path.Combine(dirName ?? ".", DB2ProviderAdapter.AssemblyName + ".dll"))
					? InformixProvider.DB2
					: InformixProvider.Informix;
#endif
		}
	}
}
