using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Provides explicitly-defined <see cref="ILinqToDBSettings"/> implementation.
	/// </summary>
	public class LinqToDBSettings : ILinqToDBSettings
	{
		private readonly IConnectionStringSettings _connectionStringSettings;

		public LinqToDBSettings(
			string connectionName,
			string providerName,
			string connectionString)
		{
			_connectionStringSettings = new ConnectionStringSettings(connectionName, connectionString, providerName);
		}

		public IEnumerable<IDataProviderSettings>     DataProviders        => Enumerable.Empty<IDataProviderSettings>();
		public string                                 DefaultConfiguration => _connectionStringSettings.Name;
		public string                                 DefaultDataProvider  => ProviderName.SqlServer;
		public IEnumerable<IConnectionStringSettings> ConnectionStrings    => new [] { _connectionStringSettings };
	}

}
