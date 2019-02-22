using System.Collections.Generic;
using LinqToDB.Configuration;

namespace LinqToDB.DataProvider
{

	public interface IDataProviderFactory
	{
		IDataProvider GetDataProvider (IEnumerable<NamedValue> attributes);
	}
}
