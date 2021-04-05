using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.Informix
{
	using System.Data.Common;
	using System.IO;
	using Data;
	using LinqToDB.Common;
	using LinqToDB.Configuration;
	using LinqToDB.DataProvider.DB2;

	public static class InformixTools
	{
#if NETFRAMEWORK
		private static readonly Lazy<IDataProvider> _informixDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new InformixDataProvider(ProviderName.Informix);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);
#endif

		private static readonly Lazy<IDataProvider> _informixDB2DataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new InformixDataProvider(ProviderName.InformixDB2);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			switch (css.ProviderName)
			{
				case ProviderName.InformixDB2:
					return _informixDB2DataProvider.Value;
#if NETFRAMEWORK
				case InformixProviderAdapter.IfxClientNamespace:
					return _informixDataProvider.Value;
#endif
				case ""                      :
				case null                    :
				case DB2ProviderAdapter.NetFxClientNamespace:
				case DB2ProviderAdapter.CoreClientNamespace :

					// this check used by both Informix and DB2 providers to avoid conflicts
					if (css.Name.Contains("Informix"))
						goto case ProviderName.Informix;
					break;
				case ProviderName.Informix   :
					if (css.Name.Contains("DB2"))
						return _informixDB2DataProvider.Value;

#if NETFRAMEWORK
					return _informixDataProvider.Value;
#else
					return _informixDB2DataProvider.Value;
#endif
			}

			return null;
		}

		private static string DetectProviderName()
		{
#if NETFRAMEWORK
			var path = typeof(InformixTools).Assembly.GetPath();

			if (File.Exists(Path.Combine(path, $"{InformixProviderAdapter.IfxAssemblyName}.dll")))
				return ProviderName.Informix;
#endif

			return ProviderName.InformixDB2;
		}

		private  static string? _detectedProviderName;
		internal static string DetectedProviderName =>
			_detectedProviderName ??= DetectProviderName();

		public static IDataProvider GetDataProvider(string? providerName = null)
		{
			switch (providerName ?? DetectedProviderName)
			{
#if NETFRAMEWORK
				case ProviderName.Informix   : return _informixDataProvider.Value;
#endif
				case ProviderName.InformixDB2: return _informixDB2DataProvider.Value;
			}

#if NETFRAMEWORK
				return _informixDataProvider.Value;
#else
				return _informixDB2DataProvider.Value;
#endif
		}

#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

#endregion

#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.ProviderSpecific;

		[Obsolete("Please use the BulkCopy extension methods within DataConnectionExtensions")]
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
					BulkCopyType       = BulkCopyType.ProviderSpecific,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

#endregion
	}
}
