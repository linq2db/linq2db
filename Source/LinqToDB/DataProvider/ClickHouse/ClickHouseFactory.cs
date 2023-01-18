using JetBrains.Annotations;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Configuration;
	using MySql;

	[UsedImplicitly]
	sealed class ClickHouseFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider     = ClickHouseProvider.Octonica;
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			provider = assemblyName switch
			{
				MySqlProviderAdapter.MySqlConnectorAssemblyName => ClickHouseProvider.MySqlConnector,
				ClickHouseProviderAdapter.ClientAssemblyName    => ClickHouseProvider.ClickHouseClient,
				_                                               => ClickHouseProvider.Octonica
			};

			return ClickHouseTools.GetDataProvider(provider);
		}
	}
}
