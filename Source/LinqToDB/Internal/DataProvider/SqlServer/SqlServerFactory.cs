using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	[UsedImplicitly]
	sealed class SqlServerFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider = GetAssemblyName(attributes) switch
			{
				SqlServerProviderAdapter.MicrosoftAssemblyName => SqlServerProvider.MicrosoftDataSqlClient,
				SqlServerProviderAdapter.SystemAssemblyName    => SqlServerProvider.SystemDataSqlClient,
				_                                              => SqlServerProvider.AutoDetect
			};

			var version = GetVersion(attributes) switch
			{
				"2005" => SqlServerVersion.v2005,
				"2008" => SqlServerVersion.v2008,
				"2012" => SqlServerVersion.v2012,
				"2014" => SqlServerVersion.v2014,
				"2016" => SqlServerVersion.v2016,
				"2017" => SqlServerVersion.v2017,
				"2019" => SqlServerVersion.v2019,
				"2022" => SqlServerVersion.v2022,
				_      => SqlServerVersion.AutoDetect,
			};

			return SqlServerTools.GetDataProvider(version, provider);
		}
	}
}
