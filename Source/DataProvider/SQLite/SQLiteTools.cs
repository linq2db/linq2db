using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Extensions;

namespace LinqToDB.DataProvider.SQLite
{
	using Data;

	public static class SQLiteTools
	{
#if !NETSTANDARD
		public static string AssemblyName   = "System.Data.SQLite";
		public static string ConnectionName = "SQLiteConnection";
		public static string DataReaderName = "SQLiteDataReader";
#else
		public static string AssemblyName   = "Microsoft.Data.Sqlite";
		public static string ConnectionName = "SqliteConnection";
		public static string DataReaderName = "SqliteDataReader";
#endif

		static readonly SQLiteDataProvider _SQLiteDataProvider = new SQLiteDataProvider();

		public static bool AlwaysCheckDbNull = true;

		static SQLiteTools()
		{
#if !NETSTANDARD
			try
			{
				var path = typeof(SQLiteTools).AssemblyEx().GetPath();

				if (!File.Exists(Path.Combine(path, AssemblyName + ".dll")))
				{
					if (Type.GetType("Mono.Runtime") != null || File.Exists(Path.Combine(path, "Mono.Data.Sqlite.dll")))
					{
						AssemblyName   = "Mono.Data.Sqlite";
						ConnectionName = "SqliteConnection";
						DataReaderName = "SqliteDataReader";
					}
					else if (File.Exists(Path.Combine(path, "Microsoft.Data.Sqlite.dll")))
					{
						AssemblyName   = "Microsoft.Data.Sqlite";
						ConnectionName = "SqliteConnection";
						DataReaderName = "SqliteDataReader";
						 
					}
				}
			}
			catch (Exception)
			{
			}
#endif
			DataConnection.AddDataProvider(_SQLiteDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _SQLiteDataProvider;
		}

		public static void ResolveSQLite(string path)
		{
			new AssemblyResolver(path, AssemblyName);
#if !NETSTANDARD
			new AssemblyResolver(path, "Mono.Data.Sqlite");
#endif
		}

		public static void ResolveSQLite(Assembly assembly)
		{
			new AssemblyResolver(assembly, AssemblyName);
#if !NETSTANDARD
			new AssemblyResolver(assembly, "Mono.Data.Sqlite");
#endif
		}

#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_SQLiteDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_SQLiteDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_SQLiteDataProvider, transaction);
		}

#endregion

		public static void CreateDatabase(string databaseName, bool deleteIfExists = false)
		{
			_SQLiteDataProvider.CreateDatabase(databaseName, deleteIfExists);
		}

		public static void DropDatabase(string databaseName)
		{
			_SQLiteDataProvider.DropDatabase(databaseName);
		}

#region BulkCopy

		private static BulkCopyType _defaultBulkCopyType = BulkCopyType.MultipleRows;
		public  static BulkCopyType  DefaultBulkCopyType
		{
			get { return _defaultBulkCopyType;  }
			set { _defaultBulkCopyType = value; }
		}

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
	}
}
