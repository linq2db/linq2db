using System;
using System.Data;
using System.Diagnostics;

namespace LinqToDB.Configuration
{
	using Data;
	using DataProvider;
	using Mapping;

	internal enum ConnectionSetupType
	{
		DefaultConfiguration,
		ConnectionString,
		ConfigurationString,
		Connection,
		ConnectionFactory,
		Transaction
	}

	/// <summary>
	/// Used to build <see cref="LinqToDbConnectionOptions"/>
	/// which is used by <see cref="DataConnection"/>
	/// to determine connection settings.
	/// </summary>
	public class LinqToDbConnectionOptionsBuilder
	{
		public MappingSchema?                        MappingSchema       { get; private set; }
		public IDataProvider?                        DataProvider        { get; private set; }
		public IDbConnection?                        DbConnection        { get; private set; }
		public bool                                  DisposeConnection   { get; private set; }
		public string?                               ConfigurationString { get; private set; }
		public string?                               ProviderName        { get; private set; }
		public string?                               ConnectionString    { get; private set; }
		public Func<IDbConnection>?                  ConnectionFactory   { get; private set; }
		public IDbTransaction?                       DbTransaction       { get; private set; }
		public Action<TraceInfo>?                    OnTrace             { get; private set; }
		public TraceLevel?                           TraceLevel          { get; private set; }
		public Action<string?, string?, TraceLevel>? WriteTrace          { get; private set; }

		private void CheckAssignSetupType(ConnectionSetupType type)
		{
			if (SetupType != ConnectionSetupType.DefaultConfiguration)
				throw new LinqToDBException(
					$"LinqToDbConnectionOptionsBuilder already setup using {SetupType}, use Reset first to overwrite");
			SetupType = type;
		}

		internal ConnectionSetupType SetupType { get; set; }

		/// <summary>
		/// Build the immutable options used by the database.
		/// </summary>
		public LinqToDbConnectionOptions<TContext> Build<TContext>()
		{
			return new LinqToDbConnectionOptions<TContext>(this);
		}

		/// <summary>
		/// Build the immutable options used by the database.
		/// </summary>
		public LinqToDbConnectionOptions Build()
		{
			return new LinqToDbConnectionOptions(this);
		}

		/// <summary>
		/// Configure the database to use SqlServer default provider and connection string.
		/// </summary>
		/// <param name="connectionString">SqlServer connection string</param>
		/// <returns>The builder instance so calls can be chained</returns>
		public LinqToDbConnectionOptionsBuilder UseSqlServer(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SqlServer, connectionString);
		}

