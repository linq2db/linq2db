using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider.SqlCe
{
	using Data;

	public class SqlCeFactory : IDataProviderFactory
	{
		static readonly SqlCeDataProvider _sqlCeDataProvider = new SqlCeDataProvider();

		static SqlCeFactory()
		{
			DataConnection.AddDataProvider(_sqlCeDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _sqlCeDataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _sqlCeDataProvider;
		}

		public static void ResolveSqlCePath(string path)
		{
			new AssemblyResolver(path, "System.Data.SqlServerCe");
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
