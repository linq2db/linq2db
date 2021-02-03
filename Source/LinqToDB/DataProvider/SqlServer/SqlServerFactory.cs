using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SqlServer
{
	using System.Collections.Generic;
	using System.Linq;

	using Configuration;

	[UsedImplicitly]
	class SqlServerFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider     = SqlServerProvider.SystemDataSqlClient;
			var version      = attributes.FirstOrDefault(_ => _.Name == "version");
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			if (assemblyName == SqlServerProviderAdapter.MicrosoftAssemblyName)
			{
				provider = SqlServerProvider.MicrosoftDataSqlClient;
			}

			return version?.Value switch
			{
				"2000" => SqlServerTools.GetDataProvider(SqlServerVersion.v2000, provider),
				"2005" => SqlServerTools.GetDataProvider(SqlServerVersion.v2005, provider),
				"2012" => SqlServerTools.GetDataProvider(SqlServerVersion.v2012, provider),
				"2014" => SqlServerTools.GetDataProvider(SqlServerVersion.v2012, provider),
				"2017" => SqlServerTools.GetDataProvider(SqlServerVersion.v2017, provider),
				"2019" => SqlServerTools.GetDataProvider(SqlServerVersion.v2017, provider),
				_      => SqlServerTools.GetDataProvider(SqlServerVersion.v2008, provider),
			};
		}
	}
}
