using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Configuration;
	using Data;

	/// <summary>
	/// Defines type of multi-row INSERT operation to generate for <see cref="BulkCopyType.RowByRow"/> bulk copy mode.
	/// </summary>
	public enum AlternativeBulkCopy
	{
		/// <summary>
		/// This mode generates INSERT ALL statement.
		/// Note that INSERT ALL doesn't support sequences and will use single generated value for all rows.
		/// <code>
		/// INSERT ALL
		///     INTO target_table VALUES(/*row data*/)
		///     ...
		///     INTO target_table VALUES(/*row data*/)
		/// </code>
		/// </summary>
		InsertAll,
		/// <summary>
		/// This mode performs regular INSERT INTO query with array of values for each column.
		/// <code>
		/// INSERT INTO target_table(/*columns*/)
		///     VALUES(:column1ArrayParameter, ..., :columnXArrayParameter)
		/// </code>
		/// </summary>
		InsertInto,
		/// <summary>
		/// This mode generates INSERT ... SELECT statement.
		/// <code>
		/// INSERT INTO target_table(/*columns*/)
		///     SELECT /*row data*/ FROM DUAL
		///     UNION ALL
		///     ...
		///     UNION ALL
		///     SELECT /*row data*/ FROM DUAL
		/// </code>
		/// </summary>
		InsertDual
	}

	public static partial class OracleTools
	{
#if NETFRAMEWORK
		static readonly Lazy<IDataProvider> _oracleNativeDataProvider11 = DataConnection.CreateDataProvider<OracleDataProviderNative11>();
		static readonly Lazy<IDataProvider> _oracleNativeDataProvider12 = DataConnection.CreateDataProvider<OracleDataProviderNative12>();
#endif

		static readonly Lazy<IDataProvider> _oracleManagedDataProvider11 = DataConnection.CreateDataProvider<OracleDataProviderManaged11>();
		static readonly Lazy<IDataProvider> _oracleManagedDataProvider12 = DataConnection.CreateDataProvider<OracleDataProviderManaged12>();

		public static bool AutoDetectProvider { get; set; } = true;

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			bool? managed = null;
			switch (css.ProviderName)
			{
#if NETFRAMEWORK
				case OracleProviderAdapter.NativeAssemblyName    :
				case OracleProviderAdapter.NativeClientNamespace :
				case ProviderName.OracleNative                   :
					managed = false;
					goto case ProviderName.Oracle;
#endif
				case OracleProviderAdapter.ManagedAssemblyName   :
				case OracleProviderAdapter.ManagedClientNamespace:
				case "Oracle.ManagedDataAccess.Core"             :
				case ProviderName.OracleManaged                  :
					managed = true;
					goto case ProviderName.Oracle;
				case ""                                          :
				case null                                        :

					if (css.Name.Contains("Oracle"))
						goto case ProviderName.Oracle;
					break;
				case ProviderName.Oracle                         :
#if NETFRAMEWORK
					if (css.Name.Contains("Native") || managed == false)
					{
						if (css.Name.Contains("11"))
							return _oracleNativeDataProvider11.Value;
						if (css.Name.Contains("12"))
							return _oracleNativeDataProvider12.Value;
						return GetDataProvider(css, connectionString, false);
					}
#endif

					if (css.Name.Contains("Managed") || managed == true)
					{
						if (css.Name.Contains("11"))
							return _oracleManagedDataProvider11.Value;
						if (css.Name.Contains("12"))
							return _oracleManagedDataProvider12.Value;
						return GetDataProvider(css, connectionString, true);
					}

					return GetDataProvider();
			}

			return null;
		}

		private static OracleVersion DetectProviderVersion(IConnectionStringSettings css, string connectionString, bool managed)
		{
			try
			{
				var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;

				OracleProviderAdapter providerAdapter;

#if NETFRAMEWORK
				if (!managed)
					providerAdapter = OracleProviderAdapter.GetInstance(ProviderName.OracleNative);
				else
#endif
					providerAdapter = OracleProviderAdapter.GetInstance(ProviderName.OracleManaged);

				using (var conn = providerAdapter.CreateConnection(cs))
				{
					conn.Open();

					var command = conn.CreateCommand();
					command.CommandText =
						"select VERSION from PRODUCT_COMPONENT_VERSION where PRODUCT like 'PL/SQL%'";
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

		public static OracleVersion DefaultVersion = OracleVersion.v12;

		static string? _detectedProviderName;

		private static IDataProvider GetDataProvider(IConnectionStringSettings css, string connectionString, bool managed)
		{
			var version = DefaultVersion;
			if (AutoDetectProvider)
				version = DetectProviderVersion(css, connectionString, managed);

			return GetVersionedDataProvider(version, managed);
		}

		private static IDataProvider GetVersionedDataProvider(OracleVersion version, bool managed)
		{
#if NETFRAMEWORK
			if (!managed)
			{
				return version switch
				{
					OracleVersion.v11 => _oracleNativeDataProvider11.Value,
					_                 => _oracleNativeDataProvider12.Value,
				};
			}
#endif
			return version switch
			{
				OracleVersion.v11 => _oracleManagedDataProvider11.Value,
				_                 => _oracleManagedDataProvider12.Value,
			};
		}

		public static string  DetectedProviderName =>
			_detectedProviderName ??= DetectProviderName();

		private static string DetectProviderName()
		{
#if NETFRAMEWORK
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
#else
			return ProviderName.OracleManaged;
#endif
		}

		public static IDataProvider GetDataProvider(string? providerName = null, string? assemblyName = null, OracleVersion? version = null)
		{
			version ??= DefaultVersion;

#if NETFRAMEWORK
			if (assemblyName == OracleProviderAdapter.NativeAssemblyName ) return GetVersionedDataProvider(version.Value, false);
			if (assemblyName == OracleProviderAdapter.ManagedAssemblyName) return GetVersionedDataProvider(version.Value, true);

			return providerName switch
			{
				ProviderName.OracleNative  => GetVersionedDataProvider(version.Value, false),
				ProviderName.OracleManaged => GetVersionedDataProvider(version.Value, true),
				_						   =>
					DetectedProviderName == ProviderName.OracleNative
					? GetVersionedDataProvider(version.Value, false)
					: GetVersionedDataProvider(version.Value, true),
			};
#else
			return GetVersionedDataProvider(version.Value, true);
#endif
		}

		public static void ResolveOracle(string path)       => new AssemblyResolver(
			path,
#if NETFRAMEWORK
			DetectedProviderName == ProviderName.OracleManaged
				? OracleProviderAdapter.ManagedAssemblyName
				: OracleProviderAdapter.NativeAssemblyName
#else
			OracleProviderAdapter.ManagedAssemblyName
#endif
			);

		public static void ResolveOracle(Assembly assembly) => new AssemblyResolver(assembly, assembly.FullName!);

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

		#endregion

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		[Obsolete("Please use the BulkCopy extension methods within DataConnectionExtensions")]
		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			this DataConnection          dataConnection,
			IEnumerable<T>               source,
			int                          maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied>?  rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		[Obsolete("Please use the BulkCopy extension methods within DataConnectionExtensions")]
		public static BulkCopyRowsCopied ProviderSpecificBulkCopy<T>(
			DataConnection               dataConnection,
			IEnumerable<T>               source,
			int?                         maxBatchSize       = null,
			int?                         bulkCopyTimeout    = null,
			int                          notifyAfter        = 0,
			Action<BulkCopyRowsCopied>?  rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.ProviderSpecific,
					MaxBatchSize       = maxBatchSize,
					BulkCopyTimeout    = bulkCopyTimeout,
					NotifyAfter        = notifyAfter,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion

		/// <summary>
		/// Specifies type of multi-row INSERT operation to generate for <see cref="BulkCopyType.RowByRow"/> bulk copy mode.
		/// Default value: <see cref="AlternativeBulkCopy.InsertAll"/>.
		/// </summary>
		public static AlternativeBulkCopy UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;

		[Obsolete("This field is not used by linq2db. Configure reader expressions on DataProvider directly")]
		public static Func<IDataReader,int,decimal> DataReaderGetDecimal = (dr, i) => dr.GetDecimal(i);

		/// <summary>
		/// Gets or sets flag to tell LinqToDB to quote identifiers, if they contain lowercase letters.
		/// Default value: <c>false</c>.
		/// This flag is added for backward compatibility and not recommended for use with new applications.
		/// </summary>
		public static bool DontEscapeLowercaseIdentifiers { get; set; }
	}
}
