﻿using JetBrains.Annotations;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SQLite
{
	using Configuration;

	[UsedImplicitly]
	sealed class SQLiteFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return SQLiteTools.GetDataProvider();
		}
	}
}
