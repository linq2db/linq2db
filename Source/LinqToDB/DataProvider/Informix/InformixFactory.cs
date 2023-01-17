using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Informix
{
	using System.Collections.Generic;
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
