using System;

using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace LinqToDB
{
	using DataProvider.Access;
	using DataProvider.ClickHouse;
	using DataProvider.DB2;
	using DataProvider.Firebird;
	using DataProvider.Informix;
	using DataProvider.MySql;
	using DataProvider.Oracle;
	using DataProvider.PostgreSQL;
	using DataProvider.SapHana;
	using DataProvider.SqlCe;
	using DataProvider.SQLite;
	using DataProvider.SqlServer;
	using DataProvider.Sybase;

	/*
	 * To define database configuration overloads stick to those rules:
	 * 
	 * 1. All overloads should have same name: "Use<Database>" (e.g. "Use<Database>Odbc" is not valid name as it contains specific provider name)
	 * 
	 * 2. All overloads should accept "Func<*Options, *Options>[?] optionSetter[ = null]" parameter as last parameter
	 * 
	 * 3. There should be only two or four overloads for each database:
	 * 
	 * For database without multiple providers/dialects - two methods:
	 *    - Use(optionSetter = null)
	 *    - Use(connectionString, optionSetter = null)
	 * 
	 * For database with multiple providers and/or dialects configuration - four methods:
	 *    - Use(optionSetter) // note that setter is not optional to avoid overload conflicts
	 *    - Use(connectionString, optionSetter) // note that setter is not optional to avoid overload conflicts
	 *    - Use(dialect, provider, optionSetter = null)
	 *    - Use(dialect, provider, connectionString, optionSetter = null)
	 * 
	 * 4. if dialect/provider should have default AutoDetect value
	 * 
	 * Examples.
	 * 
	 * Database with single dialect/provider:
	 * DataOptions UseDB(this DataOptions options,                          Func<DBOptions, DBOptions>? optionSetter = null);
	 * DataOptions UseDB(this DataOptions options, string connectionString, Func<DBOptions, DBOptions>? optionSetter = null);
	 * 
	 * Database with multiple dialects/providers:
	 * DataOptions UseDB(this DataOptions options,                          Func<DBOptions, DBOptions> optionSetter);
	 * DataOptions UseDB(this DataOptions options, string connectionString, Func<DBOptions, DBOptions> optionSetter);
	 * DataOptions UseDB(this DataOptions options,                          DBVersion dialect = DBVersion.AutoDetect, DBProvider provider = DBProvider.AutoDetect, Func<DBOptions, DBOptions>? optionSetter = null);
	 * DataOptions UseDB(this DataOptions options, string connectionString, DBVersion dialect = DBVersion.AutoDetect, DBProvider provider = DBProvider.AutoDetect, Func<DBOptions, DBOptions>? optionSetter = null);
	*/
	public static partial class DataOptionsExtensions
	{
		#region UseSqlServer

		/// <summary>
		/// Configure SQL Server connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="SqlServerOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSqlServer(
			this DataOptions                              options,
			     Func<SqlServerOptions, SqlServerOptions> optionSetter)
		{
			return DataProvider.SqlServer.SqlServerTools.ProviderDetector
				.CreateOptions(options, SqlServerVersion.AutoDetect, SqlServerProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure SQL Server connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <param name="optionSetter"><see cref="SqlServerOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSqlServer(
			this DataOptions                              options,
			     string                                   connectionString,
			     Func<SqlServerOptions, SqlServerOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return DataProvider.SqlServer.SqlServerTools.ProviderDetector
				.CreateOptions(options, SqlServerVersion.AutoDetect, SqlServerProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure SQL Server connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">SQL Server provider to use.</param>
		/// <param name="dialect">SQL Server dialect support level.</param>
		/// <param name="optionSetter">Optional <see cref="SqlServerOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSqlServer(
			this DataOptions                               options,
			     SqlServerVersion                          dialect      = SqlServerVersion. AutoDetect,
			     SqlServerProvider                         provider     = SqlServerProvider.AutoDetect,
			     Func<SqlServerOptions, SqlServerOptions>? optionSetter = null)
		{
			options = DataProvider.SqlServer.SqlServerTools.ProviderDetector.CreateOptions(options, dialect, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure SQL Server connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <param name="provider">SQL Server provider to use.</param>
		/// <param name="dialect">SQL Server dialect support level.</param>
		/// <param name="optionSetter">Optional <see cref="SqlServerOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSqlServer(
			this DataOptions                               options,
			     string                                    connectionString,
			     SqlServerVersion                          dialect      = SqlServerVersion. AutoDetect,
			     SqlServerProvider                         provider     = SqlServerProvider.AutoDetect,
			     Func<SqlServerOptions, SqlServerOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = DataProvider.SqlServer.SqlServerTools.ProviderDetector.CreateOptions(options, dialect, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseOracle

		/// <summary>
		/// Configure Oracle connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="OracleOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseOracle(
			this DataOptions                        options,
			     Func<OracleOptions, OracleOptions> optionSetter)
		{
			return OracleTools.ProviderDetector
				.CreateOptions(options, OracleVersion.AutoDetect, OracleProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure Oracle connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="optionSetter"><see cref="OracleOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseOracle(
			this DataOptions                        options,
			     string                             connectionString,
			     Func<OracleOptions, OracleOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return OracleTools.ProviderDetector
				.CreateOptions(options, OracleVersion.AutoDetect, OracleProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure Oracle connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <param name="provider">ADO.NET provider to use.</param>
		/// <param name="optionSetter">Optional <see cref="OracleOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseOracle(
			this DataOptions                         options,
			     OracleVersion                       dialect      = OracleVersion.AutoDetect,
			     OracleProvider                      provider     = OracleProvider.AutoDetect,
			     Func<OracleOptions, OracleOptions>? optionSetter = null)
		{
			options = OracleTools.ProviderDetector.CreateOptions(options, dialect, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure Oracle connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <param name="provider">ADO.NET provider to use.</param>
		/// <param name="optionSetter">Optional <see cref="OracleOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseOracle(
			this DataOptions                         options,
			     string                              connectionString,
			     OracleVersion                       dialect      = OracleVersion.AutoDetect,
			     OracleProvider                      provider     = OracleProvider.AutoDetect,
			     Func<OracleOptions, OracleOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = OracleTools.ProviderDetector.CreateOptions(options, dialect, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure Oracle connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="provider">ADO.NET provider to use.</param>
		/// <param name="optionSetter">Optional <see cref="OracleOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseOracle)} overload with {nameof(OracleVersion)} and {nameof(OracleProvider)} parameters")]
		[Pure]
		public static DataOptions UseOracle(this DataOptions options, string connectionString, OracleProvider provider,
			Func<OracleOptions, OracleOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = OracleTools.ProviderDetector.CreateOptions(options, OracleVersion.AutoDetect, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UsePostgreSQL

		/// <summary>
		/// Configure PostgreSQL connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="PostgreSQLOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UsePostgreSQL(
			this DataOptions                                options,
			     Func<PostgreSQLOptions, PostgreSQLOptions> optionSetter)
		{
			return PostgreSQLTools.ProviderDetector
				.CreateOptions(options, PostgreSQLVersion.AutoDetect, default)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure PostgreSQL connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">PostgreSQL connection string.</param>
		/// <param name="optionSetter"><see cref="PostgreSQLOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UsePostgreSQL(
			this DataOptions                                options,
			     string                                     connectionString,
			     Func<PostgreSQLOptions, PostgreSQLOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return PostgreSQLTools.ProviderDetector
				.CreateOptions(options, PostgreSQLVersion.AutoDetect, default)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure PostgreSQL connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="dialect">PostgreSQL dialect support level.</param>
		/// <param name="optionSetter">Optional <see cref="PostgreSQLOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UsePostgreSQL(
			this DataOptions                                 options,
			     PostgreSQLVersion                           dialect      = PostgreSQLVersion.AutoDetect,
			     Func<PostgreSQLOptions, PostgreSQLOptions>? optionSetter = null)
		{
			options = PostgreSQLTools.ProviderDetector.CreateOptions(options, dialect, default);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure PostgreSQL connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">PostgreSQL connection string.</param>
		/// <param name="dialect">PostgreSQL dialect support level.</param>
		/// <param name="optionSetter">Optional <see cref="PostgreSQLOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UsePostgreSQL(
			this DataOptions                                 options,
			     string                                      connectionString,
			     PostgreSQLVersion                           dialect      = PostgreSQLVersion.AutoDetect,
			     Func<PostgreSQLOptions, PostgreSQLOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options =  PostgreSQLTools.ProviderDetector.CreateOptions(options, dialect, default);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseMySql

		/// <summary>
		/// Configure MySQL or MariaDB connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be chosen by probing current folder for provider assembly and if it is not found, default to <c>MySqlConnector</c> provider.
		/// </para>
		/// </remarks>
		[Pure]
		public static DataOptions UseMySql(
			this DataOptions                      options,
			     Func<MySqlOptions, MySqlOptions> optionSetter)
		{
			return MySqlTools.ProviderDetector
				.CreateOptions(options, default, MySqlProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure MySQL or MariaDB connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <param name="optionSetter"><see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be chosen by probing current folder for provider assembly and if it is not found, default to <c>MySqlConnector</c> provider.
		/// </para>
		/// </remarks>
		[Pure]
		public static DataOptions UseMySql(
			this DataOptions                      options,
			     string                           connectionString,
			     Func<MySqlOptions, MySqlOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return MySqlTools.ProviderDetector
				.CreateOptions(options, default, MySqlProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure MySQL or MariaDB connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="dialect">MySql dialect.</param>
		/// <param name="provider">MySql ADO.NET provider.</param>
		/// <param name="optionSetter">Optional <see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseMySql(
			this DataOptions                       options,
				 MySqlVersion                      dialect      = MySqlVersion.AutoDetect,
				 MySqlProvider                     provider     = MySqlProvider.AutoDetect,
			     Func<MySqlOptions, MySqlOptions>? optionSetter = null)
		{
			options = MySqlTools.ProviderDetector.CreateOptions(options, dialect, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure MySQL or MariaDB connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <param name="dialect">MySql dialect.</param>
		/// <param name="provider">MySql ADO.NET provider.</param>
		/// <param name="optionSetter">Optional <see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseMySql(
			this DataOptions                       options,
			     string                            connectionString,
				 MySqlVersion                      dialect      = MySqlVersion.AutoDetect,
			     MySqlProvider                     provider     = MySqlProvider.AutoDetect,
			     Func<MySqlOptions, MySqlOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = MySqlTools.ProviderDetector.CreateOptions(options, dialect, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>MySql.Data</c> MySql provider.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter">Optional <see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseMySql)} with {nameof(MySqlProvider)} parameter")]
		[Pure]
		public static DataOptions UseMySqlData(
			this DataOptions                       options,
			     Func<MySqlOptions, MySqlOptions>? optionSetter = null)
		{
			options = options.UseProvider(ProviderName.MySqlOfficial);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>MySql.Data</c> MySql provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <param name="optionSetter">Optional <see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseMySql)} with {nameof(MySqlProvider)} parameter")]
		[Pure]
		public static DataOptions UseMySqlData(
			this DataOptions                       options,
			     string                            connectionString,
			     Func<MySqlOptions, MySqlOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.MySqlOfficial, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>MySqlConnector</c> MySql provider.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter">Optional <see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseMySql)} with {nameof(MySqlProvider)} parameter")]
		[Pure]
		public static DataOptions UseMySqlConnector(
			this DataOptions                       options,
			     Func<MySqlOptions, MySqlOptions>? optionSetter = null)
		{
			options = options.UseProvider(ProviderName.MySqlConnector);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>MySqlConnector</c> MySql provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <param name="optionSetter">Optional <see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseMySql)} with {nameof(MySqlProvider)} parameter")]
		[Pure]
		public static DataOptions UseMySqlConnector(
			this DataOptions                       options,
			     string                            connectionString,
			     Func<MySqlOptions, MySqlOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.MySqlConnector, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseSQLite

		/// <summary>
		/// Configure SQLite connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSQLite(
			this DataOptions                        options,
			     Func<SQLiteOptions, SQLiteOptions> optionSetter)
		{
			return SQLiteTools.ProviderDetector
				.CreateOptions(options, default, SQLiteProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure SQLite connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <param name="optionSetter"><see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSQLite(
			this DataOptions                        options,
			     string                             connectionString,
			     Func<SQLiteOptions, SQLiteOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return SQLiteTools.ProviderDetector
				.CreateOptions(options, default, SQLiteProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure SQLite connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">ADO.NET provider.</param>
		/// <param name="optionSetter">Optional <see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSQLite(
			this DataOptions                         options,
			     SQLiteProvider                      provider     = SQLiteProvider.AutoDetect,
			     Func<SQLiteOptions, SQLiteOptions>? optionSetter = null)
		{
			options = SQLiteTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure SQLite connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <param name="provider">ADO.NET provider.</param>
		/// <param name="optionSetter">Optional <see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSQLite(
			this DataOptions                         options,
			     string                              connectionString,
			     SQLiteProvider                      provider     = SQLiteProvider.AutoDetect,
			     Func<SQLiteOptions, SQLiteOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = SQLiteTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>System.Data.Sqlite</c> SQLite provider.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter">Optional <see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseSQLite)} with {nameof(SQLiteProvider)} parameter")]
		[Pure]
		public static DataOptions UseSQLiteOfficial(this DataOptions options,
			Func<SQLiteOptions, SQLiteOptions>? optionSetter = null)
		{
			options = options.UseProvider(ProviderName.SQLiteClassic);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>System.Data.Sqlite</c> SQLite provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseSQLite)} with {nameof(SQLiteProvider)} parameter")]
		[Pure]
		public static DataOptions UseSQLiteOfficial(this DataOptions options, string connectionString,
			Func<SQLiteOptions, SQLiteOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.SQLiteClassic, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>Microsoft.Data.Sqlite</c> SQLite provider.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter">Optional <see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseSQLite)} with {nameof(SQLiteProvider)} parameter")]
		[Pure]
		public static DataOptions UseSQLiteMicrosoft(this DataOptions options,
			Func<SQLiteOptions, SQLiteOptions>? optionSetter = null)
		{
			options = options.UseProvider(ProviderName.SQLiteMS);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>Microsoft.Data.Sqlite</c> SQLite provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseSQLite)} with {nameof(SQLiteProvider)} parameter")]
		[Pure]
		public static DataOptions UseSQLiteMicrosoft(this DataOptions options, string connectionString,
			Func<SQLiteOptions, SQLiteOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.SQLiteMS, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseAccess

		/// <summary>
		/// Configure Access connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseAccess(
			this DataOptions                        options,
			     Func<AccessOptions, AccessOptions> optionSetter)
		{
			return AccessTools.ProviderDetector
				.CreateOptions(options, default, AccessProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure Access connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <param name="optionSetter"><see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseAccess(
			this DataOptions                        options,
			     string                             connectionString,
			     Func<AccessOptions, AccessOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return AccessTools.ProviderDetector
				.CreateOptions(options, default, AccessProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure Access connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">Access ADO.NET provider.</param>
		/// <param name="optionSetter">Optional <see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseAccess(
			this DataOptions options,
			     AccessProvider                      provider     = AccessProvider.AutoDetect,
			     Func<AccessOptions, AccessOptions>? optionSetter = null)
		{
			options = AccessTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure Access connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">Access ADO.NET provider.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <param name="optionSetter">Optional <see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseAccess(
			this DataOptions                         options,
			     string                              connectionString,
			     AccessProvider                      provider     = AccessProvider.AutoDetect,
			     Func<AccessOptions, AccessOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = AccessTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use Access OleDb provider.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter">Optional <see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseAccess)} overload with {nameof(AccessProvider)} parameter")]
		[Pure]
		public static DataOptions UseAccessOleDb(this DataOptions options,
			Func<AccessOptions, AccessOptions>? optionSetter = null)
		{
			options = options.UseProvider(ProviderName.Access);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use Access OleDb provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <param name="optionSetter">Optional <see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseAccess)} overload with {nameof(AccessProvider)} parameter")]
		[Pure]
		public static DataOptions UseAccessOleDb(this DataOptions options, string connectionString,
			Func<AccessOptions, AccessOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.Access, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use Access ODBC provider.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter">Optional <see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseAccess)} overload with {nameof(AccessProvider)} parameter")]
		[Pure]
		public static DataOptions UseAccessOdbc(this DataOptions options,
			Func<AccessOptions, AccessOptions>? optionSetter = null)
		{
			options = options.UseProvider(ProviderName.AccessOdbc);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use Access ODBC provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <param name="optionSetter">Optional <see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseAccess)} overload with {nameof(AccessProvider)} parameter")]
		[Pure]
		public static DataOptions UseAccessOdbc(this DataOptions options, string connectionString,
			Func<AccessOptions, AccessOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.AccessOdbc, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseDB2

		/// <summary>
		/// Configure DB2 connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="DB2Options"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		/// <remarks>
		/// <para>
		/// DB2 provider will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="DB2Tools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <c>DB2 LUW</c> provider will be chosen.</item>
		/// </list>
		/// </para>
		/// For more precise configuration use overloads with <see cref="DB2Version"/> parameter.
		/// </remarks>
		[Pure]
		public static DataOptions UseDB2(
			this DataOptions                  options,
			     Func<DB2Options, DB2Options> optionSetter)
		{
			return DB2Tools.ProviderDetector
				.CreateOptions(options, DB2Version.AutoDetect, default)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure DB2 connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">DB2 connection string.</param>
		/// <param name="optionSetter"><see cref="DB2Options"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		/// <remarks>
		/// <para>
		/// DB2 provider will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="DB2Tools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <c>DB2 LUW</c> provider will be chosen.</item>
		/// </list>
		/// </para>
		/// For more precise configuration use overloads with <see cref="DB2Version"/> parameter.
		/// </remarks>
		[Pure]
		public static DataOptions UseDB2(
			this DataOptions                  options,
			     string                       connectionString,
			     Func<DB2Options, DB2Options> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return DB2Tools.ProviderDetector
				.CreateOptions(options, DB2Version.AutoDetect, default)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure DB2 connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="version">DB2 server version.</param>
		/// <param name="optionSetter">Optional <see cref="DB2Options"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseDB2(
			this DataOptions                   options,
			     DB2Version                    version      = DB2Version.AutoDetect,
			     Func<DB2Options, DB2Options>? optionSetter = null)
		{
			options = DB2Tools.ProviderDetector.CreateOptions(options, version, default);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure DB2 connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">DB2 connection string.</param>
		/// <param name="version">DB2 server version.</param>
		/// <param name="optionSetter">Optional <see cref="DB2Options"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseDB2(
			this DataOptions                   options,
			     string                        connectionString,
			     DB2Version                    version      = DB2Version.AutoDetect,
			     Func<DB2Options, DB2Options>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = DB2Tools.ProviderDetector.CreateOptions(options, version, default);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseFirebird

		/// <summary>
		/// Configure Firebird connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="FirebirdOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseFirebird(
			this DataOptions                            options,
			     Func<FirebirdOptions, FirebirdOptions> optionSetter)
		{
			return FirebirdTools.ProviderDetector
				.CreateOptions(options, FirebirdVersion.AutoDetect, default)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure Firebird connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Firebird connection string.</param>
		/// <param name="optionSetter">Optional <see cref="FirebirdOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseFirebird(
			this DataOptions                            options,
			     string                                 connectionString,
			     Func<FirebirdOptions, FirebirdOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return FirebirdTools.ProviderDetector
				.CreateOptions(options, FirebirdVersion.AutoDetect, default)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure Firebird connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="dialect">Firebird dialect support level.</param>
		/// <param name="optionSetter">Optional <see cref="FirebirdOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseFirebird(
			this DataOptions                             options,
				 FirebirdVersion                         dialect      = FirebirdVersion.AutoDetect,
				 Func<FirebirdOptions, FirebirdOptions>? optionSetter = null)
		{
			options = FirebirdTools.ProviderDetector.CreateOptions(options, dialect, default);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure Firebird connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Firebird connection string.</param>
		/// <param name="dialect">Firebird dialect support level.</param>
		/// <param name="optionSetter">Optional <see cref="FirebirdOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseFirebird(
			this DataOptions options,
				 string                                  connectionString,
				 FirebirdVersion                         dialect      = FirebirdVersion.AutoDetect,
				 Func<FirebirdOptions, FirebirdOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = FirebirdTools.ProviderDetector.CreateOptions(options, dialect, default);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseInformix

		/// <summary>
		/// Configure Informix connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="InformixOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be chosen by probing current folder for provider assembly and if it is not found, default to <c>IBM.Data.DB2</c> provider.
		/// This is not applicable to .NET Core applications as they always use <c>IBM.Data.DB2</c> provider.
		/// </para>
		/// </remarks>
		[Pure]
		public static DataOptions UseInformix(
			this DataOptions                            options,
			     Func<InformixOptions, InformixOptions> optionSetter)
		{
			return InformixTools.ProviderDetector
				.CreateOptions(options, default, InformixProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure Informix connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Informix connection string.</param>
		/// <param name="optionSetter"><see cref="InformixOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be chosen by probing current folder for provider assembly and if it is not found, default to <c>IBM.Data.DB2</c> provider.
		/// This is not applicable to .NET Core applications as they always use <c>IBM.Data.DB2</c> provider.
		/// </para>
		/// </remarks>
		[Pure]
		public static DataOptions UseInformix(
			this DataOptions                            options,
			     string                                 connectionString,
			     Func<InformixOptions, InformixOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return InformixTools.ProviderDetector
				.CreateOptions(options, default, InformixProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure Informix connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">ADO.NET provider.</param>
		/// <param name="optionSetter">Optional <see cref="InformixOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseInformix(
			this DataOptions                             options,
			     InformixProvider                        provider     = InformixProvider.AutoDetect,
			     Func<InformixOptions, InformixOptions>? optionSetter = null)
		{
			options = InformixTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure Informix connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">ADO.NET provider.</param>
		/// <param name="connectionString">Informix connection string.</param>
		/// <param name="optionSetter">Optional <see cref="InformixOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseInformix(
			this DataOptions                             options,
			     string                                  connectionString,
			     InformixProvider                        provider     = InformixProvider.AutoDetect,
			     Func<InformixOptions, InformixOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = InformixTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure Informix connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Informix connection string.</param>
		/// <param name="useDB2Provider">if <c>true</c>, <c>IBM.Data.DB2</c> provider will be used; otherwise <c>IBM.Data.Informix</c>.</param>
		/// <param name="optionSetter">Optional <see cref="InformixOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseInformix)} overload with {nameof(InformixProvider)} parameter")]
		[Pure]
		public static DataOptions UseInformix(this DataOptions options, string connectionString, bool useDB2Provider,
			Func<InformixOptions, InformixOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(
				InformixTools.GetDataProvider(useDB2Provider ? ProviderName.InformixDB2 : ProviderName.Informix),
				connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}
#endif

		#endregion

		#region UseSapHana

		/// <summary>
		/// Configure SAP HANA connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be <c>Sap.Data.Hana</c> native provider for .NET Framework and .NET Core applications and ODBC provider for .NET STANDARD builds.
		/// </para>
		/// </remarks>
		[Pure]
		public static DataOptions UseSapHana(
			this DataOptions                          options,
			     Func<SapHanaOptions, SapHanaOptions> optionSetter)
		{
			return SapHanaTools.ProviderDetector
				.CreateOptions(options, default, SapHanaProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure SAP HANA connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <param name="optionSetter"><see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be <c>Sap.Data.Hana</c> native provider for .NET Framework and .NET Core applications and ODBC provider for .NET STANDARD builds.
		/// </para>
		/// </remarks>
		[Pure]
		public static DataOptions UseSapHana(
			this DataOptions                          options,
			     string                               connectionString,
			     Func<SapHanaOptions, SapHanaOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return SapHanaTools.ProviderDetector
				.CreateOptions(options, default, SapHanaProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure SAP HANA connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">SAP HANA ADO.NET provider.</param>
		/// <param name="optionSetter">Optional <see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSapHana(
			this DataOptions                           options,
			     SapHanaProvider                       provider     = SapHanaProvider.AutoDetect,
			     Func<SapHanaOptions, SapHanaOptions>? optionSetter = null)
		{
			options = SapHanaTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure SAP HANA connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <param name="provider">SAP HANA ADO.NET provider.</param>
		/// <param name="optionSetter">Optional <see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSapHana(
			this DataOptions                           options,
			     string                                connectionString,
			     SapHanaProvider                       provider     = SapHanaProvider.AutoDetect,
			     Func<SapHanaOptions, SapHanaOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = SapHanaTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use native SAP HANA provider.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter">Optional <see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseSapHana)} overload")]
		[Pure]
		public static DataOptions UseSapHanaNative(this DataOptions options,
			Func<SapHanaOptions, SapHanaOptions>? optionSetter = null)
		{
			options = options.UseProvider(ProviderName.SapHanaNative);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use native SAP HANA provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseSapHana)} overload")]
		[Pure]
		public static DataOptions UseSapHanaNative(this DataOptions options, string connectionString,
			Func<SapHanaOptions, SapHanaOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.SapHanaNative, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use SAP HANA ODBC provider.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter">Optional <see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseSapHana)} overload")]
		[Pure]
		public static DataOptions UseSapHanaODBC(this DataOptions options,
			Func<SapHanaOptions, SapHanaOptions>? optionSetter = null)
		{
			options = options.UseProvider(ProviderName.SapHanaOdbc);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use SAP HANA ODBC provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>S
		[Obsolete($"Use {nameof(UseSapHana)} overload")]
		[Pure]
		public static DataOptions UseSapHanaODBC(this DataOptions options, string connectionString,
			Func<SapHanaOptions, SapHanaOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.SapHanaOdbc, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseSqlCe

		/// <summary>
		/// Configure SQL CE connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter">Optional <see cref="SqlCeOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSqlCe(
			this DataOptions                       options,
			     Func<SqlCeOptions, SqlCeOptions>? optionSetter = null)
		{
			options = options.UseProvider(ProviderName.SqlCe);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure SQL CE connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQL CE connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SqlCeOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseSqlCe(
			this DataOptions                       options,
			     string                            connectionString,
			     Func<SqlCeOptions, SqlCeOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.SqlCe, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseAse

		/// <summary>
		/// Configure SAP/Sybase ASE connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="SybaseOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseAse(
			this DataOptions                        options,
			     Func<SybaseOptions, SybaseOptions> optionSetter)
		{
			return SybaseTools.ProviderDetector
				.CreateOptions(options, default, SybaseProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure SAP/Sybase ASE connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP/Sybase ASE connection string.</param>
		/// <param name="optionSetter"><see cref="SybaseOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseAse(
			this DataOptions                        options,
			     string                             connectionString,
			     Func<SybaseOptions, SybaseOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return SybaseTools.ProviderDetector
				.CreateOptions(options, default, SybaseProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure SAP/Sybase ASE connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">ADO.NET provider.</param>
		/// <param name="optionSetter">Optional <see cref="SybaseOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseAse(
			this DataOptions                         options,
			     SybaseProvider                      provider     = SybaseProvider.AutoDetect,
			     Func<SybaseOptions, SybaseOptions>? optionSetter = null)
		{
			options = SybaseTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure SAP/Sybase ASE connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">ADO.NET provider.</param>
		/// <param name="connectionString">SAP/Sybase ASE connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SybaseOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseAse(
			this DataOptions                         options,
			     string                              connectionString,
			     SybaseProvider                      provider     = SybaseProvider.AutoDetect,
			     Func<SybaseOptions, SybaseOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = SybaseTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure SAP/Sybase ASE connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP/Sybase ASE connection string.</param>
		/// <param name="useNativeProvider">if <c>true</c>, <c>Sybase.AdoNet45.AseClient</c> provider will be used; otherwise managed <c>AdoNetCore.AseClient</c>.</param>
		/// <param name="optionSetter">Optional <see cref="SybaseOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseAse)} overload with {nameof(SybaseProvider)} parameter")]
		[Pure]
		public static DataOptions UseAse(this DataOptions options, string connectionString, bool useNativeProvider,
			Func<SybaseOptions, SybaseOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(
				SybaseTools.GetDataProvider(useNativeProvider ? ProviderName.Sybase : ProviderName.SybaseManaged),
				connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}
#endif

		#endregion

		#region UseClickHouse

		/// <summary>
		/// Configure ClickHouse connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="optionSetter"><see cref="ClickHouseOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseClickHouse(
			this DataOptions                                options,
			     Func<ClickHouseOptions, ClickHouseOptions> optionSetter)
		{

			return ClickHouseTools.ProviderDetector
				.CreateOptions(options, default, ClickHouseProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure ClickHouse connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">ClickHouse connection string.</param>
		/// <param name="optionSetter"><see cref="ClickHouseOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseClickHouse(
			this DataOptions                                options,
			     string                                     connectionString,
			     Func<ClickHouseOptions, ClickHouseOptions> optionSetter)
		{
			options = options.UseConnectionString(connectionString);
			return ClickHouseTools.ProviderDetector
				.CreateOptions(options, default, ClickHouseProvider.AutoDetect)
				.WithOptions(optionSetter);
		}

		/// <summary>
		/// Configure ClickHouse connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">ClickHouse provider.</param>
		/// <param name="optionSetter">Optional <see cref="ClickHouseOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseClickHouse(
			this DataOptions options,
			     ClickHouseProvider                          provider     = ClickHouseProvider.AutoDetect,
			     Func<ClickHouseOptions, ClickHouseOptions>? optionSetter = null)
		{
			options = ClickHouseTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure ClickHouse connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">ClickHouse connection string.</param>
		/// <param name="provider">ClickHouse provider.</param>
		/// <param name="optionSetter">Optional <see cref="ClickHouseOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Pure]
		public static DataOptions UseClickHouse(
			this DataOptions                                 options,
			     string                                      connectionString,
			     ClickHouseProvider                          provider     = ClickHouseProvider.AutoDetect,
			     Func<ClickHouseOptions, ClickHouseOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = ClickHouseTools.ProviderDetector.CreateOptions(options, default, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure ClickHouse connection.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">ClickHouse provider.</param>
		/// <param name="connectionString">ClickHouse connection string.</param>
		/// <param name="optionSetter">Optional <see cref="ClickHouseOptions"/> configuration callback.</param>
		/// <returns>New options instance with applied changes.</returns>
		[Obsolete($"Use {nameof(UseClickHouse)} overload with {nameof(connectionString)} parameter first")]
		[Pure]
		public static DataOptions UseClickHouse(this DataOptions options, ClickHouseProvider provider, string connectionString,
			Func<ClickHouseOptions, ClickHouseOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ClickHouseTools.GetDataProvider(provider), connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion
	}
}
