using System;
using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Firebird
{
	using Configuration;
	using Data;

	[PublicAPI]
	public static class FirebirdTools
	{
		internal static FirebirdProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(FirebirdVersion version = FirebirdVersion.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), default, version);
		}

		public static void ResolveFirebird(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			_ = new AssemblyResolver(path, FirebirdProviderAdapter.AssemblyName);
		}

		public static void ResolveFirebird(Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));
			_ = new AssemblyResolver(assembly, FirebirdProviderAdapter.AssemblyName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, FirebirdVersion version = FirebirdVersion.AutoDetect)
		{
			return new DataConnection(GetDataProvider(version, connectionString: connectionString), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, FirebirdVersion version = FirebirdVersion.AutoDetect)
		{
			return new DataConnection(GetDataProvider(version), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, FirebirdVersion version = FirebirdVersion.AutoDetect)
		{
			return new DataConnection(GetDataProvider(version), transaction);
		}

		#endregion

		#region BulkCopy

		[Obsolete("Use FirebirdOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => FirebirdOptions.Default.BulkCopyType;
			set => FirebirdOptions.Default = FirebirdOptions.Default with { BulkCopyType = value };
		}

		#endregion

		#region ClearAllPools

		public static void ClearAllPools() => FirebirdProviderAdapter.Instance.ClearAllPools();

		#endregion
	}
}
