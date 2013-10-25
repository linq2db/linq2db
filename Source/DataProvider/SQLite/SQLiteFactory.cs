using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SQLite
{
	[UsedImplicitly]
	class SQLiteFactory: IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return new SQLiteDataProvider();
		}
	}
}
