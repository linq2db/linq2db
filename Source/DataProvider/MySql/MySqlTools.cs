using System;
using System.Data;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.MySql
{
	using Data;

	public static class MySqlTools
	{
		static readonly MySqlDataProvider _mySqlDataProvider = new MySqlDataProvider();
		
		private static BulkCopyType _defaultBulkCopyType = BulkCopyType.MultipleRows;
		public static BulkCopyType DefaultBulkCopyType
		{
			get { return _defaultBulkCopyType; }
			set { _defaultBulkCopyType = value; }
		}

		static MySqlTools()
		{
			DataConnection.AddDataProvider(_mySqlDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _mySqlDataProvider;
		}

		public static void ResolveMySql([NotNull] string path)
		{
			if (path == null) throw new ArgumentNullException("path");
			new AssemblyResolver(path, "MySql.Data");
		}

		public static void ResolveMySql([NotNull] Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException("assembly");
			new AssemblyResolver(assembly, "MySql.Data");
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_mySqlDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_mySqlDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_mySqlDataProvider, transaction);
		}

		#endregion
	}
}
