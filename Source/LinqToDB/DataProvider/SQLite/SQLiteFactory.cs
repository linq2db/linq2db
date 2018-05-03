using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SQLite
{
	using System.Collections.Generic;
	using Configuration;

	[UsedImplicitly]
	class SQLiteFactory: IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return new SQLiteDataProvider();
		}
	}
}
