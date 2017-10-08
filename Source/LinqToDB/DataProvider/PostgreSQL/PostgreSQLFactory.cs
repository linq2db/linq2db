using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using System.Collections.Generic;
	using Configuration;

	[UsedImplicitly]
	class PostgreSQLFactory: IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return new PostgreSQLDataProvider();
		}
	}
}
