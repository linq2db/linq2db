using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	using Data;

	public class Access : IDataProviderFactory
	{
		static readonly AccessDataProvider _accessDataProvider = new AccessDataProvider();

		static Access()
		{
			DataConnection.AddDataProvider(ProviderName.Access, _accessDataProvider);
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
	}
}
