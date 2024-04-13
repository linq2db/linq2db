using System;
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

		public static IDataProvider GetDataProvider(MySqlProvider provider = MySqlProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(connectionString), provider, default);
		}

		[Obsolete($"Use overload with {nameof(MySqlProvider)} parameter")]
		public static IDataProvider GetDataProvider(string? providerName = null, string? connectionString = null)
		{
			return providerName switch
			{
				ProviderName.MySqlOfficial  => GetDataProvider(MySqlProvider.MySqlData, connectionString),
				ProviderName.MySqlConnector => GetDataProvider(MySqlProvider.MySqlConnector, connectionString),
				_                           => GetDataProvider(MySqlProvider.AutoDetect, connectionString),
			};
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

		public static DataConnection CreateDataConnection(string connectionString, MySqlProvider provider = MySqlProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider, connectionString), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, MySqlProvider provider = MySqlProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, MySqlProvider provider = MySqlProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), transaction);
		}

		[Obsolete($"Use overload with {nameof(MySqlProvider)} parameter")]
		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connectionString);
		}

		[Obsolete($"Use overload with {nameof(MySqlProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		[Obsolete($"Use overload with {nameof(MySqlProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

		#endregion

		#region BulkCopy

		[Obsolete("Use MySqlOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => MySqlOptions.Default.BulkCopyType;
			set => MySqlOptions.Default = MySqlOptions.Default with { BulkCopyType = value };
		}

		#endregion
	}
}
