using JetBrains.Annotations;
using System.Collections.Generic;

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
