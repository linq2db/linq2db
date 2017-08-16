using System;
using System.Data;
using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Connection string configuration provider.
	/// </summary>
	public interface IConnectionStringSettings
	{
		/// <summary>
		/// Gets connection string.
		/// </summary>
		string ConnectionString { get; }
		/// <summary>
		/// Gets connection configuration name.
		/// </summary>
		string Name             { get; }
		/// <summary>
		/// Gets data provider configuration name.
		/// </summary>
		string ProviderName     { get; }
		/// <summary>
		/// Is this connection configuration defined on global level (machine.config) or on application level.
		/// </summary>
		bool   IsGlobal         { get; }
	}
}
