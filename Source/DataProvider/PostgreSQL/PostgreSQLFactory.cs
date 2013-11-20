using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.PostgreSQL
{
	[UsedImplicitly]
	class PostgreSQLFactory: IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return new PostgreSQLDataProvider();
		}
	}
}
