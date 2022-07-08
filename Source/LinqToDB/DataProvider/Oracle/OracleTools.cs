using System;
using System.Data.Common;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.Oracle
{
	using Common.Internal.Cache;
	using Common;
	using Configuration;
	using Data;

	public static partial class OracleTools
	{
		static readonly Lazy<IDataProvider> _oracleNativeDataProvider11 = DataConnection.CreateDataProvider<OracleDataProviderNative11>();
		static readonly Lazy<IDataProvider> _oracleNativeDataProvider12 = DataConnection.CreateDataProvider<OracleDataProviderNative12>();

		static readonly Lazy<IDataProvider> _oracleManagedDataProvider11 = DataConnection.CreateDataProvider<OracleDataProviderManaged11>();
		static readonly Lazy<IDataProvider> _oracleManagedDataProvider12 = DataConnection.CreateDataProvider<OracleDataProviderManaged12>();

//		static readonly MemoryCache<(bool managed, string connectionString)> _providerCache = new(new ());

		static readonly Lazy<IDataProvider> _oracleDevartDataProvider11 = DataConnection.CreateDataProvider<OracleDataProviderDevart11>();
		static readonly Lazy<IDataProvider> _oracleDevartDataProvider12 = DataConnection.CreateDataProvider<OracleDataProviderDevart12>();

		public static bool          AutoDetectProvider { get; set; } = true;

		public static OracleVersion DefaultVersion = OracleVersion.v12;

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			OracleProvider? provider = null;

			switch (css.ProviderName)
			{
				case OracleProviderAdapter.NativeAssemblyName    :
				case OracleProviderAdapter.NativeClientNamespace :
				case ProviderName.OracleNative                   :
				case ProviderName.Oracle11Native                 :
					provider = OracleProvider.Native;
					goto case ProviderName.Oracle;
				case OracleProviderAdapter.DevartAssemblyName    :
				case ProviderName.OracleDevart                   :
				case ProviderName.Oracle11Devart                 :
					provider = OracleProvider.Devart;
					goto case ProviderName.Oracle;
				case OracleProviderAdapter.ManagedAssemblyName   :
				case OracleProviderAdapter.ManagedClientNamespace:
				case "Oracle.ManagedDataAccess.Core"             :
				case ProviderName.OracleManaged                  :
				case ProviderName.Oracle11Managed                :
					provider = OracleProvider.Managed;
					goto case ProviderName.Oracle;
				case ""                                          :
				case null                                        :

					if (css.Name.Contains("Oracle"))
						goto case ProviderName.Oracle;
					break;
				case ProviderName.Oracle                         :
					if (provider == null)
					{
						if (css.Name.Contains("Native") || css.ProviderName?.Contains("Native") == true)
							provider = OracleProvider.Native;
						else if (css.Name.Contains("Devart") || css.ProviderName?.Contains("Devart") == true)
							provider = OracleProvider.Devart;
						else
							provider = OracleProvider.Managed;
					}

					if (css.Name.Contains("11") || css.ProviderName?.Contains("11") == true) return GetDataProvider(OracleVersion.v11, provider.Value);
					if (css.Name.Contains("12") || css.ProviderName?.Contains("12") == true) return GetDataProvider(OracleVersion.v12, provider.Value);
					if (css.Name.Contains("18") || css.ProviderName?.Contains("18") == true) return GetDataProvider(OracleVersion.v12, provider.Value);
					if (css.Name.Contains("19") || css.ProviderName?.Contains("19") == true) return GetDataProvider(OracleVersion.v12, provider.Value);
					if (css.Name.Contains("21") || css.ProviderName?.Contains("21") == true) return GetDataProvider(OracleVersion.v12, provider.Value);

					var version = AutoDetectProvider ? DetectProviderVersion(css, connectionString, provider.Value) : DefaultVersion;

					return GetDataProvider(version, provider.Value);
			}

			return null;
		}

		private static OracleVersion DetectProviderVersion(IConnectionStringSettings css, string connectionString, OracleProvider provider)
		{
			try
			{
				var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;

				var providerAdapter = OracleProviderAdapter.GetInstance(provider);

				using (var conn = providerAdapter.CreateConnection(cs))
				{
					conn.Open();

					var command = conn.CreateCommand();
					command.CommandText = "SELECT  VERSION from PRODUCT_COMPONENT_VERSION WHERE ROWNUM = 1";
					if (command.ExecuteScalar() is string result)
					{
						var version = int.Parse(result.Split('.')[0]);

						if (version <= 11)
							return OracleVersion.v11;

						return OracleVersion.v12;
					}
					return DefaultVersion;
				}
			}
			catch
			{
				return DefaultVersion;
			}
		}

		public static IDataProvider GetDataProvider(
			OracleVersion  version  = OracleVersion.v12,
			OracleProvider provider = OracleProvider.Managed)
		{
			return (provider, version) switch
			{
				(OracleProvider.Native , OracleVersion.v11) => _oracleNativeDataProvider11 .Value,
				(OracleProvider.Native , OracleVersion.v12) => _oracleNativeDataProvider12 .Value,
				(OracleProvider.Managed, OracleVersion.v11) => _oracleManagedDataProvider11.Value,
				(OracleProvider.Managed, OracleVersion.v12) => _oracleManagedDataProvider12.Value,
				(OracleProvider.Devart , OracleVersion.v11) => _oracleDevartDataProvider11 .Value,
				(OracleProvider.Devart , OracleVersion.v12) => _oracleDevartDataProvider12 .Value,
				_                                           => _oracleManagedDataProvider12.Value,
			};
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
			version ??= DefaultVersion;

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
		public static void ResolveOracle(Assembly assembly) => new AssemblyResolver(assembly, assembly.FullName!);

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
			}

			return ProviderName.OracleNative;
		}

		[Obsolete("This API will be removed in v5")]
		private static IDataProvider GetVersionedDataProvider(OracleVersion version, bool managed)
		{
			if (!managed)
			{
				return version switch
				{
					OracleVersion.v11 => _oracleNativeDataProvider11.Value,
					_ => _oracleNativeDataProvider12.Value,
				};
			}

			return version switch
			{
				OracleVersion.v11 => _oracleManagedDataProvider11.Value,
				_ => _oracleManagedDataProvider12.Value,
			};
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
