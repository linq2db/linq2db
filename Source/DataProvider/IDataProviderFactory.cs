using System;
using System.Collections.Specialized;

namespace LinqToDB.DataProvider
{
	public interface IDataProviderFactory
	{
		IDataProvider GetDataProvider (NameValueCollection attributes);
	}
}
