using System;
using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.SQLite
{
	using Data;

	public static class SQLiteTools
	{
		static readonly SQLiteDataProvider _SQLiteDataProvider = new SQLiteDataProvider();

		public static bool AlwaysCheckDbNull = true;

		static SQLiteTools()
		{
			DataConnection.AddDataProvider(_SQLiteDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _SQLiteDataProvider;
		}

		public static void ResolveSQLite(string path)
		{
			new AssemblyResolver(path, "System.Data.SQLite");
		}

		public static void ResolveSQLite(Assembly assembly)
		{
			new AssemblyResolver(assembly, "System.Data.SQLite");
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
	}
}
