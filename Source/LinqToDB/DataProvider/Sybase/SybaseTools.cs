using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;
	using Common;
	using Configuration;

	public static class SybaseTools
	{
#if NET45 || NET46
		private static readonly Lazy<IDataProvider> _sybaseNativeDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new SybaseDataProvider(ProviderName.Sybase);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);
#endif
		private static readonly Lazy<IDataProvider> _sybaseManagedDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new SybaseDataProvider(ProviderName.SybaseManaged);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			switch (css.ProviderName)
			{
				case SybaseProviderAdapter.ManagedClientNamespace:
				case ProviderName.SybaseManaged                  : return _sybaseManagedDataProvider.Value;
#if NET45 || NET46
				case "Sybase.Native"                             :
				case SybaseProviderAdapter.NativeClientNamespace :
				case SybaseProviderAdapter.NativeAssemblyName    : return _sybaseNativeDataProvider.Value;
#endif
				case ""                                          :
				case null                                        :
					if (css.Name.Contains("Sybase"))
						goto case ProviderName.Sybase;
					break;
				case ProviderName.Sybase                         :
					if (css.Name.Contains("Managed"))
						return _sybaseManagedDataProvider.Value;
#if NET45 || NET46
					if (css.Name.Contains("Native"))
						return _sybaseNativeDataProvider.Value;
#endif
					return GetDataProvider();
			}

			return null;
		}

		private static string? _detectedProviderName;
		public  static string  DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		private static string DetectProviderName()
		{
			var path = typeof(SybaseTools).Assembly.GetPath();

			if (File.Exists(Path.Combine(path, $"{SybaseProviderAdapter.ManagedAssemblyName}.dll")))
				return ProviderName.SybaseManaged;

			return ProviderName.Sybase;
		}

		public static IDataProvider GetDataProvider(string? providerName = null, string? assemblyName = null)
		{
#if NET45 || NET46
			if (assemblyName == SybaseProviderAdapter.NativeAssemblyName)  return _sybaseNativeDataProvider.Value;
			if (assemblyName == SybaseProviderAdapter.ManagedAssemblyName) return _sybaseManagedDataProvider.Value;

			switch (providerName)
			{
				case ProviderName.Sybase       : return _sybaseNativeDataProvider.Value;
				case ProviderName.SybaseManaged: return _sybaseManagedDataProvider.Value;
			}

			if (DetectedProviderName == ProviderName.Sybase)
				return _sybaseNativeDataProvider.Value;
#endif

			return _sybaseManagedDataProvider.Value;
		}

		public static void ResolveSybase(string path)
		{
			new AssemblyResolver(
				path,
				DetectedProviderName == ProviderName.Sybase
					? SybaseProviderAdapter.NativeAssemblyName
					: SybaseProviderAdapter.ManagedAssemblyName);
		}

		public static void ResolveSybase(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.FullName!);
		}

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
		// don't set ProviderSpecific as default type while SAP not fix incorrect bit field value
		// insert for first record
		/// <summary>
		/// Using <see cref="BulkCopyType.ProviderSpecific"/> mode with bit and identity fields could lead to following errors:
		/// - bit: <c>false</c> inserted into bit field for first record even if <c>true</c> provided;
		/// - identity: bulk copy operation fail with exception: "Bulk insert failed. Null value is not allowed in not null column.".
		/// Those are provider bugs and could be fixed in latest versions.
		/// </summary>
		public static BulkCopyType DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection              dataConnection,
			IEnumerable<T>              source,
			int                         maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied>? rowsCopiedCallback = null)
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

		#endregion
	}
}
