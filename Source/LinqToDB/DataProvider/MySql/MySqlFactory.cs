﻿using JetBrains.Annotations;

namespace LinqToDB.DataProvider.MySql
{
	using System.Collections.Generic;
	using Configuration;

	[UsedImplicitly]
	sealed class MySqlFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return MySqlTools.GetDataProvider();
		}
	}
}
