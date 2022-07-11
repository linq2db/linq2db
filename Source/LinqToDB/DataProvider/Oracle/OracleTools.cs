using System;
using System.Data.Common;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Data;

	public static partial class OracleTools
	{
		internal static OracleProviderDetector ProviderDetector = new();

		public static OracleVersion DefaultVersion
		{
			get => ProviderDetector.DefaultVersion;
			set => ProviderDetector.DefaultVersion = value;
		}

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(
			OracleVersion  version          = OracleVersion.v12,
			OracleProvider provider         = OracleProvider.Managed,
			string?        connectionString = null)
		{
			return ProviderDetector.GetDataProvider(provider, version, connectionString);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(
			string connectionString,
			OracleVersion version   = OracleVersion.v12,
			OracleProvider provider = OracleProvider.Managed)
		{
			return new DataConnection(GetDataProvider(version, provider), connectionString);
		}

		public static DataConnection CreateDataConnection(
			DbConnection connection,
			OracleVersion version   = OracleVersion.v12,
			OracleProvider provider = OracleProvider.Managed)
		{
			return new DataConnection(GetDataProvider(version, provider), connection);
		}

		public static DataConnection CreateDataConnection(
			DbTransaction transaction,
			OracleVersion version   = OracleVersion.v12,
			OracleProvider provider = OracleProvider.Managed)
		{
			return new DataConnection(GetDataProvider(version, provider), transaction);
		}

		#region Obsoleted (V5 remove)
		[Obsolete("This API will be removed in v5")]
		static string? _detectedProviderName;
		[Obsolete("This API will be removed in v5")]
		public static string DetectedProviderName => _detectedProviderName ??= DetectProviderName();

		[Obsolete("Use GetDataProvider(OracleVersion, OracleProvider) overload")]
		public static IDataProvider GetDataProvider(string? providerName, string? assemblyName = null, OracleVersion? version = null)
		{
			version ??= ProviderDetector.DefaultVersion;

			if (assemblyName == OracleProviderAdapter.NativeAssemblyName)  return GetVersionedDataProvider(version.Value, false);
			if (assemblyName == OracleProviderAdapter.ManagedAssemblyName) return GetVersionedDataProvider(version.Value, true);

			return providerName switch
			{
				ProviderName.OracleNative  => GetVersionedDataProvider(version.Value, false),
				ProviderName.OracleManaged => GetVersionedDataProvider(version.Value, true),
				_ =>
					DetectedProviderName == ProviderName.OracleNative
						? GetVersionedDataProvider(version.Value, false)
						: GetVersionedDataProvider(version.Value, true),
			};
		}

		[Obsolete("This API will be removed in v5")]
		public static void ResolveOracle(string path) => _ = new AssemblyResolver(
			path,
			DetectedProviderName == ProviderName.OracleManaged
				? OracleProviderAdapter.ManagedAssemblyName
				: OracleProviderAdapter.NativeAssemblyName);

		[Obsolete("This API will be removed in v5")]
		public static void ResolveOracle(Assembly assembly) => _ = new AssemblyResolver(assembly, assembly.FullName!);

		[Obsolete("Use CreateDataConnection(string, OracleVersion, OracleProvider) overload")]
		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connectionString);
		}

		[Obsolete("Use CreateDataConnection(DbConnection, OracleVersion, OracleProvider) overload")]
		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		[Obsolete("Use CreateDataConnection(DbTransaction, OracleVersion, OracleProvider) overload")]
		public static DataConnection CreateDataConnection(DbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

		[Obsolete("This API will be removed in v5")]
		private static string DetectProviderName()
		{
			try
			{
				var path = typeof(OracleTools).Assembly.GetPath();
				if (!File.Exists(Path.Combine(path, $"{OracleProviderAdapter.NativeAssemblyName}.dll")))
					if (File.Exists(Path.Combine(path, $"{OracleProviderAdapter.ManagedAssemblyName}.dll")))
						return ProviderName.OracleManaged;
			}
			catch
			{
				// ignored
			}

			return ProviderName.OracleNative;
		}

		[Obsolete("This API will be removed in v5")]
		private static IDataProvider GetVersionedDataProvider(OracleVersion version, bool managed)
		{
			return GetDataProvider(version, managed ? OracleProvider.Managed : OracleProvider.Native);
		}

		#endregion

		#endregion

		#region BulkCopy
		public static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;
		#endregion

		/// <summary>
		/// Specifies type of multi-row INSERT operation to generate for <see cref="BulkCopyType.RowByRow"/> bulk copy mode.
		/// Default value: <see cref="AlternativeBulkCopy.InsertAll"/>.
		/// </summary>
		public static AlternativeBulkCopy UseAlternativeBulkCopy
		{
			get => OracleOptions.Default.AlternativeBulkCopy;
			set => OracleOptions.Default = OracleOptions.Default with { AlternativeBulkCopy = value };
		}

		/// <summary>
		/// Gets or sets flag to tell LinqToDB to quote identifiers, if they contain lowercase letters.
		/// Default value: <c>false</c>.
		/// This flag is added for backward compatibility and not recommended for use with new applications.
		/// </summary>
		public static bool DontEscapeLowercaseIdentifiers { get; set; }
	}
}
