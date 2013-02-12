using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	using Data;

	public class Sybase : IDataProviderFactory
	{
		static readonly SybaseDataProvider _sybaseDataProvider = new SybaseDataProvider();

		static Sybase()
		{
			DataConnection.AddDataProvider(_sybaseDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _sybaseDataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _sybaseDataProvider;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_sybaseDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_sybaseDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_sybaseDataProvider, transaction);
		}

		#endregion
	}
}
