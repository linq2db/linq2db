using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.Informix
{
	using Data;

	public static class InformixTools
	{
		internal static InformixProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(InformixProvider provider = InformixProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(connectionString), provider, default);
		}

		[Obsolete($"Use overload with {nameof(InformixProvider)} parameter")]
		public static IDataProvider GetDataProvider(string? providerName = null, string? connectionString = null)
		{
			return providerName switch
			{
				ProviderName.Informix    => GetDataProvider(InformixProvider.Informix, connectionString),
				ProviderName.InformixDB2 => GetDataProvider(InformixProvider.DB2, connectionString),
				_                        => GetDataProvider(InformixProvider.AutoDetect, connectionString),
			};
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, InformixProvider provider = InformixProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider, connectionString), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, InformixProvider provider = InformixProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, InformixProvider provider = InformixProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), transaction);
		}

		[Obsolete($"Use overload with {nameof(InformixProvider)} parameter")]
		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName, connectionString), connectionString);
		}

		[Obsolete($"Use overload with {nameof(InformixProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		[Obsolete($"Use overload with {nameof(InformixProvider)} parameter")]
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
