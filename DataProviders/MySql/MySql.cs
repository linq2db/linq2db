using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	using Data;

	public class MySql : IDataProviderFactory
	{
		static readonly MySqlDataProvider _mySqlDataProvider = new MySqlDataProvider(ProviderName.MySql);

		static MySql()
		{
			DataConnection.AddDataProvider(_mySqlDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _mySqlDataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _mySqlDataProvider;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_mySqlDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_mySqlDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_mySqlDataProvider, transaction);
		}

		#endregion
	}
}
