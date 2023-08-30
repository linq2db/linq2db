using System.Collections.Generic;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Informix
{
	using Configuration;

	[UsedImplicitly]
	sealed class InformixFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return InformixTools.GetDataProvider();
		}
	}
}
