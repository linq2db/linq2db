using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
	using Configuration;
	using Data;
	using DB2;
	using Extensions;

	public static class InformixTools
	{
#if NETFRAMEWORK
		static readonly Lazy<IDataProvider> _informixDataProvider = DataConnection.CreateDataProvider<InformixDataProviderInformix>();
#endif

		static readonly Lazy<IDataProvider> _informixDB2DataProvider = DataConnection.CreateDataProvider<InformixDataProviderDB2>();

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
			switch (options.ProviderName)
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
					if (options.ConfigurationString?.ContainsEx("Informix") == true)
						goto case ProviderName.Informix;
					break;
				case ProviderName.Informix   :
					if (options.ConfigurationString?.ContainsEx("DB2") == true)
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

		[Obsolete("Use InformixOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => InformixOptions.Default.BulkCopyType;
			set => InformixOptions.Default = InformixOptions.Default with { BulkCopyType = value };
		}

		#endregion
	}
}
