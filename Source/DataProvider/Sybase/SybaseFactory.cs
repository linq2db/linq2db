using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider.Sybase
{
	using System.Reflection;

	using Data;

	public class SybaseFactory : IDataProviderFactory
	{
		public static string AssemblyName = "Sybase.AdoNet2.AseClient";

		static readonly SybaseDataProvider _sybaseDataProvider = new SybaseDataProvider();

		static SybaseFactory()
		{
			DataConnection.AddDataProvider(_sybaseDataProvider);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			for (var i = 0; i < attributes.Count; i++)
				if (attributes.GetKey(i) == "assemblyName")
					AssemblyName = attributes.Get(i);

			return _sybaseDataProvider;
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
