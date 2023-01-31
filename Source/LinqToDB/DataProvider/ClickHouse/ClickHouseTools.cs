using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Configuration;
	using Data;

	public static class ClickHouseTools
	{
		static readonly Lazy<IDataProvider> _octonicaDataProvider = DataConnection.CreateDataProvider<ClickHouseOctonicaDataProvider>();
		static readonly Lazy<IDataProvider> _clientDataProvider   = DataConnection.CreateDataProvider<ClickHouseClientDataProvider>();
		static readonly Lazy<IDataProvider> _mysqlDataProvider    = DataConnection.CreateDataProvider<ClickHouseMySqlDataProvider>();

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
			if ((options.ProviderName?.Contains("Octonica") == true && options.ProviderName?.Contains("ClickHouse") == true)
				|| (options.ConfigurationString?.Contains("Octonica") == true && options.ConfigurationString?.Contains("ClickHouse") == true))
				return _octonicaDataProvider.Value;

			if ((options.ProviderName?.Contains("ClickHouse") == true && options.ProviderName?.Contains("MySql") == true)
				|| (options.ConfigurationString?.Contains("ClickHouse") == true && options.ConfigurationString?.Contains("MySql") == true))
				return _mysqlDataProvider.Value;

			if (options.ProviderName?.Contains("ClickHouse.Client") == true || options.ConfigurationString?.Contains("ClickHouse.Client") == true)
				return _clientDataProvider.Value;

			return null;
		}

		public static IDataProvider GetDataProvider(ClickHouseProvider provider = ClickHouseProvider.Octonica)
		{
			return provider switch
			{
				ClickHouseProvider.ClickHouseClient => _clientDataProvider.Value,
				ClickHouseProvider.MySqlConnector   => _mysqlDataProvider.Value,
				_                                   => _octonicaDataProvider.Value
			};
		}

		public static DataConnection CreateDataConnection(string connectionString, ClickHouseProvider provider = ClickHouseProvider.Octonica)
		{
			return new DataConnection(GetDataProvider(provider), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, ClickHouseProvider provider = ClickHouseProvider.Octonica)
		{
			return new DataConnection(GetDataProvider(provider), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, ClickHouseProvider provider = ClickHouseProvider.Octonica)
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
