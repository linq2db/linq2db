using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SqlServer
{
	using Configuration;

	[UsedImplicitly]
	class SqlServerFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider     = SqlServerProvider.SystemDataSqlClient;
			var version      = attributes.FirstOrDefault(_ => _.Name == "version")?.Value;
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			if (assemblyName == SqlServerProviderAdapter.MicrosoftAssemblyName)
			{
				provider = SqlServerProvider.MicrosoftDataSqlClient;
			}

			return version switch
			{
				"2005" => SqlServerTools.GetDataProvider(SqlServerVersion.v2005, provider, null),
				"2012" => SqlServerTools.GetDataProvider(SqlServerVersion.v2012, provider, null),
				"2014" => SqlServerTools.GetDataProvider(SqlServerVersion.v2014, provider, null),
				"2016" => SqlServerTools.GetDataProvider(SqlServerVersion.v2016, provider, null),
				"2017" => SqlServerTools.GetDataProvider(SqlServerVersion.v2017, provider, null),
				"2019" => SqlServerTools.GetDataProvider(SqlServerVersion.v2019, provider, null),
				"2022" => SqlServerTools.GetDataProvider(SqlServerVersion.v2022, provider, null),
				_      => SqlServerTools.GetDataProvider(SqlServerVersion.v2008, provider, null),
			};
		}
	}
}
