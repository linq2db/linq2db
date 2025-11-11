using System;
using System.Data.Common;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.DataProvider.Informix;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SapHana;
using LinqToDB.DataProvider.SqlCe;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.DataProvider.Sybase;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.ClickHouse;

// ReSharper disable once CheckNamespace
namespace LinqToDB
{
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
			return options.UseSqlServer(SqlServerVersion.AutoDetect, SqlServerProvider.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseSqlServer(optionSetter);
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
			options = SqlServerTools.ProviderDetector.CreateOptions(options, dialect, provider);
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
			return options.UseConnectionString(connectionString).UseSqlServer(dialect, provider, optionSetter);
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
			return options.UseOracle(OracleVersion.AutoDetect, OracleProvider.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseOracle(optionSetter);
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
			return options.UseConnectionString(connectionString).UseOracle(dialect, provider, optionSetter);
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
			return options.UsePostgreSQL(PostgreSQLVersion.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UsePostgreSQL(optionSetter);
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
			return options.UseConnectionString(connectionString).UsePostgreSQL(dialect, optionSetter);
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
			return options.UseMySql(MySqlVersion.AutoDetect, MySqlProvider.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseMySql(optionSetter);
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
			return options.UseConnectionString(connectionString).UseMySql(dialect, provider, optionSetter);
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
			return options.UseSQLite(SQLiteProvider.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseSQLite(optionSetter);
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
			return options.UseConnectionString(connectionString).UseSQLite(provider, optionSetter);
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
			return options.UseAccess(AccessProvider.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseAccess(optionSetter);
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
			return options.UseConnectionString(connectionString).UseAccess(provider, optionSetter);
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
			return options.UseDB2(DB2Version.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseDB2(optionSetter);
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
			return options.UseConnectionString(connectionString).UseDB2(version, optionSetter);
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
			return options.UseFirebird(FirebirdVersion.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseFirebird(optionSetter);
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
			return options.UseConnectionString(connectionString).UseFirebird(dialect, optionSetter);
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
			return options.UseInformix(InformixProvider.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseInformix(optionSetter);
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
			return options.UseConnectionString(connectionString).UseInformix(provider, optionSetter);
		}

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
			return options.UseSapHana(SapHanaProvider.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseSapHana(optionSetter);
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
			return options.UseConnectionString(connectionString).UseSapHana(provider, optionSetter);
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
			return options.UseConnectionString(connectionString).UseSqlCe(optionSetter);
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
			return options.UseAse(SybaseProvider.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseAse(optionSetter);
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
			return options.UseConnectionString(connectionString).UseAse(provider, optionSetter);
		}

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

			return options.UseClickHouse(ClickHouseProvider.AutoDetect, optionSetter);
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
			return options.UseConnectionString(connectionString).UseClickHouse(optionSetter);
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
			if (optionSetter != null)
				options = options.WithOptions(optionSetter);

			// if ClickHouseOptions.HttpClient is set, plumb it now..
			var chops = options.FindOrDefault(ClickHouseOptions.Default);
			if (options.ConnectionOptions.DataProvider is ClickHouseDataProvider chProvider && chProvider.Provider == ClickHouseProvider.ClickHouseDriver && chops.HttpClient != null)
			{
				options = options.WithOptions<ConnectionOptions>(static co => co with
				{
					ConnectionFactory = opts =>
					{
						var innerChops = opts.FindOrDefault(ClickHouseOptions.Default);
						var connString = opts.ConnectionOptions.ConnectionString ?? "host=localhost";
						var dataProvider = (ClickHouseDataProvider)opts.ConnectionOptions.DataProvider!;
						var adapter = dataProvider.Adapter;
						if (innerChops.HttpClient != null)
						{
							var connectionType = adapter.ConnectionType;
							var ctor = connectionType.GetConstructor(new[] { typeof(string), innerChops.HttpClient.GetType() });
							if (ctor != null)
								return (DbConnection)ctor.InvokeExt(new object[] { connString, innerChops.HttpClient });
						}

						return adapter.CreateConnection(connString);
					}
				});
			}

			return options;
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
			return options.UseConnectionString(connectionString).UseClickHouse(provider, optionSetter);
		}

		#endregion
	}
}
