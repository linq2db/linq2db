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
	using Extensions;

	public static class SQLiteTools
	{
		public static string AssemblyName;

		static readonly SQLiteDataProvider _SQLiteClassicDataProvider  = new SQLiteDataProvider(ProviderName.SQLiteClassic);
		static readonly SQLiteDataProvider _SQLiteMSDataProvider       = new SQLiteDataProvider(ProviderName.SQLiteMS);

		public static bool AlwaysCheckDbNull = true;

		static SQLiteTools()
		{
			AssemblyName = DetectedProviderName == ProviderName.SQLiteClassic ? "System.Data.SQLite" : "Microsoft.Data.Sqlite";

			DataConnection.AddDataProvider(ProviderName.SQLite, DetectedProvider);
			DataConnection.AddDataProvider(_SQLiteClassicDataProvider);
			DataConnection.AddDataProvider(_SQLiteMSDataProvider);

			DataConnection.AddProviderDetector(ProviderDetector);
		}

		static IDataProvider ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (css.IsGlobal)
				return null;

			switch (css.ProviderName)
			{
				case ""                                :
				case null                              :

					if (css.Name.Contains("SQLite"))
						goto case "SQLite";
					break;

				case "SQLite.MS"             :
				case "SQLite.Microsoft"      :
				case "Microsoft.Data.Sqlite" :
				case "Microsoft.Data.SQLite" : return _SQLiteMSDataProvider;
				case "SQLite.Classic"        :
				case "System.Data.SQLite"    : return _SQLiteClassicDataProvider;
				case "SQLite"                :

					if (css.Name.Contains("MS") || css.Name.Contains("Microsoft"))
						return _SQLiteMSDataProvider;

					if (css.Name.Contains("Classic"))
						return _SQLiteClassicDataProvider;

					return DetectedProvider;
			}

			return null;
		}

		static string _detectedProviderName;

		public static string  DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		static SQLiteDataProvider DetectedProvider =>
			DetectedProviderName == ProviderName.SQLiteClassic ? _SQLiteClassicDataProvider : _SQLiteMSDataProvider;

		static string DetectProviderName()
		{
			try
			{
				var path = typeof(SQLiteTools).AssemblyEx().GetPath();

				if (!File.Exists(Path.Combine(path, "System.Data.SQLite.dll")))
					if (File.Exists(Path.Combine(path, "Microsoft.Data.Sqlite.dll")))
						return ProviderName.SQLiteMS;
			}
			catch (Exception)
			{
			}

#if NETSTANDARD1_6 || NETSTANDARD2_0
			return ProviderName.SQLiteMS;
#else
			return ProviderName.SQLiteClassic;
#endif
		}


		public static IDataProvider GetDataProvider()
		{
			return DetectedProvider;
		}

		public static void ResolveSQLite(string path)
		{
			new AssemblyResolver(path, AssemblyName);
		}

		public static void ResolveSQLite(Assembly assembly)
		{
			new AssemblyResolver(assembly, AssemblyName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(DetectedProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(
				connection.GetType().Namespace.Contains("Microsoft") ? _SQLiteMSDataProvider : _SQLiteClassicDataProvider,
				connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(
				transaction.GetType().Namespace.Contains("Microsoft") ? _SQLiteMSDataProvider : _SQLiteClassicDataProvider,
				transaction);
		}

		#endregion

		public static void CreateDatabase(string databaseName, bool deleteIfExists = false)
		{
			DetectedProvider.CreateDatabase(databaseName, deleteIfExists);
		}

		public static void DropDatabase(string databaseName)
		{
			DetectedProvider.DropDatabase(databaseName);
		}

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int                        maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
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


		/// <summary>
		/// Executes "PRAGMA key = &lt;KEY&gt;" query to set database encryption key.
		/// <seealso href="https://www.bricelam.net/2016/06/13/sqlite-encryption.html"/>.
		/// </summary>
		/// <param name="connection">Database connection to configure.</param>
		/// <param name="key">Database encryption key.</param>
		public static void PragmaKey(IDbConnection connection, string key)
		{
			string quotedKey = Quote(connection, key);

			using (var command = connection.CreateCommand())
			{
				command.CommandText = "PRAGMA key = " + quotedKey;
				command.ExecuteNonQuery();
			}
		}

		private static string Quote(IDbConnection connection, string value)
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT quote($value);";

				var parameter = command.CreateParameter();
				parameter.ParameterName = "$value";
				parameter.Value = value;
				command.Parameters.Add(parameter);

				return (string)command.ExecuteScalar();
			}
		}

		/// <summary>
		/// Executes "PRAGMA rekey = &lt;KEY&gt;" query to change database encryption key.
		/// <seealso href="https://www.bricelam.net/2016/06/13/sqlite-encryption.html"/>.
		/// </summary>
		/// <param name="connection">Database connection to configure.</param>
		/// <param name="key">New database encryption key or <c>null</c> to decrypt database.</param>
		public static void PragmaRekey(IDbConnection connection, string key)
		{
			var quotedKey = "NULL";

			if (key != null)
				quotedKey = Quote(connection, key);

			using (var command = connection.CreateCommand())
			{
				command.CommandText = "PRAGMA rekey = " + quotedKey;
				command.ExecuteNonQuery();
			}
		}
	}
}
