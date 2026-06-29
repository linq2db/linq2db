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
	/// Utility methods for working with Linq To DB and YDB.
	/// </summary>
	[PublicAPI]
	public static class YdbTools
	{
		enum Fake { };

		static readonly Lazy<IDataProvider> _ydbDataProvider = ProviderDetectorBase<Fake>.CreateDataProvider<YdbDataProvider>();

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
			static bool HasYdb(string? s) =>
				s?.IndexOf("Ydb", StringComparison.OrdinalIgnoreCase) >= 0;

			if (HasYdb(options.ProviderName) || HasYdb(options.ConfigurationString))
				return _ydbDataProvider.Value;

			return null;
		}

		/// <summary>
		/// Returns YDB data provider instance.
		/// </summary>
		/// <param name="connectionString">Connection string (not used for provider detection; YDB exposes a single provider).</param>
		/// <param name="connection">Connection instance (not used for provider detection).</param>
		/// <param name="transaction">Transaction instance (not used for provider detection).</param>
		/// <returns>YDB data provider instance.</returns>
		public static IDataProvider GetDataProvider(string? connectionString = null, DbConnection? connection = null, DbTransaction? transaction = null) => _ydbDataProvider.Value;

		#region CreateDataConnection

		/// <summary>
		/// Creates <see cref="DataConnection"/> instance that uses YDB data provider and the provided connection string.
		/// </summary>
		/// <param name="connectionString">YDB connection string.</param>
		/// <returns>New <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(_ydbDataProvider.Value, connectionString));
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> instance that uses YDB data provider and the provided connection.
		/// </summary>
		/// <param name="connection">YDB connection instance.</param>
		/// <returns>New <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(DbConnection connection)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(_ydbDataProvider.Value, connection));
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> instance that uses YDB data provider and the provided transaction.
		/// </summary>
		/// <param name="transaction">YDB transaction instance.</param>
		/// <returns>New <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(DbTransaction transaction)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(_ydbDataProvider.Value, transaction));
		}

		#endregion

		/// <summary>
		/// Loads and registers YDB ADO.NET provider assembly for use with Linq To DB from the specified path.
		/// </summary>
		/// <param name="path">Path to the directory or file with the YDB provider assembly.</param>
		public static void ResolveYdb(string path)
		{
			_ = new AssemblyResolver(path, YdbProviderAdapter.AssemblyName);
		}

		/// <summary>
		/// Registers the provided YDB ADO.NET provider assembly for use with Linq To DB.
		/// </summary>
		/// <param name="assembly">YDB provider assembly.</param>
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
