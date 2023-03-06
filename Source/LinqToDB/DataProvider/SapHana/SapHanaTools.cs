using System;
using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SapHana
{
	using Data;

	[PublicAPI]
	public static class SapHanaTools
	{
		internal static SapHanaProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static void ResolveSapHana(string path, string? assemblyName = null)
		{
			_ = new AssemblyResolver(path, assemblyName ?? OdbcProviderAdapter.AssemblyName);
		}

		public static void ResolveSapHana(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		public static IDataProvider GetDataProvider(SapHanaProvider provider = SapHanaProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(connectionString), provider, default);
		}


		[Obsolete($"Use overload with {nameof(SapHanaProvider)} parameter")]
		public static IDataProvider GetDataProvider(string? providerName = null, string? assemblyName = null)
		{
			return (assemblyName, providerName) switch
			{
				(SapHanaProviderAdapter.AssemblyName, _) => GetDataProvider(SapHanaProvider.Unmanaged),
				(OdbcProviderAdapter.AssemblyName, _)    => GetDataProvider(SapHanaProvider.ODBC),
				(_, ProviderName.SapHanaOdbc)            => GetDataProvider(SapHanaProvider.ODBC),
				(_, ProviderName.SapHanaNative)          => GetDataProvider(SapHanaProvider.Unmanaged),
				_                                        => GetDataProvider(SapHanaProvider.AutoDetect)
			};
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, SapHanaProvider provider = SapHanaProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider, connectionString), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, SapHanaProvider provider = SapHanaProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, SapHanaProvider provider = SapHanaProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), transaction);
		}

		[Obsolete($"Use overload with {nameof(SapHanaProvider)} parameter")]
		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName, connectionString), connectionString);
		}

		[Obsolete($"Use overload with {nameof(SapHanaProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		[Obsolete($"Use overload with {nameof(SapHanaProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

		#endregion

		[Obsolete("Use SapHanaOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => SapHanaOptions.Default.BulkCopyType;
			set => SapHanaOptions.Default = SapHanaOptions.Default with { BulkCopyType = value };
		}
	}
}
