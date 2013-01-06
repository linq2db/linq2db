using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	using Data;

	public class SqlCe: IDataProviderFactory
	{
		static readonly SqlCeDataProvider _sqlCeDataProvider = new SqlCeDataProvider();

		static SqlCe()
		{
			DataConnection.AddDataProvider(ProviderName.SqlCe, _sqlCeDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _sqlCeDataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _sqlCeDataProvider;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_sqlCeDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_sqlCeDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_sqlCeDataProvider, transaction);
		}

		#endregion
	}
}
