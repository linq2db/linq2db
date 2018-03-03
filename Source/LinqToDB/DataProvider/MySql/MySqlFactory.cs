using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.MySql
{
	using System.Collections.Generic;
	using Configuration;

	[UsedImplicitly]
	class MySqlFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return new MySqlDataProvider();
		}
	}
}
