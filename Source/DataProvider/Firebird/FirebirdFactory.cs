using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Firebird
{
	[UsedImplicitly]
	class FirebirdFactory: IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			return new FirebirdDataProvider();
		}
	}
}
