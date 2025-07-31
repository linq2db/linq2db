using System;
using System.Data.Common;
using System.IO;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Sybase;
using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.DataProvider.Sybase
{
	public class SybaseProviderDetector : ProviderDetectorBase<SybaseProvider>
	{
		public SybaseProviderDetector() : base()
		{
		}

		static readonly Lazy<IDataProvider> _sybaseNativeDataProvider  = CreateDataProvider<SybaseDataProviderNative>();
		static readonly Lazy<IDataProvider> _sybaseManagedDataProvider = CreateDataProvider<SybaseDataProviderManaged>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			var provider = options.ProviderName switch
			{
				SybaseProviderAdapter.ManagedClientNamespace => SybaseProvider.DataAction,
				SybaseProviderAdapter.NativeClientNamespace  => SybaseProvider.Unmanaged,
				_                                            => DetectProvider()
			};

			switch (options.ProviderName)
			{
				case SybaseProviderAdapter.ManagedClientNamespace:
				case ProviderName.SybaseManaged                  : return _sybaseManagedDataProvider.Value;
				case "Sybase.Native"                             :
				case SybaseProviderAdapter.NativeClientNamespace :
				case SybaseProviderAdapter.NativeAssemblyName    : return _sybaseNativeDataProvider.Value;
				case ""                                          :
				case null                                        :
					if (options.ConfigurationString?.Contains("Sybase") == true)
						goto case ProviderName.Sybase;
					break;
				case ProviderName.Sybase                         :
					if (options.ConfigurationString?.Contains("Managed") == true)
						return _sybaseManagedDataProvider.Value;
					if (options.ConfigurationString?.Contains("Native") == true)
						return _sybaseNativeDataProvider.Value;
					return GetDataProvider(options, provider, default);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, SybaseProvider provider, NoDialect version)
		{
			if (provider == SybaseProvider.AutoDetect)
				provider = DetectProvider();

			return provider switch
			{
				SybaseProvider.Unmanaged => _sybaseNativeDataProvider.Value,
				_                        => _sybaseManagedDataProvider.Value,
			};
		}

		public static SybaseProvider DetectProvider()
		{
			var fileName = typeof(SybaseProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", SybaseProviderAdapter.NativeAssemblyName + ".dll"))
				? SybaseProvider.Unmanaged
				: SybaseProvider.DataAction;
		}

		protected override DbConnection CreateConnection(SybaseProvider provider, string connectionString)
		{
			return SybaseProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
