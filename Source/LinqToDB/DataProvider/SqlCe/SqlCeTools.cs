using System;
using System.Data.Common;
using System.Reflection;

using LinqToDB.Data;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.SqlCe;

namespace LinqToDB.DataProvider.SqlCe
{
	public static class SqlCeTools
	{
		enum Fake { };

		static readonly Lazy<IDataProvider> _sqlCeDataProvider = ProviderDetectorBase<Fake>.CreateDataProvider<SqlCeDataProvider>();

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
			if (options.ProviderName?.Contains("SqlCe", StringComparison.Ordinal) == true
				|| options.ProviderName?.Contains("SqlServerCe", StringComparison.Ordinal) == true
				|| options.ConfigurationString?.Contains("SqlCe", StringComparison.Ordinal) == true
				|| options.ConfigurationString?.Contains("SqlServerCe", StringComparison.Ordinal) == true)
				return _sqlCeDataProvider.Value;

			return null;
		}

		public static IDataProvider GetDataProvider() => _sqlCeDataProvider.Value;

		public static void ResolveSqlCe(string path)
		{
			_ = new AssemblyResolver(path, SqlCeProviderAdapter.AssemblyName);
		}

		public static void ResolveSqlCe(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(_sqlCeDataProvider.Value, connectionString));
		}

		public static DataConnection CreateDataConnection(DbConnection connection)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(_sqlCeDataProvider.Value, connection));
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(_sqlCeDataProvider.Value, transaction));
		}

		#endregion

		public static void CreateDatabase(string databaseName, bool deleteIfExists = false)
		{
			ArgumentNullException.ThrowIfNull(databaseName);

			DataTools.CreateFileDatabase(
				databaseName, deleteIfExists, ".sdf",
				dbName =>
				{
					using var engine = SqlCeProviderAdapter.GetInstance().CreateSqlCeEngine("Data Source=" + dbName);
					engine.CreateDatabase();
				});
		}

		public static void DropDatabase(string databaseName)
		{
			ArgumentNullException.ThrowIfNull(databaseName);

			DataTools.DropFileDatabase(databaseName, ".sdf");
		}
	}
}
