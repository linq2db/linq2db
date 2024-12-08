using System.Collections.Generic;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Configuration;

	using MySql;

	[UsedImplicitly]
	sealed class ClickHouseFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider = GetAssemblyName(attributes) switch
			{
				MySqlProviderAdapter.MySqlConnectorAssemblyName => ClickHouseProvider.MySqlConnector,
				ClickHouseProviderAdapter.ClientAssemblyName    => ClickHouseProvider.ClickHouseClient,
				ClickHouseProviderAdapter.OctonicaAssemblyName  => ClickHouseProvider.Octonica,
				_                                               => ClickHouseProvider.AutoDetect
			};

			return ClickHouseTools.GetDataProvider(provider);
		}
	}
}
