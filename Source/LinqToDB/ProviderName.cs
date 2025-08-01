using JetBrains.Annotations;

using LinqToDB.Internal.DataProvider.Access;
using LinqToDB.Internal.DataProvider.DB2;
using LinqToDB.Internal.DataProvider.Firebird;
using LinqToDB.Internal.DataProvider.Informix;
using LinqToDB.Internal.DataProvider.MySql;
using LinqToDB.Internal.DataProvider.Oracle;
using LinqToDB.Internal.DataProvider.PostgreSQL;
using LinqToDB.Internal.DataProvider.SapHana;
using LinqToDB.Internal.DataProvider.SqlCe;
using LinqToDB.Internal.DataProvider.SQLite;
using LinqToDB.Internal.DataProvider.SqlServer;
using LinqToDB.Internal.DataProvider.Sybase;

namespace LinqToDB
{
	// TODO: v6: obsolete/remove all provider-specific entries as it should be used for dialects only
	/// <summary>
	/// Default names for providers.
	/// </summary>
	[PublicAPI]
	public static class ProviderName
	{
		/// <summary>
		/// Microsoft Access OleDb provider (with JET or ACE detection).
		/// Used as configuration name for Access mapping schema <see cref="AccessMappingSchema"/>.
		/// </summary>
		public const string Access        = "Access";

		/// <summary>
		/// Microsoft Access ODBC provider (with JET or ACE detection).
		/// Used as configuration name for Access mapping schema <see cref="AccessMappingSchema"/>.
		/// </summary>
		public const string AccessOdbc    = "Access.Odbc";

		/// <summary>
		/// Microsoft Access JET OleDb provider.
		/// Used as configuration name for Access mapping schema <see cref="AccessMappingSchema"/>.
		/// </summary>
		public const string AccessJetOleDb        = "Access.Jet.OleDb";

		/// <summary>
		/// Microsoft Access JET ODBC provider.
		/// Used as configuration name for Access mapping schema <see cref="AccessMappingSchema"/>.
		/// </summary>
		public const string AccessJetOdbc    = "Access.Jet.Odbc";

		/// <summary>
		/// Microsoft Access ACE OleDb provider.
		/// Used as configuration name for Access mapping schema <see cref="AccessMappingSchema"/>.
		/// </summary>
		public const string AccessAceOleDb        = "Access.Ace.OleDb";

		/// <summary>
		/// Microsoft Access ACE ODBC provider.
		/// Used as configuration name for Access mapping schema <see cref="AccessMappingSchema"/>.
		/// </summary>
		public const string AccessAceOdbc    = "Access.Ace.Odbc";

