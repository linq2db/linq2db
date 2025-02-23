using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.MySql;

namespace LinqToDB.Internal.DataProvider.MySql
{
	[UsedImplicitly]
	sealed class MySqlFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var version = GetVersion(attributes) switch
			{
				"5.5" or "5.6" or "5.7"          => MySqlVersion.MySql57,
				"8.0" or "8.1" or "8.2" or "8.3" => MySqlVersion.MySql80,
				"10" or "11"                     => MySqlVersion.MariaDB10,
				_                                => MySqlVersion.AutoDetect,
			};

			var provider = GetAssemblyName(attributes) switch
			{
				MySqlProviderAdapter.MySqlConnectorAssemblyName => MySqlProvider.MySqlConnector,
				MySqlProviderAdapter.MySqlDataAssemblyName      => MySqlProvider.MySqlData,
				_                                               => MySqlProvider.AutoDetect
			};

			return MySqlTools.GetDataProvider(version, provider);
		}
	}
}
