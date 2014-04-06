using System;
using System.Data;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Firebird
{
	using Data;

	public static class FirebirdTools
	{
		static readonly FirebirdDataProvider _firebirdDataProvider = new FirebirdDataProvider();

		static FirebirdTools()
		{
			DataConnection.AddDataProvider(_firebirdDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _firebirdDataProvider;
		}

		public static void ResolveFirebird([NotNull] string path)
		{
			if (path == null) throw new ArgumentNullException("path");
			new AssemblyResolver(path, "FirebirdSql.Data.FirebirdClient");
		}

		public static void ResolveFirebird([NotNull] Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException("assembly");
			new AssemblyResolver(assembly, "FirebirdSql.Data.FirebirdClient");
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_firebirdDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_firebirdDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_firebirdDataProvider, transaction);
		}

		#endregion
	}
}
