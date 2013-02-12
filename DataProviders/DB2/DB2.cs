using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	using Data;

	public class DB2 : IDataProviderFactory
	{
		static readonly DB2DataProvider _db2DataProvider = new DB2DataProvider();

		static DB2()
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
