using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider.Access
{
	using Data;

	public class AccessFactory : IDataProviderFactory
	{
		static readonly AccessDataProvider _accessDataProvider = new AccessDataProvider();

		static AccessFactory()
		{
			DataConnection.AddDataProvider(_accessDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _accessDataProvider;
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
