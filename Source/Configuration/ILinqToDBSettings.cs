using System;
using System.Collections.Generic;
using LinqToDB.Data;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Used to provide configuration settings to <seealso cref="IDataContext"/>
	/// Default configuration is set with <see cref="DataConnection.DefaultConfiguration"/>
	/// </summary>
	public interface ILinqToDBSettings
	{
		/// <summary>
		/// Lists <see cref="IDataProviderSettings"/>
		/// </summary>
		IEnumerable<IDataProviderSettings>     DataProviders        { get; }
		/// <summary>
		/// Name of the default configuration.
		/// <seealso cref="DataConnection.DefaultConfiguration"/>
		/// </summary>
		string                                 DefaultConfiguration { get; }
		/// <summary>
		/// Name of the default <see cref="LinqToDB.DataProvider"/>
		/// <seealso cref="DataConnection.DefaultConfiguration"/>
		/// </summary>
		string                                 DefaultDataProvider  { get; }
		/// <summary>
		/// Lists <see cref="IConnectionStringSettings"/>
		/// </summary>
		IEnumerable<IConnectionStringSettings> ConnectionStrings    { get; }
	}
}