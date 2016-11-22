using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Firebird
{
	using System.Collections.Generic;
	using Configuration;

	[UsedImplicitly]
	class FirebirdFactory: IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return new FirebirdDataProvider();
		}
	}
}
