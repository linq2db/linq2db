using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	using Data;

	public class Informix : IDataProviderFactory
	{
		static readonly InformixDataProvider _informixDataProvider = new InformixDataProvider();

		static Informix()
		{
			DataConnection.AddDataProvider(ProviderName.Informix, _informixDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _informixDataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _informixDataProvider;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_informixDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_informixDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_informixDataProvider, transaction);
		}

		#endregion
	}
}
