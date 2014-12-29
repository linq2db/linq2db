using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.Informix
{
	using Data;

	public static class InformixTools
	{
		static readonly InformixDataProvider _informixDataProvider = new InformixDataProvider();

		static InformixTools()
		{
			DataConnection.AddDataProvider(_informixDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _informixDataProvider;
		}

		public static void ResolveInformix(string path)
		{
			new AssemblyResolver(path, "IBM.Data.Informix");
		}

		public static void ResolveInformix(Assembly assembly)
		{
			new AssemblyResolver(assembly, "IBM.Data.Informix");
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_informixDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_informixDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_informixDataProvider, transaction);
		}

		#endregion


		#region BulkCopy

		private static BulkCopyType _defaultBulkCopyType = BulkCopyType.MultipleRows;
		public  static BulkCopyType  DefaultBulkCopyType
		{
			get { return _defaultBulkCopyType;  }
			set { _defaultBulkCopyType = value; }
		}

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int                        maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion
	}
}
