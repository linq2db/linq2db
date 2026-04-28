using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.DuckDB
{
	[UsedImplicitly]
	sealed class DuckDBFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			return DuckDBTools.GetDataProvider();
		}
	}
}
