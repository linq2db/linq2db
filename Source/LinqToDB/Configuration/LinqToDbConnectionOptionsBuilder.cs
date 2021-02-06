using System;
using System.Data;
using System.Diagnostics;

namespace LinqToDB.Configuration
{
	using Data;
	using DataProvider;
	using LinqToDB.DataProvider.Access;
	using LinqToDB.DataProvider.DB2;
	using LinqToDB.DataProvider.Informix;
	using LinqToDB.DataProvider.MySql;
	using LinqToDB.DataProvider.Oracle;
	using LinqToDB.DataProvider.PostgreSQL;
	using LinqToDB.DataProvider.SapHana;
	using LinqToDB.DataProvider.SQLite;
	using LinqToDB.DataProvider.SqlServer;
	using LinqToDB.DataProvider.Sybase;
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

		#region UseSqlServer
		/// <summary>
		/// Configure connection to use SQL Server default provider, dialect and connection string.
		/// </summary>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider configured using <see cref="SqlServerTools.Provider"/> option and set to <see cref="SqlServerProvider.SystemDataSqlClient"/> by default.
		/// </para>
		/// <para>
		/// SQL Server dialect will be choosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="SqlServerTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, Linq To DB will query server for version</item>
		/// <item>otherwise <see cref="SqlServerVersion.v2008"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSqlServer(string, SqlServerProvider, SqlServerVersion)"/> overload.
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UseSqlServer(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SqlServer, connectionString);
		}

		/// <summary>
		/// Configure connection to use specific SQL Server provider, dialect and connection string.
		/// </summary>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <param name="provider">SQL Server provider to use.</param>
		/// <param name="dialect">SQL Server dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseSqlServer(string connectionString, SqlServerProvider provider, SqlServerVersion dialect)
		{
			return UseConnectionString(SqlServerTools.GetDataProvider(dialect, provider), connectionString);
		}
		#endregion

		#region UseOracle
		/// <summary>
		/// Configure connection to use Oracle default provider, dialect and connection string.
		/// </summary>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default Linq To DB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be choosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, Linq To DB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseOracle(string, OracleVersion)"/> overload.
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UseOracle(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.Oracle, connectionString);
		}

		/// <summary>
		/// Configure connection to use Oracle default provider, specific dialect and connection string.
		/// </summary>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default Linq To DB tries to load managed version of Oracle provider.
		/// </para>
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UseOracle(string connectionString, OracleVersion dialect)
		{
			return UseConnectionString(OracleTools.GetDataProvider(LinqToDB.ProviderName.Oracle, null, dialect), connectionString);
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific Oracle provider, dialect and connection string.
		/// </summary>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <param name="useNativeProvider">if <c>true</c>, <c>Oracle.DataAccess</c> provider will be used; othwerwise managed <c>Oracle.ManagedDataAccess</c>.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseOracle(string connectionString, OracleVersion dialect, bool useNativeProvider)
		{
			return UseConnectionString(
				OracleTools.GetDataProvider(
					useNativeProvider ? LinqToDB.ProviderName.OracleNative : LinqToDB.ProviderName.OracleManaged,
					null,
					dialect),
				connectionString);
		}
#endif
		#endregion

		#region UsePostgreSQL
		/// <summary>
		/// Configure connection to use PostgreSQL Npgsql provider, default dialect and connection string.
		/// </summary>
		/// <param name="connectionString">PostgreSQL connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// PostgreSQL dialect will be choosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="PostgreSQLTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, Linq To DB will query server for version</item>
		/// <item>otherwise <see cref="PostgreSQLVersion.v92"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UsePostgreSQL(string, PostgreSQLVersion)"/> overload.
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UsePostgreSQL(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.PostgreSQL, connectionString);
		}

		/// <summary>
		/// Configure connection to use PostgreSQL Npgsql provider, specific dialect and connection string.
		/// </summary>
		/// <param name="connectionString">PostgreSQL connection string.</param>
		/// <param name="dialect">POstgreSQL dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UsePostgreSQL(string connectionString, PostgreSQLVersion dialect)
		{
			return UseConnectionString(PostgreSQLTools.GetDataProvider(dialect), connectionString);
		}
		#endregion

		#region UseMySql
		/// <summary>
		/// Configure connection to use MySql default provider and connection string.
		/// </summary>
		/// <param name="connectionString">MySql connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be choosen by probing current folder for provider assembly and if it is not found, default to <c>MySql.Data</c> provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseMySqlData(string)"/> and <see cref="UseMySqlConnector(string)"/> methods.
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UseMySql(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.MySql, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>MySql.Data</c> MySql provider and connection string.
		/// </summary>
		/// <param name="connectionString">MySql connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseMySqlData(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.MySqlOfficial, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>MySqlConnector</c> MySql provider and connection string.
		/// </summary>
		/// <param name="connectionString">MySql connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseMySqlConnector(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.MySqlConnector, connectionString);
		}
		#endregion

		#region UseSQLite
		/// <summary>
		/// Configure connection to use SQLite default provider and connection string.
		/// </summary>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be choosen by probing current folder for provider assembly and if it is not found, default to <c>System.Data.Sqlite</c> provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSQLiteOfficial(string)"/> and <see cref="UseSQLiteMicrosoft(string)"/> methods.
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UseSQLite(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SQLite, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>System.Data.Sqlite</c> SQLite provider and connection string.
		/// </summary>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseSQLiteOfficial(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SQLiteClassic, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>Microsoft.Data.Sqlite</c> SQLite provider and connection string.
		/// </summary>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseSQLiteMicrosoft(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SQLiteMS, connectionString);
		}
		#endregion

		#region UseAccess
		/// <summary>
		/// Configure connection to use Access default provider and connection string.
		/// </summary>
		/// <param name="connectionString">Access connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider determined by inspecting connection string for OleDb or ODBC-specific markers and otherwise defaults to OleDb provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseAccessOleDb(string)"/> and <see cref="UseAccessODBC(string)"/> methods.
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UseAccess(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.Access, connectionString);
		}

		/// <summary>
		/// Configure connection to use Access OleDb provider and connection string.
		/// </summary>
		/// <param name="connectionString">Access connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseAccessOleDb(string connectionString)
		{
			return UseConnectionString(AccessTools.GetDataProvider(null), connectionString);
		}

		/// <summary>
		/// Configure connection to use Access ODBC provider and connection string.
		/// </summary>
		/// <param name="connectionString">Access connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseAccessODBC(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.AccessOdbc, connectionString);
		}
		#endregion

		#region UseDB2
		/// <summary>
		/// Configure connection to use DB2 default provider and connection string.
		/// </summary>
		/// <param name="connectionString">DB2 connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// DB2 provider will be choosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="DB2Tools.AutoDetectProvider"/> (default: <c>true</c>) enabled, Linq To DB will query server for version</item>
		/// <item>otherwise <c>DB2 LUW</c> provider will be choosen.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseDB2(string, DB2Version)"/> overload.
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UseDB2(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.DB2, connectionString);
		}

		/// <summary>
		/// Configure connection to use specific DB2 provider and connection string.
		/// </summary>
		/// <param name="connectionString">DB2 connection string.</param>
		/// <param name="version">DB2 server version.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseDB2(string connectionString, DB2Version version)
		{
			return UseConnectionString(DB2Tools.GetDataProvider(version), connectionString);
		}
		#endregion

		#region UseFirebird
		/// <summary>
		/// Configure connection to use Firebird provider and connection string.
		/// </summary>
		/// <param name="connectionString">Firebird connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseFirebird(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.Firebird, connectionString);
		}
		#endregion

		#region UseInformix
		/// <summary>
		/// Configure connection to use Informix default provider and connection string.
		/// </summary>
		/// <param name="connectionString">Informix connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be choosen by probing current folder for provider assembly and if it is not found, default to <c>IBM.Data.DB2</c> provider.
		/// This is not applicable to .NET Core applications as they always use <c>IBM.Data.DB2</c> provider.
		/// </para>
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UseInformix(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.Informix, connectionString);
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific Informix provider and connection string.
		/// </summary>
		/// <param name="connectionString">Informix connection string.</param>
		/// <param name="useDB2Provider">if <c>true</c>, <c>IBM.Data.DB2</c> provider will be used; othwerwise <c>IBM.Data.Informix</c>.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseInformix(string connectionString, bool useDB2Provider)
		{
			return UseConnectionString(
				InformixTools.GetDataProvider(useDB2Provider ? LinqToDB.ProviderName.InformixDB2 : LinqToDB.ProviderName.Informix),
				connectionString);
		}
