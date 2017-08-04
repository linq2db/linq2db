using System;
using System.Data;
using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Database connection configuration
	/// </summary>
	public interface IConnectionStringSettings
	{
		/// <summary>
		/// Connection String <see cref="IDbConnection.ConnectionString"/>
		/// </summary>
		string ConnectionString { get; }
		/// <summary>
		/// Configuration name, used to identify configuration. Should be unique.
		/// <seealso cref="ILinqToDBSettings.DefaultConfiguration"/>
		/// <seealso cref="DataConnection.DefaultConfiguration"/>
		/// </summary>
		string Name             { get; }
		/// <summary>
		/// <see cref="LinqToDB.DataProvider"/> name
		/// <seealso cref="IDataProvider.Name"/>
		/// </summary>
		string ProviderName     { get; }
		/// <summary>
		/// Used to define if configuration is global for machine
		/// </summary>
		bool   IsGlobal         { get; }
	}
}
