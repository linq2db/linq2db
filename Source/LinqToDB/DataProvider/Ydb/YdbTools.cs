using System;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.Ydb;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Utility methods for working with Linq To DB and YDB,
	/// similar to <c>PostgreSQLTools</c> for PostgreSQL.
	/// </summary>
	[PublicAPI]
	public static class YdbTools
	{
		enum Fake { };

		static readonly Lazy<IDataProvider> _ydbDataProvider = ProviderDetectorBase<Fake>.CreateDataProvider<YdbDataProvider>();

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
			if (options.ProviderName?.Contains("Ydb") == true || options.ConfigurationString?.Contains("Ydb") == true)
				return _ydbDataProvider.Value;

			return null;
		}

		public static IDataProvider GetDataProvider() => _ydbDataProvider.Value;

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(_ydbDataProvider.Value, connectionString));
		}

		public static DataConnection CreateDataConnection(DbConnection connection)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(_ydbDataProvider.Value, connection));
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(_ydbDataProvider.Value, transaction));
		}

		#endregion

		public static void ResolveYdb(string path)
		{
			_ = new AssemblyResolver(path, YdbProviderAdapter.AssemblyName);
		}

		public static void ResolveYdb(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		/// <summary>
		/// Clear all YDB client connection pools.
		/// </summary>
		public static Task ClearAllPools() => YdbProviderAdapter.Instance.ClearAllPools();

		/// <summary>
		/// Clear connection pool for connection's connection string.
		/// </summary>
		public static Task ClearPool(DbConnection connection) => YdbProviderAdapter.Instance.ClearPool(connection);
	}
}
