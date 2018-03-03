using System;
using System.Collections.Generic;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Settings provider interface.
	/// </summary>
	public interface ILinqToDBSettings
	{
		/// <summary>
		/// Gets list of data provider settings.
		/// </summary>
		IEnumerable<IDataProviderSettings>     DataProviders        { get; }
		/// <summary>
		/// Gets name of default connection configuration.
		/// </summary>
		string                                 DefaultConfiguration { get; }
		/// <summary>
		/// Gets name of default data provider configuration.
		/// </summary>
		string                                 DefaultDataProvider  { get; }
		/// <summary>
		/// Gets list of connection configurations.
		/// </summary>
		IEnumerable<IConnectionStringSettings> ConnectionStrings    { get; }
	}
}