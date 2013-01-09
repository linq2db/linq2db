using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	using Data;

	public class Oracle: IDataProviderFactory
	{
		static readonly OracleDataProvider _oracleDataProvider = new OracleDataProvider();

		static Oracle()
		{
			DataConnection.AddDataProvider(ProviderName.Oracle, _oracleDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _oracleDataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _oracleDataProvider;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_oracleDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_oracleDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_oracleDataProvider, transaction);
		}

		#endregion
	}
}
