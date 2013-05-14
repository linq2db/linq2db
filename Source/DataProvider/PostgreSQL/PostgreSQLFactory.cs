using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using System.Reflection;

	using Data;

	public class PostgreSQLFactory: IDataProviderFactory
	{
		static readonly PostgreSQLDataProvider _postgreSQLDataProvider = new PostgreSQLDataProvider();

		static PostgreSQLFactory()
		{
			DataConnection.AddDataProvider(_postgreSQLDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return _postgreSQLDataProvider;
		}

		public static IDataProvider GetDataProvider()
		{
			return _postgreSQLDataProvider;
		}

		public static void ResolveOracle(string path)
		{
			new AssemblyResolver(path, "Npgsql");
		}

		public static void ResolvePostgreSQL(Assembly assembly)
		{
			new AssemblyResolver(assembly, "Npgsql");
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
