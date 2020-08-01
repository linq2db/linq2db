using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.SQLite
{
	using Common;
	using Configuration;
	using Data;

	public static class SQLiteTools
	{
		private static readonly Lazy<IDataProvider> _SQLiteClassicDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new SQLiteDataProvider(ProviderName.SQLiteClassic);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _SQLiteMSDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new SQLiteDataProvider(ProviderName.SQLiteMS);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		public static bool AlwaysCheckDbNull = true;

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (css.IsGlobal)
				return null;

			switch (css.ProviderName)
			{
				case SQLiteProviderAdapter.SystemDataSQLiteClientNamespace   :
				case ProviderName.SQLiteClassic                              : return _SQLiteClassicDataProvider.Value;
				case SQLiteProviderAdapter.MicrosoftDataSQLiteClientNamespace:
				case "Microsoft.Data.SQLite"                                 :
				case ProviderName.SQLiteMS                                   : return _SQLiteMSDataProvider.Value;
				case ""                                                      :
				case null                                                    :
					if (css.Name.Contains("SQLite") || css.Name.Contains("Sqlite"))
						goto case ProviderName.SQLite;
					break;
				case ProviderName.SQLite                                     :
					if (css.Name.Contains("MS") || css.Name.Contains("Microsoft"))
						return _SQLiteMSDataProvider.Value;

					if (css.Name.Contains("Classic"))
						return _SQLiteClassicDataProvider.Value;

					return GetDataProvider();
				case var providerName when providerName.Contains("SQLite") || providerName.Contains("Sqlite"):
					if (css.ProviderName.Contains("MS") || css.ProviderName.Contains("Microsoft"))
						return _SQLiteMSDataProvider.Value;

					if (css.ProviderName.Contains("Classic"))
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

		public static DataConnection CreateDataConnection(IDbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, string? providerName = null)
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

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		[Obsolete("Please use the BulkCopy extension methods within DataConnectionExtensions")]
		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection               dataConnection,
			IEnumerable<T>               source,
			int                          maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied>?  rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion
	}
}
