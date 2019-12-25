using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.Informix
{
	using System.IO;
	using Data;
	using LinqToDB.Common;
	using LinqToDB.Configuration;

	public static class InformixTools
	{
#if NET45 || NET46
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
			// NOTE: IBM.Data.DB2.Core already mapped to DB2 provider
			switch (css.ProviderName)
			{
				case "":
				case null:

					if (css.Name == "Informix.Core")
						goto case "IBM.Data.Informix.Core";
					if (css.Name == "Informix")
						goto case "IBM.Data.Informix";
					break;

				case "Informix.Core":
				case "IBM.Data.Informix.Core":
					return _informixDB2DataProvider.Value;

				case "IBM.Data.Informix":
#if NET45 || NET46
					return _informixDataProvider.Value;
#else
					return _informixDB2DataProvider.Value;
#endif
			}

			return null;
		}

		private static string DetectProviderName()
		{
#if NET45 || NET46
			var path = typeof(InformixTools).Assembly.GetPath();

			if (File.Exists(Path.Combine(path, "IBM.Data.Informix.dll")))
				return ProviderName.Informix;
#endif

			return ProviderName.InformixDB2;
		}

		private  static string? _detectedProviderName;
		internal static string DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		public static IDataProvider GetDataProvider(string? providerName = null)
		{
			switch (providerName ?? DetectedProviderName)
			{
#if NET45 || NET46
				case ProviderName.Informix   : return _informixDataProvider.Value;
#endif
				case ProviderName.InformixDB2: return _informixDB2DataProvider.Value;
			}

#if NET45 || NET46
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

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.ProviderSpecific;

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
