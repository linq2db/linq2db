using System;
using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.Firebird;

namespace LinqToDB.DataProvider.Firebird
{
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
			return new DataConnection(new DataOptions()
				.UseConnectionString(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), default, version), connectionString));
		}

		public static DataConnection CreateDataConnection(DbConnection connection, FirebirdVersion version = FirebirdVersion.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), default, version), connection));
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, FirebirdVersion version = FirebirdVersion.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), default, version), transaction));
		}

		#endregion

		public static void ClearAllPools() => FirebirdProviderAdapter.Instance.ClearAllPools();
	}
}
