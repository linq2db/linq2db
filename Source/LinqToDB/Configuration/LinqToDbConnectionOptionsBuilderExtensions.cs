namespace LinqToDB.Configuration
{
	using LinqToDB.DataProvider.Access;
	using LinqToDB.DataProvider.DB2;
	using LinqToDB.DataProvider.Informix;
	using LinqToDB.DataProvider.Oracle;
	using LinqToDB.DataProvider.PostgreSQL;
	using LinqToDB.DataProvider.SapHana;
	using LinqToDB.DataProvider.SqlServer;
	using LinqToDB.DataProvider.Sybase;

	/// <summary>
	/// Set of provider-specific extensions for <see cref="LinqToDbConnectionOptionsBuilder"/>.
	/// </summary>
	public static class LinqToDbConnectionOptionsBuilderExtensions
	{
		#region UseSqlServer
		/// <summary>
		/// Configure connection to use SQL Server default provider, dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
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
		/// For more fine-grained configuration see <see cref="UseSqlServer(LinqToDbConnectionOptionsBuilder, string, SqlServerProvider, SqlServerVersion)"/> overload.
		/// </remarks>
		public static LinqToDbConnectionOptionsBuilder UseSqlServer(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.SqlServer, connectionString);
		}

		/// <summary>
		/// Configure connection to use specific SQL Server provider, dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">SQL Server connection string.</param>
		/// <param name="provider">SQL Server provider to use.</param>
		/// <param name="dialect">SQL Server dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseSqlServer(this LinqToDbConnectionOptionsBuilder builder, string connectionString, SqlServerProvider provider, SqlServerVersion dialect)
		{
			return builder.UseConnectionString(SqlServerTools.GetDataProvider(dialect, provider), connectionString);
		}
		#endregion

		#region UseOracle
		/// <summary>
		/// Configure connection to use Oracle default provider, dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
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
		/// For more fine-grained configuration see <see cref="UseOracle(LinqToDbConnectionOptionsBuilder, string, OracleVersion)"/> overload.
		/// </remarks>
		public static LinqToDbConnectionOptionsBuilder UseOracle(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.Oracle, connectionString);
		}

		/// <summary>
		/// Configure connection to use Oracle default provider, specific dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default Linq To DB tries to load managed version of Oracle provider.
		/// </para>
		/// </remarks>
		public static LinqToDbConnectionOptionsBuilder UseOracle(this LinqToDbConnectionOptionsBuilder builder, string connectionString, OracleVersion dialect)
		{
			return builder.UseConnectionString(OracleTools.GetDataProvider(ProviderName.Oracle, null, dialect), connectionString);
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific Oracle provider, dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="dialect">Oracle dialect support level.</param>
		/// <param name="useNativeProvider">if <c>true</c>, <c>Oracle.DataAccess</c> provider will be used; othwerwise managed <c>Oracle.ManagedDataAccess</c>.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseOracle(this LinqToDbConnectionOptionsBuilder builder, string connectionString, OracleVersion dialect, bool useNativeProvider)
		{
			return builder.UseConnectionString(
				OracleTools.GetDataProvider(
					useNativeProvider ? ProviderName.OracleNative : ProviderName.OracleManaged,
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
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
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
		/// For more fine-grained configuration see <see cref="UsePostgreSQL(LinqToDbConnectionOptionsBuilder, string, PostgreSQLVersion)"/> overload.
		/// </remarks>
		public static LinqToDbConnectionOptionsBuilder UsePostgreSQL(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.PostgreSQL, connectionString);
		}

		/// <summary>
		/// Configure connection to use PostgreSQL Npgsql provider, specific dialect and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">PostgreSQL connection string.</param>
		/// <param name="dialect">POstgreSQL dialect support level.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UsePostgreSQL(this LinqToDbConnectionOptionsBuilder builder, string connectionString, PostgreSQLVersion dialect)
		{
			return builder.UseConnectionString(PostgreSQLTools.GetDataProvider(dialect), connectionString);
		}
		#endregion

		#region UseMySql
		/// <summary>
		/// Configure connection to use MySql default provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be choosen by probing current folder for provider assembly and if it is not found, default to <c>MySql.Data</c> provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseMySqlData(LinqToDbConnectionOptionsBuilder, string)"/> and <see cref="UseMySqlConnector(LinqToDbConnectionOptionsBuilder, string)"/> methods.
		/// </remarks>
		public static LinqToDbConnectionOptionsBuilder UseMySql(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.MySql, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>MySql.Data</c> MySql provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseMySqlData(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.MySqlOfficial, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>MySqlConnector</c> MySql provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">MySql connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseMySqlConnector(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.MySqlConnector, connectionString);
		}
		#endregion

		#region UseSQLite
		/// <summary>
		/// Configure connection to use SQLite default provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be choosen by probing current folder for provider assembly and if it is not found, default to <c>System.Data.Sqlite</c> provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSQLiteOfficial(LinqToDbConnectionOptionsBuilder, string)"/> and <see cref="UseSQLiteMicrosoft(LinqToDbConnectionOptionsBuilder, string)"/> methods.
		/// </remarks>
		public static LinqToDbConnectionOptionsBuilder UseSQLite(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.SQLite, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>System.Data.Sqlite</c> SQLite provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseSQLiteOfficial(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.SQLiteClassic, connectionString);
		}

		/// <summary>
		/// Configure connection to use <c>Microsoft.Data.Sqlite</c> SQLite provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">SQLite connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseSQLiteMicrosoft(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.SQLiteMS, connectionString);
		}
		#endregion

		#region UseAccess
		/// <summary>
		/// Configure connection to use Access default provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider determined by inspecting connection string for OleDb or ODBC-specific markers and otherwise defaults to OleDb provider.
		/// </para>
		/// For more fine-grained configuration see <see cref="UseAccessOleDb(LinqToDbConnectionOptionsBuilder, string)"/> and <see cref="UseAccessODBC(LinqToDbConnectionOptionsBuilder, string)"/> methods.
		/// </remarks>
		public static LinqToDbConnectionOptionsBuilder UseAccess(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.Access, connectionString);
		}

		/// <summary>
		/// Configure connection to use Access OleDb provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseAccessOleDb(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(AccessTools.GetDataProvider(null), connectionString);
		}

		/// <summary>
		/// Configure connection to use Access ODBC provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseAccessODBC(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.AccessOdbc, connectionString);
		}
		#endregion

		#region UseDB2
		/// <summary>
		/// Configure connection to use DB2 default provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
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
		/// For more fine-grained configuration see <see cref="UseDB2(LinqToDbConnectionOptionsBuilder, string, DB2Version)"/> overload.
		/// </remarks>
		public static LinqToDbConnectionOptionsBuilder UseDB2(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.DB2, connectionString);
		}

		/// <summary>
		/// Configure connection to use specific DB2 provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">DB2 connection string.</param>
		/// <param name="version">DB2 server version.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseDB2(this LinqToDbConnectionOptionsBuilder builder, string connectionString, DB2Version version)
		{
			return builder.UseConnectionString(DB2Tools.GetDataProvider(version), connectionString);
		}
		#endregion

		#region UseFirebird
		/// <summary>
		/// Configure connection to use Firebird provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">Firebird connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseFirebird(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.Firebird, connectionString);
		}
		#endregion

		#region UseInformix
		/// <summary>
		/// Configure connection to use Informix default provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">Informix connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be choosen by probing current folder for provider assembly and if it is not found, default to <c>IBM.Data.DB2</c> provider.
		/// This is not applicable to .NET Core applications as they always use <c>IBM.Data.DB2</c> provider.
		/// </para>
		/// </remarks>
		public static LinqToDbConnectionOptionsBuilder UseInformix(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.Informix, connectionString);
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific Informix provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">Informix connection string.</param>
		/// <param name="useDB2Provider">if <c>true</c>, <c>IBM.Data.DB2</c> provider will be used; othwerwise <c>IBM.Data.Informix</c>.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseInformix(this LinqToDbConnectionOptionsBuilder builder, string connectionString, bool useDB2Provider)
		{
			return builder.UseConnectionString(
				InformixTools.GetDataProvider(useDB2Provider ? ProviderName.InformixDB2 : ProviderName.Informix),
				connectionString);
		}
