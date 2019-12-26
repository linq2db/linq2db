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

	public enum AlternativeBulkCopy
	{
		InsertAll,
		InsertInto,
		InsertDual
	}

	public static partial class OracleTools
	{
#if NET45 || NET46
		private static readonly Lazy<IDataProvider> _oracleNativeDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new OracleDataProvider(ProviderName.OracleNative);

			DataConnection.AddDataProvider(provider);

			if (DetectedProviderName == ProviderName.OracleNative)
				DataConnection.AddDataProvider(ProviderName.Oracle, provider);

			return provider;
		}, true);
#endif

		private static readonly Lazy<IDataProvider> _oracleManagedDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new OracleDataProvider(ProviderName.OracleManaged);

			DataConnection.AddDataProvider(provider);

			if (DetectedProviderName == ProviderName.OracleManaged)
				DataConnection.AddDataProvider(ProviderName.Oracle, provider);

			return provider;
		}, true);

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			switch (css.ProviderName)
			{
#if NET45 || NET46
				case "Oracle.DataAccess"              :
				case "Oracle.DataAccess.Client"       :
				case ProviderName.OracleNative        : return _oracleNativeDataProvider.Value;
#endif
				case "Oracle.ManagedDataAccess"       :
				case "Oracle.ManagedDataAccess.Core"  :
				case "Oracle.ManagedDataAccess.Client":
				case ProviderName.OracleManaged       : return _oracleManagedDataProvider.Value;
				case ""                               :
				case null                             :

					if (css.Name.Contains("Oracle"))
						goto case ProviderName.Oracle;
					break;
				case ProviderName.Oracle              :
#if NET45 || NET46
					if (css.Name.Contains("Native"))
						return _oracleNativeDataProvider.Value;
#endif

					if (css.Name.Contains("Managed"))
						return _oracleManagedDataProvider.Value;

					return GetDataProvider();
			}

			return null;
		}

		static string? _detectedProviderName;

		public static string  DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		static string DetectProviderName()
		{
#if NET45 || NET46
			try
			{
				var path = typeof(OracleTools).Assembly.GetPath();
				if (!File.Exists(Path.Combine(path, "Oracle.DataAccess.dll")))
					if (File.Exists(Path.Combine(path, "Oracle.ManagedDataAccess.dll")))
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

		public static IDataProvider GetDataProvider(string? providerName = null, string? assemblyName = null)
		{
#if NET45 || NET46
			if (assemblyName == OracleWrappers.NativeAssemblyName)  return _oracleManagedDataProvider.Value;
			if (assemblyName == OracleWrappers.ManagedAssemblyName) return _oracleManagedDataProvider.Value;

			switch (providerName)
			{
				case ProviderName.OracleNative : return _oracleNativeDataProvider.Value;
				case ProviderName.OracleManaged: return _oracleManagedDataProvider.Value;
			}

			return DetectedProviderName == ProviderName.OracleNative
				? _oracleNativeDataProvider.Value
				: _oracleManagedDataProvider.Value;
#else
			return _oracleManagedDataProvider.Value;
#endif
		}

		public static void ResolveOracle(string path)       => new AssemblyResolver(
			path,
#if NET45 || NET46
			DetectedProviderName == ProviderName.OracleManaged
				? OracleWrappers.ManagedAssemblyName
				: OracleWrappers.NativeAssemblyName
#else
			OracleWrappers.ManagedAssemblyName
#endif
			);

		public static void ResolveOracle(Assembly assembly) => new AssemblyResolver(assembly, assembly.FullName);

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

		public static AlternativeBulkCopy UseAlternativeBulkCopy = AlternativeBulkCopy.InsertAll;

		public static Func<IDataReader,int,decimal> DataReaderGetDecimal = (dr, i) => dr.GetDecimal(i);

		/// <summary>
		/// Gets or sets flag to tell LinqToDB to quote identifiers, if they contain lowercase letters.
		/// Default value: <c>true</c>.
		/// This flag added for backward compatibility and will be removed later, so it is recommended to
		/// set it to <c>false</c> and and fix mappings to use uppercase letters for non-quoted identifiers.
		/// </summary>
		public static bool DontEscapeLowercaseIdentifiers { get; set; } = true;
	}
}