		/// <summary>
		/// IBM DB2 default provider (DB2 LUW).
		/// Used as configuration name for both DB2 base mapping schema <see cref="DB2MappingSchema"/>.
		/// </summary>
		public const string DB2           = "DB2";
		/// <summary>
		/// IBM DB2 LUW provider.
		/// Used as configuration name for DB2 LUW mapping schema <see cref="DB2MappingSchema.DB2LUWMappingSchema"/>.
		/// </summary>
		public const string DB2LUW        = "DB2.LUW";
		/// <summary>
		/// IBM DB2 for z/OS provider.
		/// Used as configuration name for DB2 z/OS mapping schema <see cref="DB2MappingSchema.DB2zOSMappingSchema"/>.
		/// </summary>
		public const string DB2zOS        = "DB2.z/OS";
		/// <summary>
		/// Firebird provider.
		/// Used as configuration name for Firebird mapping schema <see cref="FirebirdMappingSchema"/>.
		/// </summary>
		public const string Firebird      = "Firebird";
		/// <summary>
		/// Firebird 2.5 provider.
		/// Used as configuration name for Firebird mapping schema <see cref="FirebirdMappingSchema.Firebird25MappingSchema"/>.
		/// </summary>
		public const string Firebird25      = "Firebird.2.5";
		/// <summary>
		/// Firebird 3 provider.
		/// Used as configuration name for Firebird mapping schema <see cref="FirebirdMappingSchema.Firebird3MappingSchema"/>.
		/// </summary>
		public const string Firebird3      = "Firebird.3";
		/// <summary>
		/// Firebird 4 provider.
		/// Used as configuration name for Firebird mapping schema <see cref="FirebirdMappingSchema.Firebird4MappingSchema"/>.
		/// </summary>
		public const string Firebird4      = "Firebird.4";
		/// <summary>
		/// Firebird 5 provider.
		/// Used as configuration name for Firebird mapping schema <see cref="FirebirdMappingSchema.Firebird5MappingSchema"/>.
		/// </summary>
		public const string Firebird5      = "Firebird.5";
		/// <summary>
		/// Informix IBM.Data.Informix provider (including IDS provider).
		/// Used as configuration name for Informix mapping schema <see cref="InformixMappingSchema"/>.
		/// </summary>
		public const string Informix      = "Informix";
		/// <summary>
		/// Informix over IBM.Data.DB2 IDS provider.
		/// Used as configuration name for Informix mapping schema <see cref="InformixMappingSchema"/>.
		/// </summary>
		public const string InformixDB2   = "Informix.DB2";
		/// <summary>
		/// Microsoft SQL Server default provider (SQL Server 2008).
		/// Used as configuration name for SQL Server base mapping schema <see cref="SqlServerMappingSchema"/>.
		/// </summary>
		public const string SqlServer     = "SqlServer";
		/// <summary>
		/// Microsoft SQL Server 2005 provider.
		/// Used as configuration name for SQL Server 2005 mapping schema <see cref="SqlServerMappingSchema.SqlServer2005MappingSchema"/>.
		/// </summary>
		public const string SqlServer2005 = "SqlServer.2005";
		/// <summary>
		/// Microsoft SQL Server 2008 provider.
		/// Used as configuration name for SQL Server 2008 mapping schema <see cref="SqlServerMappingSchema.SqlServer2008MappingSchema"/>.
		/// </summary>
		public const string SqlServer2008 = "SqlServer.2008";
		/// <summary>
		/// Microsoft SQL Server 2012 provider.
		/// Used as configuration name for SQL Server 2012 mapping schema <see cref="SqlServerMappingSchema.SqlServer2012MappingSchema"/>.
		/// </summary>
		public const string SqlServer2012 = "SqlServer.2012";
		/// <summary>
		/// Microsoft SQL Server 2012 provider.
		/// </summary>
		public const string SqlServer2014 = "SqlServer.2014";
		/// <summary>
		/// Microsoft SQL Server 2016 provider.
		/// Used as configuration name for SQL Server 2016 mapping schema <see cref="SqlServerMappingSchema.SqlServer2016MappingSchema"/>.
		/// </summary>
		public const string SqlServer2016 = "SqlServer.2016";
		/// <summary>
		/// Microsoft SQL Server 2017 provider.
		/// Used as configuration name for SQL Server 2017 mapping schema <see cref="SqlServerMappingSchema.SqlServer2017MappingSchema"/>.
		/// </summary>
		public const string SqlServer2017 = "SqlServer.2017";
		/// <summary>
		/// Microsoft SQL Server 2019 provider.
		/// Used as configuration name for SQL Server 2019 mapping schema <see cref="SqlServerMappingSchema.SqlServer2019MappingSchema"/>.
		/// </summary>
		public const string SqlServer2019 = "SqlServer.2019";
		/// <summary>
		/// Microsoft SQL Server 2022 provider.
		/// Used as configuration name for SQL Server 2022 mapping schema <see cref="SqlServerMappingSchema.SqlServer2022MappingSchema"/>.
		/// </summary>
		public const string SqlServer2022 = "SqlServer.2022";
		/// <summary>
		/// Microsoft SQL Server 2025 provider.
		/// Used as configuration name for SQL Server 2025 mapping schema <see cref="SqlServerMappingSchema.SqlServer2025MappingSchema"/>.
		/// </summary>
		public const string SqlServer2025 = "SqlServer.2025";
		/// <summary>
		/// MySql provider.
		/// Used as configuration name for MySql mapping schema <see cref="MySqlMappingSchema"/>.
		/// </summary>
		public const string MySql         = "MySql";
		/// <summary>
		/// MySql 5.7.x provider.
		/// Used as configuration name for MySql mapping schema <see cref="MySqlMappingSchema.MySql57MappingSchema"/>.
		/// </summary>
		public const string MySql57 = "MySql.5.7";
		/// <summary>
		/// MySql 8.x provider.
		/// Used as configuration name for MySql mapping schema <see cref="MySqlMappingSchema.MySql80MappingSchema"/>.
		/// </summary>
		public const string MySql80 = "MySql.8.0";
		/// <summary>
		/// MariaDB 10+ provider.
		/// Used as configuration name for MySql mapping schema <see cref="MySqlMappingSchema.MariaDB10MappingSchema"/>.
		/// </summary>
		public const string MariaDB10 = "MariaDB.10";
		/// <summary>
		/// MySql 5.7.x using MySql.Data provider.
		/// Used as configuration name for MySql mapping schema <see cref="MySqlMappingSchema.MySqlData57MappingSchema"/>.
		/// </summary>
		public const string MySql57MySqlData = "MySql.5.7.MySql.Data";
		/// <summary>
		/// MySql 5.7.x using MySqlConnector provider.
		/// Used as configuration name for MySql mapping schema <see cref="MySqlMappingSchema.MySqlConnector57MappingSchema"/>.
		/// </summary>
		public const string MySql57MySqlConnector = "MySql.5.7.MySqlConnector";
		/// <summary>
		/// MySql 8+ using MySql.Data provider.
		/// Used as configuration name for MySql mapping schema <see cref="MySqlMappingSchema.MySqlData80MappingSchema"/>.
		/// </summary>
		public const string MySql80MySqlData = "MySql.8.0.MySql.Data";
		/// <summary>
		/// MySql 8+ using MySqlConnector provider.
		/// Used as configuration name for MySql mapping schema <see cref="MySqlMappingSchema.MySqlConnector80MappingSchema"/>.
		/// </summary>
		public const string MySql80MySqlConnector = "MySql.8.0.MySqlConnector";
		/// <summary>
		/// MariaDB 10+ using MySql.Data provider.
		/// Used as configuration name for MariaDB mapping schema <see cref="MySqlMappingSchema.MySqlDataMariaDB10MappingSchema"/>.
		/// </summary>
		public const string MariaDB10MySqlData = "MariaDB.10.MySql.Data";
		/// <summary>
		/// MariaDB 10+ using MySqlConnector provider.
		/// Used as configuration name for MariaDB mapping schema <see cref="MySqlMappingSchema.MySqlConnectorMariaDB10MappingSchema"/>.
		/// </summary>
		public const string MariaDB10MySqlConnector = "MariaDB.10.MySqlConnector";

