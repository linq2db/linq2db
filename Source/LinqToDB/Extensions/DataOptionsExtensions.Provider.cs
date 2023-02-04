using System;

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

	public static partial class DataOptionsExtensions
	{
		#region UseSqlServer

		/// <summary>
		/// Configure connection to use specific SQL Server provider, dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <param name="provider">SQL Server provider to use.</param>
		/// <param name="dialect">SQL Server dialect support level.</param>
		/// <param name="optionSetter">Optional <see cref="SqlServerOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSqlServer(this DataOptions options, string connectionString,
			SqlServerVersion  dialect                              = SqlServerVersion. AutoDetect,
			SqlServerProvider provider                             = SqlServerProvider.AutoDetect,
			Func<SqlServerOptions, SqlServerOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = DataProvider.SqlServer.SqlServerTools.ProviderDetector.CreateOptions(options, dialect, provider);

			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseOracle

		/// <summary>
		/// Configure connection to use Oracle default provider, specific dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="optionSetter">Optional <see cref="OracleOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// </remarks>
		public static DataOptions UseOracle(this DataOptions options, string connectionString,
			Func<OracleOptions,OracleOptions>? optionSetter = null)
		{
			options = options
				.UseConnectionString(
					OracleTools.GetDataProvider(
						version : OracleTools.DefaultVersion,
						provider: OracleProvider.Managed),
					connectionString);

			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use specific Oracle provider, dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <param name="provider">ADO.NET provider to use.</param>
		/// <param name="optionSetter">Optional <see cref="OracleOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseOracle(this DataOptions options, string connectionString, OracleVersion dialect, OracleProvider provider,
			Func<OracleOptions, OracleOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options = OracleTools.ProviderDetector.CreateOptions(options, dialect, provider);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use specific Oracle provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="provider">ADO.NET provider to use.</param>
		/// <param name="optionSetter">Optional <see cref="OracleOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
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
		/// Configure connection to use PostgreSQL Npgsql provider, default dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">PostgreSQL connection string.</param>
		/// <param name="optionSetter">Optional <see cref="PostgreSQLOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// PostgreSQL dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="PostgreSQLTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="PostgreSQLVersion.v92"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UsePostgreSQL(DataOptions, string, PostgreSQLVersion, Func{PostgreSQLOptions, PostgreSQLOptions}?)"/> overload.
		/// </remarks>
		public static DataOptions UsePostgreSQL(this DataOptions options, string connectionString,
			Func<PostgreSQLOptions, PostgreSQLOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.PostgreSQL, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use PostgreSQL Npgsql provider, specific dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">PostgreSQL connection string.</param>
		/// <param name="dialect">PostgreSQL dialect support level.</param>
		/// <param name="optionSetter">Optional <see cref="PostgreSQLOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UsePostgreSQL(this DataOptions options, string connectionString, PostgreSQLVersion dialect,
			Func<PostgreSQLOptions, PostgreSQLOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(connectionString);
			options =  PostgreSQLTools.ProviderDetector.CreateOptions(options, dialect, default);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseMySql

		/// <summary>
		/// Configure connection to use MySql default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <param name="optionSetter">Optional <see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be chosen by probing current folder for provider assembly and if it is not found, default to <c>MySql.Data</c> provider.
		/// </para>
		/// </remarks>
		public static DataOptions UseMySql(this DataOptions options, string connectionString,
			Func<MySqlOptions, MySqlOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.MySql, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>MySql.Data</c> MySql provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <param name="optionSetter">Optional <see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseMySqlData(this DataOptions options, string connectionString,
			Func<MySqlOptions, MySqlOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.MySqlOfficial, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>MySqlConnector</c> MySql provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <param name="optionSetter">Optional <see cref="MySqlOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseMySqlConnector(this DataOptions options, string connectionString,
			Func<MySqlOptions, MySqlOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.MySqlConnector, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseSQLite

		/// <summary>
		/// Configure connection to use SQLite default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be chosen by probing current folder for provider assembly and if it is not found, default to <c>System.Data.Sqlite</c> provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSQLiteOfficial(DataOptions, string, Func{SQLiteOptions, SQLiteOptions}?)"/> and <see cref="UseSQLiteMicrosoft(DataOptions, string, Func{SQLiteOptions, SQLiteOptions}?)"/> methods.
		/// </remarks>
		public static DataOptions UseSQLite(this DataOptions options, string connectionString,
			Func<SQLiteOptions, SQLiteOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.SQLite, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>System.Data.Sqlite</c> SQLite provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSQLiteOfficial(this DataOptions options, string connectionString,
			Func<SQLiteOptions, SQLiteOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.SQLiteClassic, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use <c>Microsoft.Data.Sqlite</c> SQLite provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SQLiteOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSQLiteMicrosoft(this DataOptions options, string connectionString,
			Func<SQLiteOptions, SQLiteOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.SQLiteMS, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseAccess

		/// <summary>
		/// Configure connection to use Access default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <param name="optionSetter">Optional <see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider determined by inspecting connection string for OleDb or ODBC-specific markers and otherwise defaults to OleDb provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseAccessOleDb(DataOptions, string, Func{AccessOptions, AccessOptions}?)"/> and <see cref="UseAccessOdbc"/> methods.
		/// </remarks>
		public static DataOptions UseAccess(this DataOptions options, string connectionString,
			Func<AccessOptions, AccessOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(AccessTools.GetDataProvider(ProviderName.Access), connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use Access OleDb provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <param name="optionSetter">Optional <see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseAccessOleDb(this DataOptions options, string connectionString,
			Func<AccessOptions, AccessOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(AccessTools.GetDataProvider(ProviderName.Access), connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use Access ODBC provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <param name="optionSetter">Optional <see cref="AccessOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseAccessOdbc(this DataOptions options, string connectionString,
			Func<AccessOptions, AccessOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(AccessTools.GetDataProvider(ProviderName.AccessOdbc), connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseDB2

		/// <summary>
		/// Configure connection to use DB2 default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">DB2 connection string.</param>
		/// <param name="optionSetter">Optional <see cref="DB2Options"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// DB2 provider will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="DB2Tools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <c>DB2 LUW</c> provider will be chosen.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseDB2(DataOptions, string, DB2Version, Func{DB2Options, DB2Options}?)"/> overload.
		/// </remarks>
		public static DataOptions UseDB2(this DataOptions options, string connectionString,
			Func<DB2Options, DB2Options>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.DB2, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use specific DB2 provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">DB2 connection string.</param>
		/// <param name="version">DB2 server version.</param>
		/// <param name="optionSetter">Optional <see cref="DB2Options"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseDB2(this DataOptions options, string connectionString, DB2Version version,
			Func<DB2Options, DB2Options>? optionSetter = null)
		{
			options = options.UseConnectionString(DB2Tools.GetDataProvider(version), connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseFirebird

		/// <summary>
		/// Configure connection to use Firebird provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Firebird connection string.</param>
		/// <param name="optionSetter">Optional <see cref="FirebirdOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseFirebird(this DataOptions options, string connectionString,
			Func<FirebirdOptions, FirebirdOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.Firebird, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseInformix

		/// <summary>
		/// Configure connection to use Informix default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Informix connection string.</param>
		/// <param name="optionSetter">Optional <see cref="InformixOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be chosen by probing current folder for provider assembly and if it is not found, default to <c>IBM.Data.DB2</c> provider.
		/// This is not applicable to .NET Core applications as they always use <c>IBM.Data.DB2</c> provider.
		/// </para>
		/// </remarks>
		public static DataOptions UseInformix(this DataOptions options, string connectionString,
			Func<InformixOptions, InformixOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.Informix, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific Informix provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Informix connection string.</param>
		/// <param name="useDB2Provider">if <c>true</c>, <c>IBM.Data.DB2</c> provider will be used; otherwise <c>IBM.Data.Informix</c>.</param>
		/// <param name="optionSetter">Optional <see cref="InformixOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
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
		/// Configure connection to use SAP HANA default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be <c>Sap.Data.Hana</c> native provider for .NET Framework and .NET Core applications and ODBC provider for .NET STANDARD builds.
		/// </para>
		/// </remarks>
		public static DataOptions UseSapHana(this DataOptions options, string connectionString,
			Func<SapHanaOptions, SapHanaOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.SapHana, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use native SAP HANA provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSapHanaNative(this DataOptions options, string connectionString,
			Func<SapHanaOptions, SapHanaOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(
				SapHanaTools.GetDataProvider(ProviderName.SapHanaNative),
				connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		/// <summary>
		/// Configure connection to use SAP HANA ODBC provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SapHanaOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSapHanaODBC(this DataOptions options, string connectionString,
			Func<SapHanaOptions, SapHanaOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(
				SapHanaTools.GetDataProvider(ProviderName.SapHanaOdbc),
				connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseSqlCe

		/// <summary>
		/// Configure connection to use SQL CE provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQL CE connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SqlCeOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSqlCe(this DataOptions options, string connectionString,
			Func<SqlCeOptions, SqlCeOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.SqlCe, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion

		#region UseAse

		/// <summary>
		/// Configure connection to use SAP/Sybase ASE default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP/Sybase ASE connection string.</param>
		/// <param name="optionSetter">Optional <see cref="SybaseOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Provider selection available only for .NET Framework applications.
		/// </para>
		/// <para>
		/// Default provider will be choosen by probing current folder for provider assembly and if it is not found, default to official <c>Sybase.AdoNet45.AseClient</c> provider.
		/// </para>
		/// </remarks>
		public static DataOptions UseAse(this DataOptions options, string connectionString,
			Func<SybaseOptions, SybaseOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ProviderName.Sybase, connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific SAP/Sybase ASE provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP/Sybase ASE connection string.</param>
		/// <param name="useNativeProvider">if <c>true</c>, <c>Sybase.AdoNet45.AseClient</c> provider will be used; otherwise managed <c>AdoNetCore.AseClient</c>.</param>
		/// <param name="optionSetter">Optional <see cref="SybaseOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
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
		/// Configure connection to use UseClickHouse provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="provider">ClickHouse provider.</param>
		/// <param name="connectionString">ClickHouse connection string.</param>
		/// <param name="optionSetter">Optional <see cref="ClickHouseOptions"/> configuration callback.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseClickHouse(this DataOptions options, ClickHouseProvider provider, string connectionString,
			Func<ClickHouseOptions, ClickHouseOptions>? optionSetter = null)
		{
			options = options.UseConnectionString(ClickHouseTools.GetDataProvider(provider), connectionString);
			return optionSetter != null ? options.WithOptions(optionSetter) : options;
		}

		#endregion
	}
}
