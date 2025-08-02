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
		public InformixProviderDetector() : base()
		{
		}

		static readonly Lazy<IDataProvider> _informixDataProvider    = CreateDataProvider<InformixDataProviderInformix>();
		static readonly Lazy<IDataProvider> _informixDB2DataProvider = CreateDataProvider<InformixDataProviderDB2>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			var provider = options.ProviderName switch
			{
				InformixProviderAdapter.IfxClientNamespace                                  => InformixProvider.Informix,
				DB2ProviderAdapter.ClientNamespace                                          => InformixProvider.DB2,
#if !NETFRAMEWORK
				DB2ProviderAdapter.ClientNamespaceOld when options.ProviderName is not null => InformixProvider.DB2,
#endif
				_                                                                           => DetectProvider()
			};

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
					if (options.ConfigurationString?.Contains("Informix") == true)
						goto case ProviderName.Informix;
					break;
				case ProviderName.Informix:
					if (options.ConfigurationString?.Contains("DB2") == true)
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
			if (provider == InformixProvider.AutoDetect)
				provider = DetectProvider();

			return provider switch
			{
				InformixProvider.Informix => _informixDataProvider.Value,
				_                         => _informixDB2DataProvider.Value,
			};
		}

		public static InformixProvider DetectProvider()
		{
			var fileName = typeof(InformixProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", InformixProviderAdapter.IfxAssemblyName + ".dll"))
				? InformixProvider.Informix
				: InformixProvider.DB2;
		}

		protected override DbConnection CreateConnection(InformixProvider provider, string connectionString)
		{
			return InformixProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
