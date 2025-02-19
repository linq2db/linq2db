using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlCe;

namespace LinqToDB.Internal.DataProvider.SqlCe
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
