using System;
using LinqToDB.Data;

// ReSharper disable once CheckNamespace
namespace LinqToDB
{
	using DataProvider.Access;
	using DataProvider.DB2;
	using DataProvider.Informix;
	using DataProvider.Oracle;
	using DataProvider.PostgreSQL;
	using DataProvider.SapHana;
	using DataProvider.SqlServer;
	using DataProvider.Sybase;

	public static partial class DataOptionsExtensions
	{
		#region UseSqlServer

		/// <summary>
		/// Configure connection to use SQL Server default provider, dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider configured using <see cref="SqlServerTools.Provider"/> option and set to <see cref="SqlServerProvider.SystemDataSqlClient"/> by default.
		/// </para>
		/// <para>
		/// SQL Server dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="SqlServerTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="SqlServerVersion.v2008"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSqlServer(DataOptions, string, SqlServerVersion, SqlServerProvider)"/> overload.
		/// </remarks>
		public static DataOptions UseSqlServer(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.SqlServer, connectionString);
		}

		/// <summary>
		/// Configure connection to use specific SQL Server provider, dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <param name="provider">SQL Server provider to use.</param>
		/// <param name="dialect">SQL Server dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSqlServer(this DataOptions options, string connectionString, SqlServerVersion dialect, SqlServerProvider provider)
		{
			if (dialect == SqlServerVersion.AutoDetect)
			{
				if (SqlServerTools.TryGetCachedServerVersion(connectionString, out var version))
					dialect = version ?? SqlServerVersion.v2008;
				else
					return options.WithOptions<ConnectionOptions>(o => o with
					{
						ConnectionString    = connectionString,
						DataProviderFactory = () =>
						{
							var v = SqlServerTools.DetectServerVersionCached(provider, connectionString);
							return SqlServerTools.GetDataProvider(v ?? SqlServerVersion.v2008, provider, connectionString);
						}
					});
			}

			return options.UseConnectionString(SqlServerTools.GetDataProvider(dialect, provider, null), connectionString);
		}

		/// <summary>
		/// Configure connection to use specific SQL Server provider, dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <param name="provider">SQL Server provider to use.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSqlServer(this DataOptions options, string connectionString, SqlServerProvider provider)
		{
			return UseSqlServer(options, connectionString, SqlServerVersion.AutoDetect, provider);
		}

		/// <summary>
		/// Configure connection to use specific SQL Server provider, dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <param name="dialect">SQL Server dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSqlServer(this DataOptions options, string connectionString, SqlServerVersion dialect)
		{
			return UseSqlServer(options, connectionString, SqlServerVersion.AutoDetect, SqlServerTools.Provider);
		}

		#endregion

		#region UseOracle

		/// <summary>
		/// Configure connection to use Oracle default provider, dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseOracle(DataOptions, string, OracleVersion, OracleProvider)"/> overload.
		/// </remarks>
		public static DataOptions UseOracle(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.Oracle, connectionString);
		}

		/// <summary>
		/// Configure connection to use Oracle default provider, specific dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// </remarks>
		[Obsolete("Use UseOracle(this LinqToDBConnectionOptionsBuilder builder, string connectionString, OracleVersion dialect, OracleProvider provider) overload")]
		public static DataOptions UseOracle(this DataOptions options, string connectionString, OracleVersion dialect)
		{
			return options.UseConnectionString(OracleTools.GetDataProvider(ProviderName.Oracle, null, dialect), connectionString);
		}

		/// <summary>
		/// Configure connection to use Oracle default provider, specific dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// </remarks>
		public static DataOptions UseOracle(this DataOptions options, string connectionString, Func<OracleOptions,OracleOptions> optionSetter)
		{
			return options
				.UseConnectionString(
					OracleTools.GetDataProvider(
						version  : OracleTools.DefaultVersion,
						provider : OracleProvider.Managed),
					connectionString)
				.WithOptions(optionSetter);
		}

