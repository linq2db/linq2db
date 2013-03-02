using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider.SQLite
{
	using Data;

	public class SQLiteFactory: IDataProviderFactory
	{
		static readonly SQLiteDataProvider _SQLiteDataProvider = new SQLiteDataProvider();

		static SQLiteFactory()
		{
			DataConnection.AddDataProvider(_SQLiteDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _SQLiteDataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _SQLiteDataProvider;
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
	}
}
