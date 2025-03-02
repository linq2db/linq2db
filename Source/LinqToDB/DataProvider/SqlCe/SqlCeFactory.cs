using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;

namespace LinqToDB.DataProvider.SqlCe
{
	[UsedImplicitly]
	sealed class SqlCeFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return SqlCeTools.GetDataProvider();
		}
	}
}
