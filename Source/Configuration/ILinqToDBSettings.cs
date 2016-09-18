using System;

namespace LinqToDB.Configuration
{
	using System.Collections.Generic;

	public interface ILinqToDBSettings
	{
		IEnumerable<IDataProviderSettings> DataProviders { get; }
		string DefaultConfiguration { get; }
		string DefaultDataProvider { get; }
		IEnumerable<IConnectionStringSettings> ConnectionStrings { get; }
	}
}