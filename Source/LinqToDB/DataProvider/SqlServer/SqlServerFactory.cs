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

			if (assemblyName == "Microsoft.Data.SqlClient")
			{
				provider = SqlServerProvider.MicrosoftDataSqlClient;
			}

			SqlServerTools.Provider = provider;

			if (version != null)
			{
				switch (version.Value)
				{
					case "2000" : return new SqlServerDataProvider(ProviderName.SqlServer2000, SqlServerVersion.v2000, provider);
					case "2005" : return new SqlServerDataProvider(ProviderName.SqlServer2005, SqlServerVersion.v2005, provider);
					case "2012" : return new SqlServerDataProvider(ProviderName.SqlServer2012, SqlServerVersion.v2012, provider);
					case "2014" : return new SqlServerDataProvider(ProviderName.SqlServer2014, SqlServerVersion.v2012, provider);
					case "2017" : return new SqlServerDataProvider(ProviderName.SqlServer2017, SqlServerVersion.v2017, provider);
				}
			}

			return new SqlServerDataProvider(ProviderName.SqlServer2008, SqlServerVersion.v2008, provider);
		}
	}
}
