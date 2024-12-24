using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Internals.DataProviders.MySql;

namespace LinqToDB.Internals.DataProviders.ClickHouse
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