#endif
		#endregion

		#region UseSapHana
		/// <summary>
		/// Configure connection to use SAP HANA default provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider will be <c>Sap.Data.Hana</c> native provider for .NET Framework and .NET Core applications and ODBC provider for .NET STANDARD builds.
		/// </para>
		/// </remarks>
		public static LinqToDbConnectionOptionsBuilder UseSapHana(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.SapHana, connectionString);
		}

#if NETFRAMEWORK || NETCOREAPP
		/// <summary>
		/// Configure connection to use native SAP HANA provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseSapHanaNative(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(
				SapHanaTools.GetDataProvider(ProviderName.SapHanaNative),
				connectionString);
		}
#endif

		/// <summary>
		/// Configure connection to use SAP HANA ODBC provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">SAP HANA connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseSapHanaODBC(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(
				SapHanaTools.GetDataProvider(ProviderName.SapHanaOdbc),
				connectionString);
		}
		#endregion

		#region UseSqlCe
		/// <summary>
		/// Configure connection to use SQL CE provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">SQL CE connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseSqlCe(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.SqlCe, connectionString);
		}
		#endregion

		#region UseAse
		/// <summary>
		/// Configure connection to use SAP/Sybase ASE default provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
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
		public static LinqToDbConnectionOptionsBuilder UseAse(this LinqToDbConnectionOptionsBuilder builder, string connectionString)
		{
			return builder.UseConnectionString(ProviderName.Sybase, connectionString);
		}

#if NETFRAMEWORK
		/// <summary>
		/// Configure connection to use specific SAP/Sybase ASE provider and connection string.
		/// </summary>
		/// <param name="builder">Instance of <see cref="LinqToDbConnectionOptionsBuilder"/>.</param>
		/// <param name="connectionString">SAP/Sybase ASE connection string.</param>
		/// <param name="useNativeProvider">if <c>true</c>, <c>Sybase.AdoNet45.AseClient</c> provider will be used; othwerwise managed <c>AdoNetCore.AseClient</c>.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseAse(this LinqToDbConnectionOptionsBuilder builder, string connectionString, bool useNativeProvider)
		{
			return builder.UseConnectionString(
				SybaseTools.GetDataProvider(useNativeProvider ? ProviderName.Sybase : ProviderName.SybaseManaged),
				connectionString);
		}
#endif
		#endregion
	}
}
