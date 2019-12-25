using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.SqlCe
{
	using Data;
	using LinqToDB.Configuration;

	public static class SqlCeTools
	{
		private static readonly Lazy<IDataProvider> _sqlCeDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new SqlCeDataProvider();

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (css.ProviderName?.Contains("SqlCe") == true
				|| css.Name?.Contains("SqlCe") == true)
			{
				return _sqlCeDataProvider.Value;
			}

			return null;
		}

		public static IDataProvider GetDataProvider() => _sqlCeDataProvider.Value;

		public static void ResolveSqlCe(string path)
		{
			new AssemblyResolver(path, SqlCeWrappers.AssemblyName);
		}

		public static void ResolveSqlCe(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.FullName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_sqlCeDataProvider.Value, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_sqlCeDataProvider.Value, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_sqlCeDataProvider.Value, transaction);
		}

		#endregion

		public static void CreateDatabase(string databaseName, bool deleteIfExists = false)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			SqlCeWrappers.Initialize();

			DataTools.CreateFileDatabase(
				databaseName, deleteIfExists, ".sdf",
				dbName =>
				{
					using (var engine = SqlCeWrappers.NewSqlCeEngine("Data Source=" + dbName))
						engine.CreateDatabase();
				});
		}

		public static void DropDatabase(string databaseName)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			DataTools.DropFileDatabase(databaseName, ".sdf");
		}

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection               dataConnection,
			IEnumerable<T>               source,
			int                          maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied>?  rowsCopiedCallback = null)
			where T : class
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
