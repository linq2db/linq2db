using System;
using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;

	public static class SybaseTools
	{
		public static string AssemblyName = "Sybase.AdoNet2.AseClient";

		static readonly SybaseDataProvider _sybaseDataProvider = new SybaseDataProvider();

		static SybaseTools()
		{
			DataConnection.AddDataProvider(_sybaseDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _sybaseDataProvider;
		}

		public static void ResolveSybase(string path)
		{
			new AssemblyResolver(path, AssemblyName);
		}

		public static void ResolveSybase(Assembly assembly)
		{
			new AssemblyResolver(assembly, AssemblyName);
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
