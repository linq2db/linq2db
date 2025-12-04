using System.Data.Common;
using System.Reflection;

using LinqToDB.Data;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.MySql;

namespace LinqToDB.DataProvider.MySql
{
	public static class MySqlTools
	{
		internal static MySqlProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(
			MySqlVersion   version          = MySqlVersion.AutoDetect,
			MySqlProvider  provider         = MySqlProvider.AutoDetect,
			string?        connectionString = null,
			DbConnection?  connection       = null,
			DbTransaction? transaction      = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString, DbConnection: connection, DbTransaction: transaction), provider, version);
		}

		public static void ResolveMySql(string path, string? assemblyName)
		{
			_ = new AssemblyResolver(path, assemblyName ?? MySqlProviderAdapter.MySqlConnectorAssemblyName);
		}

		public static void ResolveMySql(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(
			string        connectionString,
			MySqlVersion  version  = MySqlVersion.AutoDetect,
			MySqlProvider provider = MySqlProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, version), connectionString));
		}

		public static DataConnection CreateDataConnection(
			DbConnection  connection,
			MySqlVersion  version  = MySqlVersion.AutoDetect,
			MySqlProvider provider = MySqlProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), provider, version), connection));
		}

		public static DataConnection CreateDataConnection(
			DbTransaction transaction,
			MySqlVersion  version  = MySqlVersion.AutoDetect,
			MySqlProvider provider = MySqlProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), provider, version), transaction));
		}

		#endregion
	}
}