#if NETFRAMEWORK

		/// <summary>
		/// Configure connection to use specific Oracle provider, dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <param name="useNativeProvider">if <c>true</c>, <c>Oracle.DataAccess</c> provider will be used; otherwise managed <c>Oracle.ManagedDataAccess</c>.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		[Obsolete("Use UseOracle(this LinqToDBConnectionOptionsBuilder builder, string connectionString, OracleVersion dialect, OracleProvider provider) overload")]
		public static DataOptions UseOracle(this DataOptions options, string connectionString, OracleVersion dialect, bool useNativeProvider)
		{
			return options.UseConnectionString(
				OracleTools.GetDataProvider(
					useNativeProvider ? ProviderName.OracleNative : ProviderName.OracleManaged,
					null,
					dialect),
				connectionString);
		}

#endif

		/// <summary>
		/// Configure connection to use specific Oracle provider, dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <param name="provider">ADO.NET provider to use.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseOracle(this DataOptions builder, string connectionString, OracleVersion dialect, OracleProvider provider)
		{
			return builder.UseConnectionString(
				OracleTools.GetDataProvider(dialect, provider),
				connectionString);
		}
		#endregion

		#region UsePostgreSQL

		/// <summary>
		/// Configure connection to use PostgreSQL Npgsql provider, default dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">PostgreSQL connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// PostgreSQL dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="PostgreSQLTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="PostgreSQLVersion.v92"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UsePostgreSQL(DataOptions, string, PostgreSQLVersion)"/> overload.
		/// </remarks>
		public static DataOptions UsePostgreSQL(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.PostgreSQL, connectionString);
		}

		/// <summary>
		/// Configure connection to use PostgreSQL Npgsql provider, specific dialect and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">PostgreSQL connection string.</param>
		/// <param name="dialect">PostgreSQL dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UsePostgreSQL(this DataOptions options, string connectionString, PostgreSQLVersion dialect)
		{
			return options.UseConnectionString(PostgreSQLTools.GetDataProvider(dialect), connectionString);
		}

		#endregion

		#region UseMySql

		/// <summary>
		/// Configure connection to use MySql default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be chosen by probing current folder for provider assembly and if it is not found, default to <c>MySql.Data</c> provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseMySqlData(DataOptions, string)"/> and <see cref="UseMySqlConnector(DataOptions, string)"/> methods.
		/// </remarks>
		public static DataOptions UseMySql(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.MySql, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>MySql.Data</c> MySql provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseMySqlData(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.MySqlOfficial, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>MySqlConnector</c> MySql provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseMySqlConnector(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.MySqlConnector, connectionString);
		}

		#endregion

		#region UseSQLite

		/// <summary>
		/// Configure connection to use SQLite default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be chosen by probing current folder for provider assembly and if it is not found, default to <c>System.Data.Sqlite</c> provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSQLiteOfficial(DataOptions, string)"/> and <see cref="UseSQLiteMicrosoft(DataOptions, string)"/> methods.
		/// </remarks>
		public static DataOptions UseSQLite(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.SQLite, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>System.Data.Sqlite</c> SQLite provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSQLiteOfficial(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.SQLiteClassic, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>Microsoft.Data.Sqlite</c> SQLite provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSQLiteMicrosoft(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.SQLiteMS, connectionString);
		}

		#endregion

		#region UseAccess

		/// <summary>
		/// Configure connection to use Access default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider determined by inspecting connection string for OleDb or ODBC-specific markers and otherwise defaults to OleDb provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseAccessOleDb(DataOptions, string)"/> and <see cref="UseAccessOdbc"/> methods.
		/// </remarks>
		public static DataOptions UseAccess(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(AccessTools.GetDataProvider(ProviderName.Access), connectionString);
		}

		/// <summary>
		/// Configure connection to use Access OleDb provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseAccessOleDb(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(AccessTools.GetDataProvider(ProviderName.Access), connectionString);
		}

		/// <summary>
		/// Configure connection to use Access ODBC provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseAccessOdbc(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(AccessTools.GetDataProvider(ProviderName.AccessOdbc), connectionString);
		}

		#endregion

		#region UseDB2

		/// <summary>
		/// Configure connection to use DB2 default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">DB2 connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// DB2 provider will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="DB2Tools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <c>DB2 LUW</c> provider will be chosen.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseDB2(DataOptions, string, DB2Version)"/> overload.
		/// </remarks>
		public static DataOptions UseDB2(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.DB2, connectionString);
		}

		/// <summary>
		/// Configure connection to use specific DB2 provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">DB2 connection string.</param>
		/// <param name="version">DB2 server version.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseDB2(this DataOptions options, string connectionString, DB2Version version)
		{
			return options.UseConnectionString(DB2Tools.GetDataProvider(version), connectionString);
		}

		#endregion

		#region UseFirebird

		/// <summary>
		/// Configure connection to use Firebird provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Firebird connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseFirebird(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.Firebird, connectionString);
		}

		#endregion

		#region UseInformix

		/// <summary>
		/// Configure connection to use Informix default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Informix connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be chosen by probing current folder for provider assembly and if it is not found, default to <c>IBM.Data.DB2</c> provider.
		/// This is not applicable to .NET Core applications as they always use <c>IBM.Data.DB2</c> provider.
		/// </para>
		/// </remarks>
		public static DataOptions UseInformix(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.Informix, connectionString);
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific Informix provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">Informix connection string.</param>
		/// <param name="useDB2Provider">if <c>true</c>, <c>IBM.Data.DB2</c> provider will be used; otherwise <c>IBM.Data.Informix</c>.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseInformix(this DataOptions options, string connectionString, bool useDB2Provider)
		{
			return options.UseConnectionString(
				InformixTools.GetDataProvider(useDB2Provider ? ProviderName.InformixDB2 : ProviderName.Informix),
				connectionString);
		}
