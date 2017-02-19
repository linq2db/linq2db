using System.Collections.Generic;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Access
{
	using Configuration;

	[UsedImplicitly]
	class AccessFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return new AccessDataProvider();
		}
	}
}
