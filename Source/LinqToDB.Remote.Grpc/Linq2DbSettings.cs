using System.Collections.Generic;
using System.Linq;
using LinqToDB.Configuration;

namespace LinqToDB.Remote.Grpc
{
	public class Linq2DbSettings : ILinqToDBSettings
	{
		private readonly IConnectionStringSettings _connectionStringSettings;

		public Linq2DbSettings(
			string databaseName,
			string providerName,
			string connectionString
			)
		{
			_connectionStringSettings = new ConnectionStringSettings
			{
				Name = databaseName,
				ProviderName = providerName,
				ConnectionString = connectionString
			};
		}

		public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

		public string DefaultConfiguration => _connectionStringSettings.Name;

		public string DefaultDataProvider => ProviderName.SqlServer;

		public IEnumerable<IConnectionStringSettings> ConnectionStrings
		{
			get
			{
				yield return _connectionStringSettings;
			}
		}
	}

}
