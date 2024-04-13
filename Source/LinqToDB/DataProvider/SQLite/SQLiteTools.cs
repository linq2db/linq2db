using System;
using System.Data.Common;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.SQLite
{
	using Data;

	public static partial class SQLiteTools
	{
		internal static SQLiteProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		[Obsolete("Use SQLiteOptions.Default.AlwaysCheckDbNull instead.")]
		public static bool AlwaysCheckDbNull
		{
			get => SQLiteOptions.Default.AlwaysCheckDbNull;
			set => SQLiteOptions.Default = SQLiteOptions.Default with { AlwaysCheckDbNull = value };
		}

		public static IDataProvider GetDataProvider(SQLiteProvider provider = SQLiteProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(connectionString), provider, default);
		}

		[Obsolete($"Use overload with {nameof(SQLiteProvider)} parameter")]
		public static IDataProvider GetDataProvider(string? providerName = null, string? connectionString = null)
		{
			return providerName switch
			{
				ProviderName.SQLiteClassic => GetDataProvider(SQLiteProvider.System, connectionString),
				ProviderName.SQLiteMS      => GetDataProvider(SQLiteProvider.Microsoft, connectionString),
				_                          => GetDataProvider(SQLiteProvider.AutoDetect, connectionString),
			};
		}

		public static void ResolveSQLite(string path, string? assemblyName = null)
		{
			_ = new AssemblyResolver(path, assemblyName ?? SQLiteProviderAdapter.MicrosoftDataSQLiteAssemblyName);
		}

		public static void ResolveSQLite(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, SQLiteProvider provider = SQLiteProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider, connectionString), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, SQLiteProvider provider = SQLiteProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, SQLiteProvider provider = SQLiteProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), transaction);
		}

		[Obsolete($"Use overload with {nameof(SQLiteProvider)} parameter")]
		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName, connectionString), connectionString);
		}

		[Obsolete($"Use overload with {nameof(SQLiteProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		[Obsolete($"Use overload with {nameof(SQLiteProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

		#endregion

		public static void CreateDatabase(string databaseName, bool deleteIfExists = false, string extension = ".sqlite")
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			DataTools.CreateFileDatabase(
				databaseName, deleteIfExists, extension,
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
		/// <param name="provider">For which provider ClearAllPools should be called. If <c>null</c> value passed - call method for all providers.
		/// </param>
		public static void ClearAllPools(SQLiteProvider? provider)
		{
			// method will do nothing if provider is not loaded yet, but in that case user shouldn't have pooled connections
			// except situation, when he created them externally
			if ((provider == null || provider == SQLiteProvider.Microsoft) && SQLiteProviderDetector._SQLiteMSDataProvider.IsValueCreated)
			{
				((SQLiteDataProvider)SQLiteProviderDetector._SQLiteMSDataProvider.Value).Adapter.ClearAllPools?.Invoke();
			}

			if ((provider == null || provider == SQLiteProvider.System) && SQLiteProviderDetector._SQLiteClassicDataProvider.IsValueCreated)
			{
				((SQLiteDataProvider)SQLiteProviderDetector._SQLiteClassicDataProvider.Value).Adapter.ClearAllPools?.Invoke();
			}
		}

		/// <summary>
		/// Invokes ClearAllPools() method for specified provider.
		/// </summary>
		/// <param name="providerName">For which provider ClearAllPools should be called:
		/// <list type="bullet">
		/// <item><see cref="ProviderName.SQLiteClassic"/>: System.Data.SQLite</item>
		/// <item><see cref="ProviderName.SQLiteMS"/>: Microsoft.Data.Sqlite</item>
		/// <item><c>null</c>: both (any)</item>
		/// </list>
		/// </param>
		[Obsolete($"Use overload with {nameof(SQLiteProvider)} parameter")]
		public static void ClearAllPools(string? providerName = null)
		{
			var provider = providerName switch
			{
				ProviderName.SQLiteClassic => SQLiteProvider.System,
				ProviderName.SQLiteMS      => SQLiteProvider.Microsoft,
				_                          => (SQLiteProvider?)null,
			};

			ClearAllPools(provider);
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
