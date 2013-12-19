using System;
using System.Data;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.Oracle
{
	using Data;

	public static class OracleTools
	{
		public static string AssemblyName = "Oracle.DataAccess";

		static readonly OracleDataProvider _oracleDataProvider = new OracleDataProvider();

		private static BulkCopyType _defaultBulkCopyType = BulkCopyType.MultipleRows;
		public  static BulkCopyType  DefaultBulkCopyType
		{
			get { return _defaultBulkCopyType;  }
			set { _defaultBulkCopyType = value; }
		}

		static OracleTools()
		{
			try
			{
				var path = typeof(OracleTools).Assembly.CodeBase.Replace("file:///", "");

				path = Path.GetDirectoryName(path);

				if (!File.Exists(Path.Combine(path, "Oracle.DataAccess.dll")))
					if (File.Exists(Path.Combine(path, "Oracle.ManagedDataAccess.dll")))
						AssemblyName = "Oracle.ManagedDataAccess";
			}
			catch (Exception)
			{
			}

			DataConnection.AddDataProvider(_oracleDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _oracleDataProvider;
		}

		public static void ResolveOracle(string path)
		{
			new AssemblyResolver(path, AssemblyName);
		}

		public static void ResolveOracle(Assembly assembly)
		{
			new AssemblyResolver(assembly, AssemblyName);
		}

		public static bool IsXmlTypeSupported
		{
			get { return _oracleDataProvider.IsXmlTypeSupported; }
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
