using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;

using LinqToDB.DataProvider.MySql;

namespace LinqToDB.DataProvider.ClickHouse
{
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
