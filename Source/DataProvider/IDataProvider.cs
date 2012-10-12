using System;
using System.Data;

namespace LinqToDB.DataProvider
{
	public interface IDataProvider
	{
		string Name         { get; }
		string ProviderName { get; }

		void          Configure       (string name, string value);
		IDbConnection CreateConnection(string       connectionString);
	}
}
