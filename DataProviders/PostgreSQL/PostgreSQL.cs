using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	using Data;

	public class PostgreSQL: IDataProviderFactory
	{
		static readonly PostgreSQLDataProvider _postgreSQLDataProvider = new PostgreSQLDataProvider();

		static PostgreSQL()
		{
			DataConnection.AddDataProvider(ProviderName.PostgreSQL, _postgreSQLDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _postgreSQLDataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _postgreSQLDataProvider;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_postgreSQLDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_postgreSQLDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_postgreSQLDataProvider, transaction);
		}

		#endregion
	}
}
