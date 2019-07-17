using System;
using System.Data;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Configuration
{
	public class LinqToDbConnectionOptions<T> : LinqToDbConnectionOptions
	{
		public override bool IsValidConfigForConnectionType(DataConnection connection)
		{
			return connection is T;
		}
	}

	public class LinqToDbConnectionOptions
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
					$"LinqToDbConnectionOptions already setup using {SetupType}, use Reset first to overwrite");
			SetupType = type;
		}

		internal ConnectionSetupType SetupType { get; set; }

		internal enum ConnectionSetupType
		{
			DefaultConfiguration,
			ConnectionString,
			ConfigurationString,
			Connection,
			ConnectionFactory,
			Transaction
		}

		public LinqToDbConnectionOptions UseSqlServer([JetBrains.Annotations.NotNull] string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SqlServer, connectionString);
		}
		public LinqToDbConnectionOptions UseOracle([JetBrains.Annotations.NotNull] string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.Oracle, connectionString);
		}
		public LinqToDbConnectionOptions UsePostgreSQL([JetBrains.Annotations.NotNull] string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.PostgreSQL, connectionString);
		}
		public LinqToDbConnectionOptions UseMySql([JetBrains.Annotations.NotNull] string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.MySql, connectionString);
		}
		public LinqToDbConnectionOptions UseSQLite([JetBrains.Annotations.NotNull] string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SQLite, connectionString);
		}

		/// <summary>
		/// Configure the database to use this provider and connection string
		/// </summary>
		/// <param name="providerName">See <see cref="ProviderName"/> for Default providers</param>
		/// <param name="connectionString">Database specific connections string</param>
		/// <returns></returns>
		public LinqToDbConnectionOptions UseConnectionString(
			[JetBrains.Annotations.NotNull] string providerName,
			[JetBrains.Annotations.NotNull] string connectionString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionString);

			ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
			ProviderName     = providerName     ?? throw new ArgumentNullException(nameof(providerName));

			return this;
		}

		public LinqToDbConnectionOptions UseConnectionString(
			[JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] string connectionString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionString);

			ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
			DataProvider     = dataProvider     ?? throw new ArgumentNullException(nameof(dataProvider));

			return this;
		}

		public LinqToDbConnectionOptions UseConfigurationString(
			[JetBrains.Annotations.NotNull] string configurationString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConfigurationString);
			ConfigurationString = configurationString ?? throw new ArgumentNullException(nameof(configurationString));
			return this;
		}

		public LinqToDbConnectionOptions UseConnectionFactory(
			[JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] Func<IDbConnection> connectionFactory)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionFactory);

			DataProvider      = dataProvider      ?? throw new ArgumentNullException(nameof(dataProvider));
			ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

			return this;
		}

		public LinqToDbConnectionOptions UseConnection([JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] IDbConnection connection,
			bool disposeConnection = false)
		{
			CheckAssignSetupType(ConnectionSetupType.Connection);

			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			DbConnection = connection   ?? throw new ArgumentNullException(nameof(connection));

			if (!Common.Configuration.AvoidSpecificDataProviderAPI
			    && !DataProvider.IsCompatibleConnection(DbConnection))
				throw new LinqToDBException(
					$"DataProvider '{DataProvider}' and connection '{DbConnection}' are not compatible.");

			DisposeConnection = disposeConnection;

			return this;
		}

		public LinqToDbConnectionOptions UseTransaction([JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] IDbTransaction transaction)
		{
			CheckAssignSetupType(ConnectionSetupType.Transaction);

			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			DbTransaction = transaction ?? throw new ArgumentNullException(nameof(transaction));

			return this;
		}

		public LinqToDbConnectionOptions UseMappingSchema([JetBrains.Annotations.NotNull] MappingSchema mappingSchema)
		{
			MappingSchema = mappingSchema ?? throw new ArgumentNullException(nameof(mappingSchema));
			return this;
		}

		public LinqToDbConnectionOptions UseDataProvider([JetBrains.Annotations.NotNull] IDataProvider dataProvider)
		{
			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			return this;
		}

		public LinqToDbConnectionOptions Reset()
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