#endif
		#endregion

		#region UseSapHana
		/// <summary>
		/// Configure connection to use SAP HANA default provider and connection string.
		/// </summary>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be <c>Sap.Data.Hana</c> native provider for .NET Framework and .NET Core applications and ODBC provider for .NET STANDARD builds.
		/// </para>
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UseSapHana(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SapHana, connectionString);
		}

#if NETFRAMEWORK || NETCOREAPP
		/// <summary>
		/// Configure connection to use native SAP HANA provider and connection string.
		/// </summary>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseSapHanaNative(string connectionString)
		{
			return UseConnectionString(
				SapHanaTools.GetDataProvider(LinqToDB.ProviderName.SapHanaNative),
				connectionString);
		}
#endif

		/// <summary>
		/// Configure connection to use SAP HANA ODBC provider and connection string.
		/// </summary>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseSapHanaODBC(string connectionString)
		{
			return UseConnectionString(
				SapHanaTools.GetDataProvider(LinqToDB.ProviderName.SapHanaOdbc),
				connectionString);
		}
		#endregion

		#region UseSqlCe
		/// <summary>
		/// Configure connection to use SQL CE provider and connection string.
		/// </summary>
		/// <param name="connectionString">SQL CE connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseSqlCe(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.SqlCe, connectionString);
		}
		#endregion

		#region UseAse
		/// <summary>
		/// Configure connection to use SAP/Sybase ASE default provider and connection string.
		/// </summary>
		/// <param name="connectionString">SAP/Sybase ASE connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Provider selection available only for .NET Framework applications.
		/// </para>
		/// <para>
		/// Default provider will be choosen by probing current folder for provider assembly and if it is not found, default to official <c>Sybase.AdoNet45.AseClient</c> provider.
		/// </para>
		/// </remarks>
		public LinqToDbConnectionOptionsBuilder UseAse(string connectionString)
		{
			return UseConnectionString(LinqToDB.ProviderName.Sybase, connectionString);
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific SAP/Sybase ASE provider and connection string.
		/// </summary>
		/// <param name="connectionString">SAP/Sybase ASE connection string.</param>
		/// <param name="useNativeProvider">if <c>true</c>, <c>Sybase.AdoNet45.AseClient</c> provider will be used; othwerwise managed <c>AdoNetCore.AseClient</c>.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public LinqToDbConnectionOptionsBuilder UseAse(string connectionString, bool useNativeProvider)
		{
			return UseConnectionString(
				SybaseTools.GetDataProvider(useNativeProvider ? LinqToDB.ProviderName.Sybase : LinqToDB.ProviderName.SybaseManaged),
				connectionString);
		}
#endif
		#endregion

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
