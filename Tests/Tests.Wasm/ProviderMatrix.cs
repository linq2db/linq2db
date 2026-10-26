using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.DuckDB;
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
using LinqToDB.DataProvider.Ydb;

namespace Tests.Wasm
{
	public enum MergeOutcome
	{
		Generates,
		Throws,
	}

	[Flags]
	public enum RunnerOS
	{
		None    = 0,
		Windows = 1,
		Linux   = 2,
		Any     = Windows | Linux,
	}

	/// <summary>
	/// One remote configuration the WASM client is run against. <see cref="Configuration"/> is what the client sends;
	/// the server maps it to <see cref="CreateProvider"/> explicitly, so no provider detection (and no connection) is involved.
	/// </summary>
	public sealed record MatrixRow(
		string              Configuration,
		string              ProviderName,
		Func<IDataProvider> CreateProvider,
		MergeOutcome        Merge,
		bool                LoadsProviderAssembly = false,
		RunnerOS            UnavailableOn         = RunnerOS.None,
		string?             UnavailableReason     = null)
	{
		public bool IsAvailable => (UnavailableOn & ProviderMatrix.CurrentOS) == 0;

		public override string ToString() => Configuration;
	}

	public static class ProviderMatrix
	{
		public const string RoundTripConfiguration = ProviderName.SQLiteMS;

		public const string SqliteConnectionString = "Data Source=linq2db-wasm-runtime-tests;Mode=Memory;Cache=Shared";

		// GetInfo builds a DataConnection without opening it, so every other row only needs a string to carry.
		public const string DummyConnectionString  = "Data Source=linq2db-wasm-runtime-tests";

		public static RunnerOS CurrentOS =>
			OperatingSystem.IsWindows() ? RunnerOS.Windows :
			OperatingSystem.IsLinux()   ? RunnerOS.Linux   :
			RunnerOS.None;

		// The builders of these families throw "doesn't support SQL MERGE statement" while generating SQL client-side:
		// AccessSqlBuilderBase, ClickHouseSqlBuilder, MySqlSqlBuilder, SQLiteSqlBuilder, SqlCeSqlBuilder, YdbSqlBuilder.
		const MergeOutcome Refused = MergeOutcome.Throws;
		const MergeOutcome Built   = MergeOutcome.Generates;

		const string NotOnNet   = "assembly is referenced for net462 only (Redist), so the provider cannot be constructed on .NET";
		const string NotShipped = "assembly is not referenced by any test project, so the provider cannot be constructed";

