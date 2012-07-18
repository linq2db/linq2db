using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	public interface IDataProvider
	{
		string Name         { get; }
		string ProviderName { get; }

		void          Configure       (NameValueCollection attributes);
		IDbConnection CreateConnection(string              connectionString);
	}
}