#endif

		#endregion

		#region UseSapHana

		/// <summary>
		/// Configure connection to use SAP HANA default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be <c>Sap.Data.Hana</c> native provider for .NET Framework and .NET Core applications and ODBC provider for .NET STANDARD builds.
		/// </para>
		/// </remarks>
		public static DataOptions UseSapHana(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.SapHana, connectionString);
		}

		/// <summary>
		/// Configure connection to use native SAP HANA provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSapHanaNative(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(
				SapHanaTools.GetDataProvider(ProviderName.SapHanaNative),
				connectionString);
		}

		/// <summary>
		/// Configure connection to use SAP HANA ODBC provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSapHanaODBC(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(
				SapHanaTools.GetDataProvider(ProviderName.SapHanaOdbc),
				connectionString);
		}

		#endregion

		#region UseSqlCe

		/// <summary>
		/// Configure connection to use SQL CE provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SQL CE connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseSqlCe(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.SqlCe, connectionString);
		}

		#endregion

		#region UseAse

		/// <summary>
		/// Configure connection to use SAP/Sybase ASE default provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
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
		public static DataOptions UseAse(this DataOptions options, string connectionString)
		{
			return options.UseConnectionString(ProviderName.Sybase, connectionString);
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific SAP/Sybase ASE provider and connection string.
		/// </summary>
		/// <param name="options">Instance of <see cref="DataOptions"/>.</param>
		/// <param name="connectionString">SAP/Sybase ASE connection string.</param>
		/// <param name="useNativeProvider">if <c>true</c>, <c>Sybase.AdoNet45.AseClient</c> provider will be used; otherwise managed <c>AdoNetCore.AseClient</c>.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseAse(this DataOptions options, string connectionString, bool useNativeProvider)
		{
			return options.UseConnectionString(
				SybaseTools.GetDataProvider(useNativeProvider ? ProviderName.Sybase : ProviderName.SybaseManaged),
				connectionString);
		}
#endif

		#endregion
	}
}
