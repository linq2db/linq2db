using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.ClickHouse;
using LinqToDB.Internal.DataProvider.MySql;

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
				ClickHouseProviderAdapter.DriverAssemblyName    => ClickHouseProvider.ClickHouseDriver,
				ClickHouseProviderAdapter.OctonicaAssemblyName  => ClickHouseProvider.Octonica,
				_                                               => ClickHouseProvider.AutoDetect
			};

			return ClickHouseTools.GetDataProvider(provider);
		}
	}
}
