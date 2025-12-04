using System;
using System.Data.Common;
using System.IO;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public class SQLiteProviderDetector : ProviderDetectorBase<SQLiteProvider>
	{
		internal static readonly Lazy<IDataProvider> _SQLiteClassicDataProvider = CreateDataProvider<SQLiteDataProviderClassic>();
		internal static readonly Lazy<IDataProvider> _SQLiteMSDataProvider      = CreateDataProvider<SQLiteDataProviderMS>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			// don't merge this method and DetectProvider(provider type) logic because this method could return null
			// and other method returns default provider type
			switch (options.ProviderName)
			{
				case SQLiteProviderAdapter.SystemDataSQLiteClientNamespace   :
				case ProviderName.SQLiteClassic                              : return _SQLiteClassicDataProvider.Value;
				case SQLiteProviderAdapter.MicrosoftDataSQLiteClientNamespace:
				case "Microsoft.Data.SQLite"                                 :
				case ProviderName.SQLiteMS                                   : return _SQLiteMSDataProvider.Value;
				case ""                                                      :
				case null                                                    :
					if (options.ConfigurationString?.Contains("SQLite") == true || options.ConfigurationString?.Contains("Sqlite") == true)
						goto case ProviderName.SQLite;
					break;
				case ProviderName.SQLite                                     :
					if (options.ConfigurationString?.Contains("MS") == true || options.ConfigurationString?.Contains("Microsoft") == true)
						return _SQLiteMSDataProvider.Value;

					if (options.ConfigurationString?.Contains("Classic") == true)
						return _SQLiteClassicDataProvider.Value;

					return GetDataProvider(options, DetectProvider(options, SQLiteProvider.AutoDetect), default);
				case var providerName when providerName.Contains("SQLite") || providerName.Contains("Sqlite"):
					if (options.ProviderName.Contains("MS") || options.ProviderName.Contains("Microsoft"))
						return _SQLiteMSDataProvider.Value;

					if (options.ProviderName.Contains("Classic"))
						return _SQLiteClassicDataProvider.Value;

					return GetDataProvider(options, DetectProvider(options, SQLiteProvider.AutoDetect), default);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, SQLiteProvider provider, NoDialect version)
		{
			provider = DetectProvider(options, provider);

			return provider switch
			{
				SQLiteProvider.System => _SQLiteClassicDataProvider.Value,
				_                     => _SQLiteMSDataProvider.Value,
			};
		}

		protected override DbConnection CreateConnection(SQLiteProvider provider, string connectionString)
		{
			return SQLiteProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}

		protected override SQLiteProvider DetectProvider(ConnectionOptions options, SQLiteProvider provider)
		{
			if (provider is SQLiteProvider.Microsoft or SQLiteProvider.System)
				return provider;

			switch (options.ProviderName)
			{
				case SQLiteProviderAdapter.SystemDataSQLiteClientNamespace   :
				case ProviderName.SQLiteClassic                              :
					return SQLiteProvider.System;

				case SQLiteProviderAdapter.MicrosoftDataSQLiteClientNamespace:
				case "Microsoft.Data.SQLite"                                 :
				case ProviderName.SQLiteMS                                   :
					return SQLiteProvider.Microsoft;
			}

			if (options.ProviderName?.Contains("MS") == true || options.ProviderName?.Contains("Microsoft") == true
				|| options.ConfigurationString?.Contains("MS") == true || options.ConfigurationString?.Contains("Microsoft") == true)
				return SQLiteProvider.Microsoft;

			if (options.ProviderName?.Contains("Classic") == true || options.ConfigurationString?.Contains("Classic") == true)
				return SQLiteProvider.System;

			var fileName = typeof(SQLiteProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", SQLiteProviderAdapter.SystemDataSQLiteAssemblyName + ".dll"))
				? SQLiteProvider.System
				: SQLiteProvider.Microsoft;
		}
	}
}
