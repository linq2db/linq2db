using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Data;
	using Extensions;

	public static partial class ClickHouseTools
	{
		static readonly Lazy<IDataProvider> _octonicaDataProvider = DataConnection.CreateDataProvider<ClickHouseOctonicaDataProvider>();
		static readonly Lazy<IDataProvider> _clientDataProvider   = DataConnection.CreateDataProvider<ClickHouseClientDataProvider>();
		static readonly Lazy<IDataProvider> _mysqlDataProvider    = DataConnection.CreateDataProvider<ClickHouseMySqlDataProvider>();

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
			if ((options.ProviderName?.ContainsEx("Octonica") == true && options.ProviderName?.ContainsEx("ClickHouse") == true)
				|| (options.ConfigurationString?.ContainsEx("Octonica") == true && options.ConfigurationString?.ContainsEx("ClickHouse") == true))
				return _octonicaDataProvider.Value;

			if ((options.ProviderName?.ContainsEx("ClickHouse") == true && options.ProviderName?.ContainsEx("MySql") == true)
				|| (options.ConfigurationString?.ContainsEx("ClickHouse") == true && options.ConfigurationString?.ContainsEx("MySql") == true))
				return _mysqlDataProvider.Value;

			if (options.ProviderName?.ContainsEx("ClickHouse.Client") == true || options.ConfigurationString?.ContainsEx("ClickHouse.Client") == true)
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
