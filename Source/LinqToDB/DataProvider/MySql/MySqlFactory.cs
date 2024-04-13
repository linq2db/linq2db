using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.MySql
{
	using Configuration;

	[UsedImplicitly]
	sealed class MySqlFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			var provider = assemblyName switch
			{
				MySqlProviderAdapter.MySqlConnectorAssemblyName => MySqlProvider.MySqlConnector,
				MySqlProviderAdapter.MySqlDataAssemblyName      => MySqlProvider.MySqlData,
				_                                               => MySqlProvider.AutoDetect
			};

			return MySqlTools.GetDataProvider(provider);
		}
	}
}
