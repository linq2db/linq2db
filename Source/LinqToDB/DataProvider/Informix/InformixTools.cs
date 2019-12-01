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
		static readonly InformixDataProvider _informixDataProvider    = new InformixDataProvider(ProviderName.Informix);
#endif
		static readonly InformixDataProvider _informixDB2DataProvider = new InformixDataProvider(ProviderName.InformixDB2);

		public static bool AutoDetectProvider { get; set; }

		static InformixTools()
		{
			AutoDetectProvider = true;

#if NET45 || NET46
			DataConnection.AddDataProvider(_informixDataProvider);
#endif
			DataConnection.AddDataProvider(_informixDB2DataProvider);

#if !NETCOREAPP2_1
			DataConnection.AddProviderDetector(ProviderDetector);
#endif
		}

#if !NETCOREAPP2_1
		static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			// IBM.Data.DB2.Core already mapped to DB2 provider...
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
					return _informixDB2DataProvider;

				case "IBM.Data.Informix":
#if NET45 || NET46
					return _informixDataProvider;
#else
					return _informixDB2DataProvider;
#endif
			}

			return null;
		}
#endif

		private static string DetectProviderName()
		{
#if NET45 || NET46
			var path = typeof(InformixTools).Assembly.GetPath();

			if (File.Exists(Path.Combine(path, "IBM.Data.Informix.dll")))
				return ProviderName.Informix;
#endif

			return ProviderName.InformixDB2;
		}

		private static string? _detectedProviderName;
		public static string DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		private static InformixDataProvider DetectedProvider =>
#if NET45 || NET46
			DetectedProviderName == ProviderName.Informix
				? _informixDataProvider :
#endif
				_informixDB2DataProvider;

		public static IDataProvider GetDataProvider()
		{
			return DetectedProvider;
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(DetectedProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, string? providerName = null)
		{
			switch (providerName)
			{
#if NET45 || NET46
				case ProviderName.Informix   : return new DataConnection(_informixDataProvider, connection);
#endif
				case ProviderName.InformixDB2: return new DataConnection(_informixDB2DataProvider, connection);
			}

			return new DataConnection(DetectedProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(DetectedProvider, transaction);
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
