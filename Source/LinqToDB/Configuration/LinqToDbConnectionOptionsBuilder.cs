using System;
using System.Data;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Configuration
{
	internal enum ConnectionSetupType
	{
		DefaultConfiguration,
		ConnectionString,
		ConfigurationString,
		Connection,
		ConnectionFactory,
		Transaction
	}
	
	public class LinqToDbConnectionOptionsBuilder
	{
		public MappingSchema       MappingSchema       { get; private set; }
		public IDataProvider       DataProvider        { get; private set; }
		public IDbConnection       DbConnection        { get; private set; }
		public bool                DisposeConnection   { get; private set; }
		public string              ConfigurationString { get; private set; }
		public string              ProviderName        { get; private set; }
		public string              ConnectionString    { get; private set; }
		public Func<IDbConnection> ConnectionFactory   { get; private set; }
		public IDbTransaction      DbTransaction       { get; private set; }

		private void CheckAssignSetupType(ConnectionSetupType type)
		{
			if (SetupType != ConnectionSetupType.DefaultConfiguration)
				throw new LinqToDBException(
					$"LinqToDbConnectionOptionsBuilder already setup using {SetupType}, use Reset first to overwrite");
			SetupType = type;
		}

		internal ConnectionSetupType SetupType { get; set; }

		public LinqToDbConnectionOptions<TContext> Build<TContext>()
		{
			return new LinqToDbConnectionOptions<TContext>(this);
		}
		public LinqToDbConnectionOptions Build()
		{
			return new LinqToDbConnectionOptions(this);
		}

		public LinqToDbConnectionOptionsBuilder UseSqlServer(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SqlServer, connectionString);
		}
		public LinqToDbConnectionOptionsBuilder UseOracle(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.Oracle, connectionString);
		}
		public LinqToDbConnectionOptionsBuilder UsePostgreSQL(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.PostgreSQL, connectionString);
		}
		public LinqToDbConnectionOptionsBuilder UseMySql(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.MySql, connectionString);
		}
		public LinqToDbConnectionOptionsBuilder UseSQLite(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SQLite, connectionString);
		}

		/// <summary>
		/// Configure the database to use this provider and connection string
		/// </summary>
		/// <param name="providerName">See <see cref="ProviderName"/> for Default providers</param>
		/// <param name="connectionString">Database specific connections string</param>
		/// <returns></returns>
		public LinqToDbConnectionOptionsBuilder UseConnectionString(
			string providerName,
			string connectionString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionString);

			ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
			ProviderName     = providerName     ?? throw new ArgumentNullException(nameof(providerName));

			return this;
		}

		public LinqToDbConnectionOptionsBuilder UseConnectionString(
			IDataProvider dataProvider,
			string connectionString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionString);

			ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
			DataProvider     = dataProvider     ?? throw new ArgumentNullException(nameof(dataProvider));

			return this;
		}

		public LinqToDbConnectionOptionsBuilder UseConfigurationString(
			string configurationString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConfigurationString);
			ConfigurationString = configurationString ?? throw new ArgumentNullException(nameof(configurationString));
			return this;
		}

		public LinqToDbConnectionOptionsBuilder UseConnectionFactory(
			IDataProvider dataProvider,
			Func<IDbConnection> connectionFactory)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionFactory);

			DataProvider      = dataProvider      ?? throw new ArgumentNullException(nameof(dataProvider));
			ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

			return this;
		}

		public LinqToDbConnectionOptionsBuilder UseConnection(IDataProvider dataProvider,
			IDbConnection connection,
			bool disposeConnection = false)
		{
			CheckAssignSetupType(ConnectionSetupType.Connection);

			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			DbConnection = connection   ?? throw new ArgumentNullException(nameof(connection));

			DisposeConnection = disposeConnection;

			return this;
		}

		public LinqToDbConnectionOptionsBuilder UseTransaction(IDataProvider dataProvider,
			IDbTransaction transaction)
		{
			CheckAssignSetupType(ConnectionSetupType.Transaction);

			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			DbTransaction = transaction ?? throw new ArgumentNullException(nameof(transaction));

			return this;
		}

		public LinqToDbConnectionOptionsBuilder UseMappingSchema(MappingSchema mappingSchema)
		{
			MappingSchema = mappingSchema ?? throw new ArgumentNullException(nameof(mappingSchema));
			return this;
		}

		public LinqToDbConnectionOptionsBuilder UseDataProvider(IDataProvider dataProvider)
		{
			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			return this;
		}

		public LinqToDbConnectionOptionsBuilder Reset()
		{
			MappingSchema       = null;
			DataProvider        = null;
			ConfigurationString = null;
			ConnectionString    = null;
			DbConnection        = null;
			ProviderName        = null;
			DbTransaction       = null;
			ConnectionFactory   = null;
			SetupType           = ConnectionSetupType.DefaultConfiguration;

			return this;
		}

		public virtual bool IsValidConfigForConnectionType(DataConnection connection)
		{
			return true;
		}
	}
}
