using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider.DB2
{
	using Data;

	public class DB2Factory : IDataProviderFactory
	{
		static readonly DB2DataProvider _db2DataProvider = new DB2DataProvider();

		static DB2Factory()
		{
			DataConnection.AddDataProvider(_db2DataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _db2DataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _db2DataProvider;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_db2DataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_db2DataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_db2DataProvider, transaction);
		}

		#endregion
	}
}
