using System;

namespace LinqToDB
{
	/// <summary>
	/// Default names for providers.
	/// </summary>
	public static class ProviderName
	{
		/// <summary>
		/// Microsoft Access provider.
		/// Used as configuration name for Access mapping schema <see cref="DataProvider.Access.AccessMappingSchema"/>.
		/// </summary>
		public const string Access        = "Access";
		/// <summary>
		/// IBM DB2 default provider (DB2 LUW).
		/// Used as configuration name for both DB2 base mapping schema <see cref="DataProvider.DB2.DB2MappingSchema"/>.
		/// </summary>
		public const string DB2           = "DB2";
		/// <summary>
		/// IBM DB2 LUW provider.
		/// Used as configuration name for DB2 LUW mapping schema <see cref="DataProvider.DB2.DB2LUWMappingSchema"/>.
		/// </summary>
		public const string DB2LUW        = "DB2.LUW";
		/// <summary>
		/// IBM DB2 for z/OS provider.
		/// Used as configuration name for DB2 z/OS mapping schema <see cref="DataProvider.DB2.DB2zOSMappingSchema"/>.
		/// </summary>
		public const string DB2zOS        = "DB2.z/OS";
		/// <summary>
		/// Firebird provider.
		/// Used as configuration name for Firebird mapping schema <see cref="DataProvider.Firebird.FirebirdMappingSchema"/>.
		/// </summary>
		public const string Firebird      = "Firebird";
		/// <summary>
		/// Informix provider.
		/// Used as configuration name for Informix mapping schema <see cref="DataProvider.Informix.InformixMappingSchema"/>.
		/// </summary>
		public const string Informix      = "Informix";
		/// <summary>
		/// Microsoft SQL Server default provider (SQL Server 2008).
		/// Used as configuration name for SQL Server base mapping schema <see cref="DataProvider.SqlServer.SqlServerMappingSchema"/>.
		/// </summary>
		public const string SqlServer     = "SqlServer";
		/// <summary>
		/// Microsoft SQL Server 2000 provider.
		/// Used as configuration name for SQL Server 2000 mapping schema <see cref="DataProvider.SqlServer.SqlServer2000MappingSchema"/>.
		/// </summary>
		public const string SqlServer2000 = "SqlServer.2000";
		/// <summary>
		/// Microsoft SQL Server 2005 provider.
		/// Used as configuration name for SQL Server 2005 mapping schema <see cref="DataProvider.SqlServer.SqlServer2005MappingSchema"/>.
		/// </summary>
		public const string SqlServer2005 = "SqlServer.2005";
		/// <summary>
		/// Microsoft SQL Server 2008 provider.
		/// Used as configuration name for SQL Server 2008 mapping schema <see cref="DataProvider.SqlServer.SqlServer2008MappingSchema"/>.
		/// </summary>
		public const string SqlServer2008 = "SqlServer.2008";
		/// <summary>
		/// Microsoft SQL Server 2012 provider.
		/// Used as configuration name for SQL Server 2012 mapping schema <see cref="DataProvider.SqlServer.SqlServer2012MappingSchema"/>.
		/// </summary>
		public const string SqlServer2012 = "SqlServer.2012";
		/// <summary>
		/// Microsoft SQL Server 2012 provider.
		/// </summary>
		public const string SqlServer2014 = "SqlServer.2014";
		/// <summary>
		/// MySql provider.
		/// Used as configuration name for MySql mapping schema <see cref="DataProvider.MySql.MySqlMappingSchema"/>.
		/// </summary>
		public const string MySql         = "MySql";
		/// <summary>
		/// Oracle ODP.NET autodetected provider (native or managed).
		/// Used as configuration name for Oracle base mapping schema <see cref="DataProvider.Oracle.OracleMappingSchema"/>.
		/// </summary>
		public const string Oracle        = "Oracle";
		/// <summary>
		/// Oracle ODP.NET native provider.
		/// Used as configuration name for Oracle native provider mapping schema <see cref="DataProvider.Oracle.OracleMappingSchema.NativeMappingSchema"/>.
		/// </summary>
		public const string OracleNative  = "Oracle.Native";
		/// <summary>
		/// Oracle ODP.NET managed provider.
		/// Used as configuration name for Oracle managed provider mapping schema <see cref="DataProvider.Oracle.OracleMappingSchema.ManagedMappingSchema"/>.
		/// </summary>
		public const string OracleManaged = "Oracle.Managed";
		/// <summary>
		/// PostgreSQL 9.2- data provider.
		/// Used as configuration name for PostgreSQL mapping schema <see cref="DataProvider.PostgreSQL.PostgreSQLMappingSchema"/>.
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
		/// Microsoft SQL Server Compact Edition provider.
		/// Used as configuration name for SQL CE mapping schema <see cref="DataProvider.SqlCe.SqlCeMappingSchema"/>.
		/// </summary>
		public const string SqlCe         = "SqlCe";
		/// <summary>
		/// SQLite provider.
		/// Used as configuration name for SQLite mapping schema <see cref="DataProvider.SQLite.SQLiteMappingSchema"/>.
		/// </summary>
		public const string SQLite        = "SQLite";
		/// <summary>
		/// Sybase ASE provider.
		/// Used as configuration name for Sybase ASE mapping schema <see cref="DataProvider.Sybase.SybaseMappingSchema"/>.
		/// </summary>
		public const string Sybase        = "Sybase";
		/// <summary>
		/// SAP HANA provider.
		/// Used as configuration name for SAP HANA mapping schema <see cref="DataProvider.SapHana.SapHanaMappingSchema"/>.
		/// </summary>
		public const string SapHana       = "SapHana";
	}
}
