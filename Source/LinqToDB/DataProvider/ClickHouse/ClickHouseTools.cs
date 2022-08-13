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

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if ((css.ProviderName?.Contains("Octonica") == true && css.ProviderName?.Contains("ClickHouse") == true)
				|| (css.Name.Contains("Octonica") == true && css.Name.Contains("ClickHouse") == true))
				return _octonicaDataProvider.Value;

			if ((css.ProviderName?.Contains("ClickHouse") == true && css.ProviderName?.Contains("MySql") == true)
				|| (css.Name.Contains("ClickHouse") && css.Name.Contains("MySql")))
				return _mysqlDataProvider.Value;

			if (css.ProviderName?.Contains("ClickHouse.Client") == true || css.Name.Contains("ClickHouse.Client"))
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
		public static BulkCopyType DefaultBulkCopyType { get; set; } = BulkCopyType.ProviderSpecific;
	}
}
