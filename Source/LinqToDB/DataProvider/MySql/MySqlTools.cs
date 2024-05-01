using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace LinqToDB.DataProvider.MySql
{
	using Data;

	public static partial class MySqlTools
	{
		internal static MySqlProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(
			MySqlVersion  version          = MySqlVersion.AutoDetect,
			MySqlProvider provider         = MySqlProvider.AutoDetect,
			string?       connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, version);
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
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, version), connectionString);
		}

		public static DataConnection CreateDataConnection(
			DbConnection  connection,
			MySqlVersion  version  = MySqlVersion.AutoDetect,
			MySqlProvider provider = MySqlProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), provider, version), connection);
		}

		public static DataConnection CreateDataConnection(
			DbTransaction transaction,
			MySqlVersion  version  = MySqlVersion.AutoDetect,
			MySqlProvider provider = MySqlProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), provider, version), transaction);
		}

		#endregion
	}
}
