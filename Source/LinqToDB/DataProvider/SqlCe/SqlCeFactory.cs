using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SqlCe;

using Configuration;

[UsedImplicitly]
class SqlCeFactory : IDataProviderFactory
{
	IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
	{
		return SqlCeTools.GetDataProvider();
	}
}
