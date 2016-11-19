using System;
using System.Collections.Generic;

namespace LinqToDB.Configuration
{
	public interface ILinqToDBSettings
	{
		IEnumerable<IDataProviderSettings>     DataProviders        { get; }
		string                                 DefaultConfiguration { get; }
		string                                 DefaultDataProvider  { get; }
		IEnumerable<IConnectionStringSettings> ConnectionStrings    { get; }
	}
}