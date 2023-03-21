using System.Collections.Generic;
using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Firebird
{
	using Configuration;

	[UsedImplicitly]
	sealed class FirebirdFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return FirebirdTools.GetDataProvider();
		}
	}
}
