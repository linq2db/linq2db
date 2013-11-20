using System;
using System.Data;

namespace LinqToDB.DataProvider.Access
{
	using Data;

	public static class AccessTools
	{
		static readonly AccessDataProvider _accessDataProvider = new AccessDataProvider();

		static AccessTools()
		{
			DataConnection.AddDataProvider(_accessDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _accessDataProvider;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_accessDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_accessDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_accessDataProvider, transaction);
		}

		#endregion

		public static void CreateDatabase(string databaseName, bool deleteIfExists = false)
		{
			_accessDataProvider.CreateDatabase(databaseName, deleteIfExists);
		}

		public static void DropDatabase(string databaseName)
		{
			_accessDataProvider.DropDatabase(databaseName);
		}
	}
}
