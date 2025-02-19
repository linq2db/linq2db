using System;
using System.Data.Common;
using System.IO;
using System.Reflection;

using LinqToDB.Data;
using LinqToDB.Internal.DataProvider.SQLite;

namespace LinqToDB.DataProvider.SQLite
{
	public static partial class SQLiteTools
	{
		internal static SQLiteProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(SQLiteProvider provider = SQLiteProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, default);
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
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, default), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, SQLiteProvider provider = SQLiteProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), provider, default), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, SQLiteProvider provider = SQLiteProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), provider, default), transaction);
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
	}
}
