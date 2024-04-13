using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Data;

	public static partial class ClickHouseTools
	{
		internal static ClickHouseProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static IDataProvider GetDataProvider(ClickHouseProvider provider = ClickHouseProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(connectionString), provider, default);
		}

		public static DataConnection CreateDataConnection(string connectionString, ClickHouseProvider provider = ClickHouseProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, ClickHouseProvider provider = ClickHouseProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, ClickHouseProvider provider = ClickHouseProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), transaction);
		}

		/// <summary>
		/// Default bulk copy mode.
		/// Default value: <c>BulkCopyType.ProviderSpecific</c>.
		/// </summary>
		[Obsolete("Use ClickHouseOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => ClickHouseOptions.Default.BulkCopyType;
			set => ClickHouseOptions.Default = ClickHouseOptions.Default with { BulkCopyType = value };
		}
	}
}
