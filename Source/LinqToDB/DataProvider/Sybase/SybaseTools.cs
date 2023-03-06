using System;
using System.Data.Common;
using System.Reflection;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;

	public static class SybaseTools
	{
		internal static SybaseProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(SybaseProvider provider = SybaseProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(connectionString), provider, default);
		}

		[Obsolete($"Use overload with {nameof(SybaseProvider)} parameter")]
		public static IDataProvider GetDataProvider(string? providerName = null, string? assemblyName = null)
		{
			if (assemblyName == SybaseProviderAdapter.NativeAssemblyName)  return GetDataProvider(SybaseProvider.Unmanaged);
			if (assemblyName == SybaseProviderAdapter.ManagedAssemblyName) return GetDataProvider(SybaseProvider.DataAction);

			switch (providerName)
			{
				case ProviderName.Sybase       : return GetDataProvider(SybaseProvider.Unmanaged);
				case ProviderName.SybaseManaged: return GetDataProvider(SybaseProvider.DataAction);
			}

			return GetDataProvider(SybaseProvider.AutoDetect);
		}

		public static void ResolveSybase(string path, string? assemblyName = null)
		{
			_ = new AssemblyResolver(path, assemblyName ?? SybaseProviderAdapter.ManagedAssemblyName);
		}

		public static void ResolveSybase(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection
		public static DataConnection CreateDataConnection(string connectionString, SybaseProvider provider = SybaseProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider, connectionString), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, SybaseProvider provider = SybaseProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, SybaseProvider provider = SybaseProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), transaction);
		}

		[Obsolete($"Use overload with {nameof(SybaseProvider)} parameter")]
		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName, connectionString), connectionString);
		}

		[Obsolete($"Use overload with {nameof(SybaseProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		[Obsolete($"Use overload with {nameof(SybaseProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

		#endregion

		#region BulkCopy

		// don't set ProviderSpecific as default type while SAP not fix incorrect bit field value
		// insert for first record
		/// <summary>
		/// Using <see cref="BulkCopyType.ProviderSpecific"/> mode with bit and identity fields could lead to following errors:
		/// - bit: <c>false</c> inserted into bit field for first record even if <c>true</c> provided;
		/// - identity: bulk copy operation fail with exception: "Bulk insert failed. Null value is not allowed in not null column.".
		/// Those are provider bugs and could be fixed in latest versions.
		/// </summary>
		[Obsolete("Use SybaseOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => SybaseOptions.Default.BulkCopyType;
			set => SybaseOptions.Default = SybaseOptions.Default with { BulkCopyType = value };
		}

		#endregion
	}
}
