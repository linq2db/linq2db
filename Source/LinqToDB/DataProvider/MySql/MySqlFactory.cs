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
			string? assemblyName = null;
			string? versionName  = null;

			foreach (var attr in attributes)
			{
				if (attr.Name == "version" && versionName == null)
					versionName = attr.Value;
				if (attr.Name == "assemblyName" && assemblyName == null)
					assemblyName = attr.Value;
			}

			var version = versionName switch
			{
				"5.5" or "5.6" or "5.7"          => MySqlVersion.MySql57,
				"8.0" or "8.1" or "8.2" or "8.3" => MySqlVersion.MySql80,
				"10" or "11"                     => MySqlVersion.MariaDB10,
				_                                => MySqlVersion.AutoDetect,
			};

			var provider = assemblyName switch
			{
				MySqlProviderAdapter.MySqlConnectorAssemblyName => MySqlProvider.MySqlConnector,
				MySqlProviderAdapter.MySqlDataAssemblyName      => MySqlProvider.MySqlData,
				_                                               => MySqlProvider.AutoDetect
			};

			return MySqlTools.GetDataProvider(version, provider);
		}
	}
}
