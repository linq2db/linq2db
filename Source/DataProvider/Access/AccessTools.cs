using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.Access
{
	using Data;

	public static class AccessTools
	{
		static readonly AccessDataProvider _accessDataProvider = new AccessDataProvider();

		static AccessTools()
		{
			DataConnection.AddDataProvider(_accessDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _accessDataProvider;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_accessDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_accessDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_accessDataProvider, transaction);
		}

		#endregion

		public static void CreateDatabase(string databaseName, bool deleteIfExists = false)
		{
			_accessDataProvider.CreateDatabase(databaseName, deleteIfExists);
		}

		public static void DropDatabase(string databaseName)
		{
			_accessDataProvider.DropDatabase(databaseName);
		}

		public static Func<IDataReader, int, string> GetChar = (dr, i) =>
		{
			var str = dr.GetString(i);

			if (str.Length > 0)
				return str[0].ToString();

			return string.Empty;
		};

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
