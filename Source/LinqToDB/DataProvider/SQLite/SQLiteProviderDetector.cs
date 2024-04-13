﻿using System;
using System.IO;

namespace LinqToDB.DataProvider.SQLite
{
	using Common;
	using Data;

	sealed class SQLiteProviderDetector : ProviderDetectorBase<SQLiteProvider, SQLiteProviderDetector.Dialect, SQLiteProviderAdapter.SQLiteConnection>
	{
		internal enum Dialect { }

		public SQLiteProviderDetector() : base(default, default)
		{
		}

		internal static readonly Lazy<IDataProvider> _SQLiteClassicDataProvider = DataConnection.CreateDataProvider<SQLiteDataProviderClassic>();
		internal static readonly Lazy<IDataProvider> _SQLiteMSDataProvider      = DataConnection.CreateDataProvider<SQLiteDataProviderMS>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			var provider = options.ProviderName switch
			{
				SQLiteProviderAdapter.SystemDataSQLiteClientNamespace    => SQLiteProvider.System,
				SQLiteProviderAdapter.MicrosoftDataSQLiteClientNamespace => SQLiteProvider.Microsoft,
				_                                                        => DetectProvider()
			};

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

					return GetDataProvider(options, provider, default);
				case var providerName when providerName.Contains("SQLite") || providerName.Contains("Sqlite"):
					if (options.ProviderName.Contains("MS") || options.ProviderName.Contains("Microsoft"))
						return _SQLiteMSDataProvider.Value;

					if (options.ProviderName.Contains("Classic"))
						return _SQLiteClassicDataProvider.Value;

					return GetDataProvider(options, provider, default);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, SQLiteProvider provider, Dialect version)
		{
			if (provider == SQLiteProvider.AutoDetect)
				provider = DetectProvider();

			return provider switch
			{
				SQLiteProvider.System => _SQLiteClassicDataProvider.Value,
				_                     => _SQLiteMSDataProvider.Value,
			};
		}

		public static SQLiteProvider DetectProvider()
		{
			var fileName = typeof(SQLiteProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", SQLiteProviderAdapter.SystemDataSQLiteAssemblyName + ".dll"))
				? SQLiteProvider.System
				: SQLiteProvider.Microsoft;
		}

		public override Dialect? DetectServerVersion(SQLiteProviderAdapter.SQLiteConnection connection)
		{
			return default(Dialect);
		}

		protected override SQLiteProviderAdapter.SQLiteConnection CreateConnection(SQLiteProvider provider, string connectionString)
		{
			return SQLiteProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
