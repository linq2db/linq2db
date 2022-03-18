using JetBrains.Annotations;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.NitrosBase
{
	using Configuration;

	[UsedImplicitly]
	class NitrosFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return NitrosBaseTools.GetDataProvider();
		}
	}
}
