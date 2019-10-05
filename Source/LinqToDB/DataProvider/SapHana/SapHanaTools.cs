namespace LinqToDB.DataProvider.SapHana
{
	using System;
	using System.Data;
	using System.Reflection;

	using Data;

	public static class SapHanaTools
	{
#if NET45 || NET46
		public static string AssemblyName = "Sap.Data.Hana.v4.5";
#endif

#if NETCOREAPP2_1
		public static string AssemblyName = "Sap.Data.Hana.Core.v2.1";
#endif

#if !NETSTANDARD2_0
		static readonly SapHanaDataProvider _hanaDataProvider = new SapHanaDataProvider();
#endif

		public static BulkCopyType DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		static SapHanaTools()
		{
#if !NETSTANDARD2_0
			DataConnection.AddDataProvider(_hanaDataProvider);
#endif
		}

		public static void ResolveSapHana(string path)
		{
#if NETSTANDARD2_0
			throw new PlatformNotSupportedException();
#else
			new AssemblyResolver(path, AssemblyName);
#endif
		}

		public static void ResolveSapHana(Assembly assembly)
		{
#if NETSTANDARD2_0
			throw new PlatformNotSupportedException();
#else
			new AssemblyResolver(assembly, AssemblyName);
#endif
		}

		public static IDataProvider GetDataProvider()
		{
#if NETSTANDARD2_0
			throw new PlatformNotSupportedException();
#else
			return _hanaDataProvider;
#endif
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
#if NETSTANDARD2_0
			throw new PlatformNotSupportedException();
#else
			return new DataConnection(_hanaDataProvider, connectionString);
#endif
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
#if NETSTANDARD2_0
			throw new PlatformNotSupportedException();
#else
			return new DataConnection(_hanaDataProvider, connection);
#endif
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
#if NETSTANDARD2_0
			throw new PlatformNotSupportedException();
#else
			return new DataConnection(_hanaDataProvider, transaction);
#endif
		}

		#endregion
	}
}
