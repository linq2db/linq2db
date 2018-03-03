﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.SqlCe
{
	using Data;

	public static class SqlCeTools
	{
		static readonly SqlCeDataProvider _sqlCeDataProvider = new SqlCeDataProvider();

		static SqlCeTools()
		{
			DataConnection.AddDataProvider(_sqlCeDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _sqlCeDataProvider;
		}

		public static void ResolveSqlCe(string path)
		{
			new AssemblyResolver(path, "System.Data.SqlServerCe");
		}

		public static void ResolveSqlCe(Assembly assembly)
		{
			new AssemblyResolver(assembly, "System.Data.SqlServerCe");
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

		public static void CreateDatabase(string databaseName, bool deleteIfExists = false)
		{
			_sqlCeDataProvider.CreateDatabase(databaseName, deleteIfExists);
		}

		public static void DropDatabase(string databaseName)
		{
			_sqlCeDataProvider.DropDatabase(databaseName);
		}

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