		/// <summary>
		/// Oracle ODP.NET autodetected provider (native or managed).
		/// Used as configuration name for Oracle base mapping schema <see cref="OracleMappingSchema"/>.
		/// </summary>
		public const string Oracle        = "Oracle";
		/// <summary>
		/// Oracle (11g dialect) ODP.NET native provider.
		/// Used as configuration name for Oracle native provider mapping schema <see cref="OracleMappingSchema.Native11MappingSchema"/>.
		/// </summary>
		public const string Oracle11Native = "Oracle.11.Native";
		/// <summary>
		/// Oracle (11g dialect) Devart provider.
		/// Used as configuration name for Oracle managed provider mapping schema <see cref="OracleMappingSchema.Devart11MappingSchema"/>.
		/// </summary>
		public const string Oracle11Devart = "Oracle.11.Devart";
		/// <summary>
		/// Oracle (11g dialect) ODP.NET managed provider.
		/// Used as configuration name for Oracle managed provider mapping schema <see cref="OracleMappingSchema.Managed11MappingSchema"/>.
		/// </summary>
		public const string Oracle11Managed = "Oracle.11.Managed";
		/// <summary>
		/// Oracle ODP.NET native provider.
		/// Used as configuration name for Oracle native provider mapping schema <see cref="OracleMappingSchema.NativeMappingSchema"/>.
		/// </summary>
		public const string OracleNative  = "Oracle.Native";
		/// <summary>
		/// Oracle ODP.NET managed provider.
		/// Used as configuration name for Oracle managed provider mapping schema <see cref="OracleMappingSchema.ManagedMappingSchema"/>.
		/// </summary>
		public const string OracleManaged = "Oracle.Managed";
		/// <summary>
		/// Oracle Devart provider.
		/// Used as configuration name for Oracle managed provider mapping schema <see cref="OracleMappingSchema.DevartMappingSchema"/>.
		/// </summary>
		public const string OracleDevart = "Oracle.Devart";
		/// <summary>
		/// PostgreSQL 9.2- data provider.
		/// Used as configuration name for PostgreSQL mapping schema <see cref="PostgreSQLMappingSchema"/>.
		/// </summary>
		public const string PostgreSQL    = "PostgreSQL";
		/// <summary>
		/// PostgreSQL 9.2- data provider.
		/// </summary>
		public const string PostgreSQL92  = "PostgreSQL.9.2";
		/// <summary>
		/// PostgreSQL 9.3+ data provider.
		/// </summary>
		public const string PostgreSQL93  = "PostgreSQL.9.3";
		/// <summary>
		/// PostgreSQL 9.5+ data provider.
		/// </summary>
		public const string PostgreSQL95  = "PostgreSQL.9.5";
		/// <summary>
		/// PostgreSQL 15+ data provider.
		/// </summary>
		public const string PostgreSQL15 = "PostgreSQL.15";
		/// <summary>
		/// PostgreSQL 18+ data provider.
		/// </summary>
		public const string PostgreSQL18 = "PostgreSQL.18";
		/// <summary>
		/// Microsoft SQL Server Compact Edition provider.
		/// Used as configuration name for SQL CE mapping schema <see cref="SqlCeMappingSchema"/>.
		/// </summary>
		public const string SqlCe         = "SqlCe";
		/// <summary>
		/// SQLite provider.
		/// Used as configuration name for SQLite mapping schema <see cref="SQLiteMappingSchema"/>.
		/// </summary>
		public const string SQLite        = "SQLite";
		/// <summary>
		/// System.Data.Sqlite provider.
		/// </summary>
		public const string SQLiteClassic = "SQLite.Classic";
		/// <summary>
		/// Microsoft.Data.Sqlite provider.
		/// </summary>
		public const string SQLiteMS      = "SQLite.MS";
		/// <summary>
		/// Native SAP/Sybase ASE provider.
		/// Used as configuration name for Sybase ASE mapping schema <see cref="SybaseMappingSchema.NativeMappingSchema"/>.
		/// </summary>
		public const string Sybase        = "Sybase";
		/// <summary>
		/// Managed Sybase/SAP ASE provider from <a href="https://github.com/DataAction/AdoNetCore.AseClient">DataAction</a>.
		/// Used as configuration name for Sybase ASE mapping schema <see cref="SybaseMappingSchema.ManagedMappingSchema"/>.
		/// </summary>
		public const string SybaseManaged = "Sybase.Managed";
		/// <summary>
		/// SAP HANA provider.
		/// Used as configuration name for SAP HANA mapping schema <see cref="SapHanaMappingSchema"/>.
		/// </summary>
		public const string SapHana       = "SapHana";
		/// <summary>
		/// SAP HANA provider.
		/// Used as configuration name for SAP HANA mapping schema <see cref="SapHanaMappingSchema.NativeMappingSchema"/>.
		/// </summary>
		public const string SapHanaNative = "SapHana.Native";
		/// <summary>
		/// SAP HANA ODBC provider.
		/// Used as configuration name for SAP HANA mapping schema <see cref="SapHanaMappingSchema.OdbcMappingSchema"/>.
		/// </summary>
		public const string SapHanaOdbc   = "SapHana.Odbc";
		/// <summary>
		/// ClickHouse provider base name.
		/// </summary>
		public const string ClickHouse        = "ClickHouse";
		/// <summary>
		/// ClickHouse provider using Octonica.ClickHouseClient ADO.NET provider.
		/// </summary>
		public const string ClickHouseOctonica = "ClickHouse.Octonica";
		/// <summary>
		/// ClickHouse provider using ClickHouse.Client ADO.NET provider.
		/// </summary>
		public const string ClickHouseClient   = "ClickHouse.Client";
		/// <summary>
		/// ClickHouse provider using MySqlConnector ADO.NET provider.
		/// </summary>
		public const string ClickHouseMySql   = "ClickHouse.MySql";
		/// <summary>
		/// YDB provider.
		/// </summary>
		public const string Ydb        = "YDB";
	}
}
