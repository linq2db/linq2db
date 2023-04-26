﻿using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SqlServer
{
	using Configuration;

	[UsedImplicitly]
	sealed class SqlServerFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var versionName  = attributes.FirstOrDefault(_ => _.Name == "version")?.Value;
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			var provider = assemblyName switch
			{
				SqlServerProviderAdapter.MicrosoftAssemblyName => SqlServerProvider.MicrosoftDataSqlClient,
				SqlServerProviderAdapter.SystemAssemblyName    => SqlServerProvider.SystemDataSqlClient,
				_                                              => SqlServerProvider.AutoDetect
			};

			var version = versionName switch
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