		public static readonly IReadOnlyList<MatrixRow> Rows =
		[
			new(ProviderName.AccessJetOleDb,          ProviderName.AccessJetOleDb,          () => AccessTools.GetDataProvider(AccessVersion.Jet, AccessProvider.OleDb), Refused),
			new(ProviderName.AccessJetOdbc,           ProviderName.AccessJetOdbc,           () => AccessTools.GetDataProvider(AccessVersion.Jet, AccessProvider.ODBC),  Refused),
			new(ProviderName.AccessAceOleDb,          ProviderName.AccessAceOleDb,          () => AccessTools.GetDataProvider(AccessVersion.Ace, AccessProvider.OleDb), Refused),
			new(ProviderName.AccessAceOdbc,           ProviderName.AccessAceOdbc,           () => AccessTools.GetDataProvider(AccessVersion.Ace, AccessProvider.ODBC),  Refused),

			new(ProviderName.DB2LUW,                  ProviderName.DB2LUW,                  () => DB2Tools.GetDataProvider(DB2Version.LUW), Built, LoadsProviderAssembly: true),
			// The client puts the configuration name into the request path unescaped, so the row is keyed without '/'.
			new("DB2.zOS",                            ProviderName.DB2zOS,                  () => DB2Tools.GetDataProvider(DB2Version.zOS), Built, LoadsProviderAssembly: true),

			new(ProviderName.Firebird25,              ProviderName.Firebird25,              () => FirebirdTools.GetDataProvider(FirebirdVersion.v25), Built),
			new(ProviderName.Firebird3,               ProviderName.Firebird3,               () => FirebirdTools.GetDataProvider(FirebirdVersion.v3),  Built),
			new(ProviderName.Firebird4,               ProviderName.Firebird4,               () => FirebirdTools.GetDataProvider(FirebirdVersion.v4),  Built),
			new(ProviderName.Firebird5,               ProviderName.Firebird5,               () => FirebirdTools.GetDataProvider(FirebirdVersion.v5),  Built),

			new(ProviderName.Informix,                ProviderName.Informix,                () => InformixTools.GetDataProvider(InformixProvider.Informix), Built, UnavailableOn: RunnerOS.Any, UnavailableReason: "IBM.Data.Informix " + NotOnNet),
			new(ProviderName.InformixDB2,             ProviderName.InformixDB2,             () => InformixTools.GetDataProvider(InformixProvider.DB2),      Built, LoadsProviderAssembly: true),

			new(ProviderName.SqlServer2005,           ProviderName.SqlServer2005,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2005, SqlServerProvider.SystemDataSqlClient),    Built),
			new(ProviderName.SqlServer2005 + ".MS",   ProviderName.SqlServer2005,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2005, SqlServerProvider.MicrosoftDataSqlClient), Built),
			new(ProviderName.SqlServer2008,           ProviderName.SqlServer2008,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2008, SqlServerProvider.SystemDataSqlClient),    Built),
			new(ProviderName.SqlServer2008 + ".MS",   ProviderName.SqlServer2008,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient), Built),
			new(ProviderName.SqlServer2012,           ProviderName.SqlServer2012,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient),    Built),
			new(ProviderName.SqlServer2012 + ".MS",   ProviderName.SqlServer2012,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient), Built),
			new(ProviderName.SqlServer2014,           ProviderName.SqlServer2014,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2014, SqlServerProvider.SystemDataSqlClient),    Built),
			new(ProviderName.SqlServer2014 + ".MS",   ProviderName.SqlServer2014,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2014, SqlServerProvider.MicrosoftDataSqlClient), Built),
			new(ProviderName.SqlServer2016,           ProviderName.SqlServer2016,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2016, SqlServerProvider.SystemDataSqlClient),    Built),
			new(ProviderName.SqlServer2016 + ".MS",   ProviderName.SqlServer2016,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2016, SqlServerProvider.MicrosoftDataSqlClient), Built),
			new(ProviderName.SqlServer2017,           ProviderName.SqlServer2017,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2017, SqlServerProvider.SystemDataSqlClient),    Built),
			new(ProviderName.SqlServer2017 + ".MS",   ProviderName.SqlServer2017,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2017, SqlServerProvider.MicrosoftDataSqlClient), Built),
			new(ProviderName.SqlServer2019,           ProviderName.SqlServer2019,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2019, SqlServerProvider.SystemDataSqlClient),    Built),
			new(ProviderName.SqlServer2019 + ".MS",   ProviderName.SqlServer2019,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2019, SqlServerProvider.MicrosoftDataSqlClient), Built),
			new(ProviderName.SqlServer2022,           ProviderName.SqlServer2022,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2022, SqlServerProvider.SystemDataSqlClient),    Built),
			new(ProviderName.SqlServer2022 + ".MS",   ProviderName.SqlServer2022,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient), Built),
			new(ProviderName.SqlServer2025,           ProviderName.SqlServer2025,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2025, SqlServerProvider.SystemDataSqlClient),    Built),
			new(ProviderName.SqlServer2025 + ".MS",   ProviderName.SqlServer2025,           () => SqlServerTools.GetDataProvider(SqlServerVersion.v2025, SqlServerProvider.MicrosoftDataSqlClient), Built),

			new(ProviderName.MySql57MySqlData,        ProviderName.MySql57MySqlData,        () => MySqlTools.GetDataProvider(MySqlVersion.MySql57,   MySqlProvider.MySqlData),      Refused, LoadsProviderAssembly: true),
			new(ProviderName.MySql57MySqlConnector,   ProviderName.MySql57MySqlConnector,   () => MySqlTools.GetDataProvider(MySqlVersion.MySql57,   MySqlProvider.MySqlConnector), Refused, LoadsProviderAssembly: true),
			new(ProviderName.MySql80MySqlData,        ProviderName.MySql80MySqlData,        () => MySqlTools.GetDataProvider(MySqlVersion.MySql80,   MySqlProvider.MySqlData),      Refused, LoadsProviderAssembly: true),
			new(ProviderName.MySql80MySqlConnector,   ProviderName.MySql80MySqlConnector,   () => MySqlTools.GetDataProvider(MySqlVersion.MySql80,   MySqlProvider.MySqlConnector), Refused, LoadsProviderAssembly: true),
			new(ProviderName.MariaDB10MySqlData,      ProviderName.MariaDB10MySqlData,      () => MySqlTools.GetDataProvider(MySqlVersion.MariaDB10, MySqlProvider.MySqlData),      Refused, LoadsProviderAssembly: true),
			new(ProviderName.MariaDB10MySqlConnector, ProviderName.MariaDB10MySqlConnector, () => MySqlTools.GetDataProvider(MySqlVersion.MariaDB10, MySqlProvider.MySqlConnector), Refused, LoadsProviderAssembly: true),

			new(ProviderName.Oracle11Native,          ProviderName.Oracle11Native,          () => OracleTools.GetDataProvider(OracleVersion.v11, OracleProvider.Native),  Built, UnavailableOn: RunnerOS.Any, UnavailableReason: "Oracle.DataAccess " + NotOnNet),
			new(ProviderName.Oracle11Managed,         ProviderName.Oracle11Managed,         () => OracleTools.GetDataProvider(OracleVersion.v11, OracleProvider.Managed), Built),
			new(ProviderName.Oracle11Devart,          ProviderName.Oracle11Devart,          () => OracleTools.GetDataProvider(OracleVersion.v11, OracleProvider.Devart),  Built),
			new(ProviderName.OracleNative,            ProviderName.OracleNative,            () => OracleTools.GetDataProvider(OracleVersion.v12, OracleProvider.Native),  Built, UnavailableOn: RunnerOS.Any, UnavailableReason: "Oracle.DataAccess " + NotOnNet),
			new(ProviderName.OracleManaged,           ProviderName.OracleManaged,           () => OracleTools.GetDataProvider(OracleVersion.v12, OracleProvider.Managed), Built),
			new(ProviderName.OracleDevart,            ProviderName.OracleDevart,            () => OracleTools.GetDataProvider(OracleVersion.v12, OracleProvider.Devart),  Built),

			new(ProviderName.PostgreSQL92,            ProviderName.PostgreSQL92,            () => PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v92), Built, LoadsProviderAssembly: true),
			new(ProviderName.PostgreSQL93,            ProviderName.PostgreSQL93,            () => PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v93), Built, LoadsProviderAssembly: true),
			new(ProviderName.PostgreSQL95,            ProviderName.PostgreSQL95,            () => PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95), Built, LoadsProviderAssembly: true),
			new(ProviderName.PostgreSQL11,            ProviderName.PostgreSQL11,            () => PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v11), Built, LoadsProviderAssembly: true),
			new(ProviderName.PostgreSQL12,            ProviderName.PostgreSQL12,            () => PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v12), Built, LoadsProviderAssembly: true),
			new(ProviderName.PostgreSQL13,            ProviderName.PostgreSQL13,            () => PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v13), Built, LoadsProviderAssembly: true),
			new(ProviderName.PostgreSQL15,            ProviderName.PostgreSQL15,            () => PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v15), Built, LoadsProviderAssembly: true),
			new(ProviderName.PostgreSQL18,            ProviderName.PostgreSQL18,            () => PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v18), Built, LoadsProviderAssembly: true),
			new(ProviderName.PostgreSQL19,            ProviderName.PostgreSQL19,            () => PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v19), Built, LoadsProviderAssembly: true),

			new(ProviderName.SqlCe,                   ProviderName.SqlCe,                   SqlCeTools.GetDataProvider,         Refused, UnavailableOn: RunnerOS.Any, UnavailableReason: "System.Data.SqlServerCe is not a package; the tests register it from a local install"),

			new(ProviderName.SQLiteClassic,           ProviderName.SQLiteClassic,           () => SQLiteTools.GetDataProvider(SQLiteProvider.System),    Refused),
			new(ProviderName.SQLiteMS,                ProviderName.SQLiteMS,                () => SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft), Refused),

			new(ProviderName.Sybase,                  ProviderName.Sybase,                  () => SybaseTools.GetDataProvider(SybaseProvider.Unmanaged),  Built, UnavailableOn: RunnerOS.Any, UnavailableReason: "Sybase.AdoNet45.AseClient " + NotShipped),
			new(ProviderName.SybaseManaged,           ProviderName.SybaseManaged,           () => SybaseTools.GetDataProvider(SybaseProvider.DataAction), Built),

			new(ProviderName.SapHanaNative,           ProviderName.SapHanaNative,           () => SapHanaTools.GetDataProvider(SapHanaProvider.Unmanaged), Built, UnavailableOn: RunnerOS.Any, UnavailableReason: "Sap.Data.Hana is a Redist assembly referenced by Tests/Linq only"),
			new(ProviderName.SapHanaOdbc,             ProviderName.SapHanaOdbc,             () => SapHanaTools.GetDataProvider(SapHanaProvider.ODBC),      Built),

			new(ProviderName.ClickHouseOctonica,      ProviderName.ClickHouseOctonica,      () => ClickHouseTools.GetDataProvider(ClickHouseProvider.Octonica),         Refused),
			new(ProviderName.ClickHouseDriver,        ProviderName.ClickHouseDriver,        () => ClickHouseTools.GetDataProvider(ClickHouseProvider.ClickHouseDriver), Refused, LoadsProviderAssembly: true),
			new(ProviderName.ClickHouseMySql,         ProviderName.ClickHouseMySql,         () => ClickHouseTools.GetDataProvider(ClickHouseProvider.MySqlConnector),   Refused),

			new(ProviderName.Ydb,                     ProviderName.Ydb,                     () => YdbTools.GetDataProvider(),    Refused),
			new(ProviderName.DuckDB,                  ProviderName.DuckDB,                  () => DuckDBTools.GetDataProvider(), Built, LoadsProviderAssembly: true),
		];

		/// <summary>
		/// <see cref="LinqToDB.ProviderName"/> constants that name a provider family or an alias rather than one provider:
		/// resolving them needs detection, which opens a connection or depends on the configuration name.
		/// </summary>
		public static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			[ProviderName.Access]     = "family: Jet / ACE and OleDb / ODBC are detected, by opening a connection",
			[ProviderName.AccessOdbc] = "family: Jet / ACE over ODBC is detected, by opening a connection",
			[ProviderName.DB2]        = "family: LUW / z/OS are detected, by opening a connection",
			[ProviderName.Firebird]   = "family: the server version is detected, by opening a connection",
			[ProviderName.SqlServer]  = "family: the server version is detected, by opening a connection",
			[ProviderName.MySql]      = "family: the server version is detected, by opening a connection",
			[ProviderName.MySql57]    = "version without a client library; rows name MySql.Data and MySqlConnector explicitly",
			[ProviderName.MySql80]    = "version without a client library; rows name MySql.Data and MySqlConnector explicitly",
			[ProviderName.MariaDB10]  = "version without a client library; rows name MySql.Data and MySqlConnector explicitly",
			[ProviderName.Oracle]     = "family: the server version is detected, by opening a connection",
			[ProviderName.PostgreSQL] = "family: the server version is detected, by opening a connection",
			[ProviderName.SQLite]     = "family: the client library is chosen by assembly presence",
			[ProviderName.SapHana]    = "family: the client library is chosen by assembly presence",
			[ProviderName.ClickHouse] = "family: no provider resolves for the bare name",
		};

		public static MatrixRow? Find(string configuration) => Rows.FirstOrDefault(r => r.Configuration == configuration);
	}
}
