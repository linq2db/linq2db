using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Data;

namespace LinqToDB.DataProvider.DB2
{
	[PublicAPI]
	public static class DB2Tools
	{
		internal static DB2ProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(DB2Version version = DB2Version.AutoDetect, string? connectionString = null, DbConnection? connection = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString, DbConnection: connection), default, version);
		}

		public static void ResolveDB2(string path)
		{
			_ = new AssemblyResolver(path, DB2ProviderAdapter.AssemblyName);
#if !NETFRAMEWORK
			_ = new AssemblyResolver(path, DB2ProviderAdapter.AssemblyNameOld);
#endif
		}

		public static void ResolveDB2(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided DB2 connection string.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="version">DB2 version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(string connectionString, DB2Version version = DB2Version.LUW)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), default, version), connectionString));
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided connection object.
		/// </summary>
		/// <param name="connection">Connection instance.</param>
		/// <param name="version">DB2 version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(DbConnection connection, DB2Version version = DB2Version.LUW)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), default, version), connection));
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided transaction object.
		/// </summary>
		/// <param name="transaction">Transaction instance.</param>
		/// <param name="version">DB2 version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(DbTransaction transaction, DB2Version version = DB2Version.LUW)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), default, version), transaction));
		}

		#endregion
	}
}
