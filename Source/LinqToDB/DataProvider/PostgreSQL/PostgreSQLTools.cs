using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using JetBrains.Annotations;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Data;

	[PublicAPI]
	public static partial class PostgreSQLTools
	{
		internal static PostgreSQLProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(PostgreSQLVersion version = PostgreSQLVersion.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), default, version);
		}

		public static void ResolvePostgreSQL(string path)
		{
			_ = new AssemblyResolver(path, NpgsqlProviderAdapter.AssemblyName);
		}

		public static void ResolvePostgreSQL(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, PostgreSQLVersion version = PostgreSQLVersion.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), default, version), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, PostgreSQLVersion version = PostgreSQLVersion.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), default, version), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, PostgreSQLVersion version = PostgreSQLVersion.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), default, version), transaction);
		}

		#endregion
	}
}