		/// <summary>
		/// Configure the database to use Oracle default provider and connection string.
		/// </summary>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseOracle(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.Oracle, connectionString);
		}

		/// <summary>
		/// Configure the database to use PostgreSQL default provider and connection string.
		/// </summary>
		/// <param name="connectionString">PostgreSQL connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UsePostgreSQL(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.PostgreSQL, connectionString);
		}

		/// <summary>
		/// Configure the database to use MySql default provider and connection string.
		/// </summary>
		/// <param name="connectionString">MySql connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseMySql(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.MySql, connectionString);
		}

		/// <summary>
		/// Configure the database to use SqlLite default provider and connection string.
		/// </summary>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseSQLite(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SQLite, connectionString);
		}

		/// <summary>
		/// Configure the database to use the specified provider and connection string.
		/// </summary>
		/// <param name="providerName">See <see cref="LinqToDB.ProviderName"/> for Default providers.</param>
		/// <param name="connectionString">Database specific connections string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseConnectionString(
			string providerName,
			string connectionString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionString);

			ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
			ProviderName     = providerName     ?? throw new ArgumentNullException(nameof(providerName));

			return this;
		}

		/// <summary>
		/// Configure the database to use the specified provider and connection string.
		/// </summary>
		/// <param name="dataProvider">Used by the connection to determine functionality when executing commands/queries.</param>
		/// <param name="connectionString">Database specific connections string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseConnectionString(
			IDataProvider dataProvider,
			string connectionString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionString);

			ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
			DataProvider     = dataProvider     ?? throw new ArgumentNullException(nameof(dataProvider));

			return this;
		}

		/// <summary>
		/// Configure the database to use the specified configuration string, Configurations can be added by calling <see cref="DataConnection.AddConfiguration"/>
		/// </summary>
		/// <param name="configurationString">Used used to lookup configuration, must be specified before the Database is created.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseConfigurationString(
			string configurationString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConfigurationString);
			ConfigurationString = configurationString ?? throw new ArgumentNullException(nameof(configurationString));
			return this;
		}

		/// <summary>
		/// Configure the database to use the specified provider and callback as an <see cref="IDbConnection"/> factory.
		/// </summary>
		/// <param name="dataProvider">Used by the connection to determine functionality when executing commands/queries.</param>
		/// <param name="connectionFactory">Factory function used to obtain the connection.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseConnectionFactory(
			IDataProvider dataProvider,
			Func<IDbConnection> connectionFactory)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionFactory);

			DataProvider      = dataProvider      ?? throw new ArgumentNullException(nameof(dataProvider));
			ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

			return this;
		}

		/// <summary>
		/// Configure the database to use the specified provider and an existing <see cref="IDbConnection"/>.
		/// </summary>
		/// <param name="dataProvider">Used by the connection to determine functionality when executing commands/queries.</param>
		/// <param name="connection">Existing connection, can be open or closed, will be opened automatically if closed.</param>
		/// <param name="disposeConnection">Indicates if the connection should be disposed when the context is disposed.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseConnection(
			IDataProvider dataProvider,
			IDbConnection connection,
			bool          disposeConnection = false)
		{
			CheckAssignSetupType(ConnectionSetupType.Connection);

			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			DbConnection = connection   ?? throw new ArgumentNullException(nameof(connection));

			DisposeConnection = disposeConnection;

			return this;
		}

		/// <summary>
		/// Configure the database to use the specified provider and an existing <see cref="IDbTransaction"/>.
		/// </summary>
		/// <param name="dataProvider">Used by the connection to determine functionality when executing commands/queries.</param>
		/// <param name="transaction">Existing transaction.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseTransaction(IDataProvider dataProvider,
			IDbTransaction transaction)
		{
			CheckAssignSetupType(ConnectionSetupType.Transaction);

			DataProvider  = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			DbTransaction = transaction  ?? throw new ArgumentNullException(nameof(transaction));

			return this;
		}

		/// <summary>
		/// Configure the database to use the specified mapping schema.
		/// </summary>
		/// <param name="mappingSchema">Used to define the mapping between sql and classes.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseMappingSchema(MappingSchema mappingSchema)
		{
			MappingSchema = mappingSchema ?? throw new ArgumentNullException(nameof(mappingSchema));
			return this;
		}

		/// <summary>
		/// Configure the database to use the specified provider, can override providers previously specified.
		/// </summary>
		/// <param name="dataProvider">Used by the connection to determine functionality when executing commands/queries.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseDataProvider(IDataProvider dataProvider)
		{
			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			return this;
		}

		/// <summary>
		/// Configure the database to use specified trace level.
		/// </summary>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder WithTraceLevel(TraceLevel traceLevel)
		{
			TraceLevel = traceLevel;
			return this;
		}

		/// <summary>
		/// Configure the database to use the specified callback for logging or tracing.
		/// </summary>
		/// <param name="onTrace">Callback, may not be called depending on the trace level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder WithTracing(Action<TraceInfo> onTrace)
		{
			OnTrace = onTrace;
			return this;
		}

		/// <summary>
		/// Configure the database to use the specified trace level and callback for logging or tracing.
		/// </summary>
		/// <param name="traceLevel">Trace level to use.</param>
		/// <param name="onTrace">Callback, may not be called depending on the trace level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder WithTracing(TraceLevel traceLevel, Action<TraceInfo> onTrace)
		{
			TraceLevel = traceLevel;
			OnTrace    = onTrace;
			return this;
		}


		/// <summary>
		/// Configure the database to use the specified a string trace callback.
		/// </summary>
		/// <param name="write">Callback, may not be called depending on the trace level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder WriteTraceWith(Action<string?, string?, TraceLevel> write)
		{
			WriteTrace = write;
			return this;
		}

		/// <summary>
		/// Reset the builder back to default configuration undoing all previous configured values
		/// </summary>
		/// <returns>The builder instance so calls can be chained.</returns>
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
			TraceLevel          = null;
			OnTrace             = null;
			WriteTrace          = null;
			SetupType           = ConnectionSetupType.DefaultConfiguration;

			return this;
		}
	}
}
