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
		static readonly Lazy<IDataProvider> _sybaseNativeDataProvider  = CreateDataProvider<SybaseDataProviderNative>();
		static readonly Lazy<IDataProvider> _sybaseManagedDataProvider = CreateDataProvider<SybaseDataProviderManaged>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			// don't merge this method and DetectProvider(provider type) logic because this method could return null
			// and other method returns default provider type
			switch (options.ProviderName)
			{
				case SybaseProviderAdapter.ManagedClientNamespace:
				case ProviderName.SybaseManaged                  : return _sybaseManagedDataProvider.Value;
				case "Sybase.Native"                             :
				case SybaseProviderAdapter.NativeClientNamespace :
				case SybaseProviderAdapter.NativeAssemblyName    : return _sybaseNativeDataProvider.Value;
				case ""                                          :
				case null                                        :
					if (options.ConfigurationString?.Contains("Sybase", StringComparison.Ordinal) == true)
						goto case ProviderName.Sybase;
					break;
				case ProviderName.Sybase                         :
					if (options.ConfigurationString?.Contains("Managed", StringComparison.Ordinal) == true)
						return _sybaseManagedDataProvider.Value;
					if (options.ConfigurationString?.Contains("Native", StringComparison.Ordinal) == true)
						return _sybaseNativeDataProvider.Value;
					return GetDataProvider(options, DetectProvider(options, SybaseProvider.AutoDetect), default);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, SybaseProvider provider, NoDialect version)
		{
			provider = DetectProvider(options, SybaseProvider.AutoDetect);

			return provider switch
			{
				SybaseProvider.Unmanaged => _sybaseNativeDataProvider.Value,
				_                        => _sybaseManagedDataProvider.Value,
			};
		}

		protected override DbConnection CreateConnection(SybaseProvider provider, string connectionString)
		{
			return SybaseProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}

		protected override SybaseProvider DetectProvider(ConnectionOptions options, SybaseProvider provider)
		{
			if (provider is SybaseProvider.DataAction or SybaseProvider.Unmanaged)
				return provider;

			switch (options.ProviderName)
			{
				case SybaseProviderAdapter.ManagedClientNamespace:
				case ProviderName.SybaseManaged                  :
					return SybaseProvider.DataAction;

				case "Sybase.Native"                             :
				case SybaseProviderAdapter.NativeClientNamespace :
				case SybaseProviderAdapter.NativeAssemblyName    :
					return SybaseProvider.Unmanaged;

				default                                          :
					if (options.ConfigurationString?.Contains("Managed", StringComparison.Ordinal) == true)
						return SybaseProvider.DataAction;
					if (options.ConfigurationString?.Contains("Native", StringComparison.Ordinal) == true)
						return SybaseProvider.Unmanaged;
					break;
			}

			var fileName = typeof(SybaseProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", SybaseProviderAdapter.NativeAssemblyName + ".dll"))
				? SybaseProvider.Unmanaged
				: SybaseProvider.DataAction;
		}
	}
}
