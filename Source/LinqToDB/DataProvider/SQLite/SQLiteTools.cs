using System;
using System.Data.Common;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.SQLite
{
	using Common;
	using Configuration;
	using Data;

	public static partial class SQLiteTools
	{
		static readonly Lazy<IDataProvider> _SQLiteClassicDataProvider = DataConnection.CreateDataProvider<SQLiteDataProviderClassic>();
		static readonly Lazy<IDataProvider> _SQLiteMSDataProvider      = DataConnection.CreateDataProvider<SQLiteDataProviderMS>();

		[Obsolete("Use SQLiteOptions.Default.AlwaysCheckDbNull instead.")]
		public static bool AlwaysCheckDbNull
		{
			get => SQLiteOptions.Default.AlwaysCheckDbNull;
			set => SQLiteOptions.Default = SQLiteOptions.Default with { AlwaysCheckDbNull = value };
		}

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
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

					return GetDataProvider();
				case var providerName when providerName.Contains("SQLite") || providerName.Contains("Sqlite"):
					if (options.ProviderName.Contains("MS") || options.ProviderName.Contains("Microsoft"))
						return _SQLiteMSDataProvider.Value;

					if (options.ProviderName.Contains("Classic"))
						return _SQLiteClassicDataProvider.Value;

					return GetDataProvider();
			}

			return null;
		}

		private static string? _detectedProviderName;
		public  static string  DetectedProviderName =>
			_detectedProviderName ??= DetectProviderName();

		static string DetectProviderName()
		{
			try
			{
				var path = typeof(SQLiteTools).Assembly.GetPath();

				if (   !File.Exists(Path.Combine(path, $"{SQLiteProviderAdapter.SystemDataSQLiteAssemblyName}.dll")))
					if (File.Exists(Path.Combine(path, $"{SQLiteProviderAdapter.MicrosoftDataSQLiteAssemblyName}.dll")))
						return ProviderName.SQLiteMS;
			}
			catch
			{
			}

			return ProviderName.SQLiteClassic;
		}

		public static IDataProvider GetDataProvider(string? providerName = null)
		{
			switch (providerName)
			{
				case ProviderName.SQLiteClassic: return _SQLiteClassicDataProvider.Value;
				case ProviderName.SQLiteMS     : return _SQLiteMSDataProvider.Value;
			}

			if (DetectedProviderName == ProviderName.SQLiteClassic)
				return _SQLiteClassicDataProvider.Value;

			return _SQLiteMSDataProvider.Value;
		}

		public static void ResolveSQLite(string path)
		{
			new AssemblyResolver(
				path,
				DetectedProviderName == ProviderName.SQLiteClassic
						? SQLiteProviderAdapter.SystemDataSQLiteAssemblyName
						: SQLiteProviderAdapter.MicrosoftDataSQLiteAssemblyName);
		}

		public static void ResolveSQLite(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

		#endregion

		public static void CreateDatabase(string databaseName, bool deleteIfExists = false)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			DataTools.CreateFileDatabase(
				databaseName, deleteIfExists, ".sqlite",
				dbName =>
				{
					// don't use CreateFile method of System.Data.Sqlite as it just creates empty file
					using (File.Create(dbName)) { };
				});
		}

		public static void DropDatabase(string databaseName)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			DataTools.DropFileDatabase(databaseName, ".sqlite");
		}

		/// <summary>
		/// Invokes ClearAllPools() method for specified provider.
		/// </summary>
		/// <param name="provider">For which provider ClearAllPools should be called:
		/// <list type="bullet">
		/// <item><see cref="ProviderName.SQLiteClassic"/>: System.Data.SQLite</item>
		/// <item><see cref="ProviderName.SQLiteMS"/>: Microsoft.Data.Sqlite</item>
		/// <item><c>null</c>: both (any)</item>
		/// </list>
		/// </param>
		public static void ClearAllPools(string? provider = null)
		{
			// method will do nothing if provider is not loaded yet, but in that case user shouldn't have pooled connections
			// except situation, when he created them externally
			if ((provider == null || provider == ProviderName.SQLiteMS) && _SQLiteMSDataProvider.IsValueCreated)
			{
				((SQLiteDataProvider)_SQLiteMSDataProvider.Value).Adapter.ClearAllPools?.Invoke();
			}

			if ((provider == null || provider == ProviderName.SQLiteClassic) && _SQLiteClassicDataProvider.IsValueCreated)
			{
				((SQLiteDataProvider)_SQLiteClassicDataProvider.Value).Adapter.ClearAllPools?.Invoke();
			}
		}

		#region BulkCopy

		[Obsolete("Use SQLiteOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => SQLiteOptions.Default.BulkCopyType;
			set => SQLiteOptions.Default = SQLiteOptions.Default with { BulkCopyType = value };
		}

		#endregion
	}
}
