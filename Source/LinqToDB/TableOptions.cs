using System;

using JetBrains.Annotations;

namespace LinqToDB
{
	/// <summary>
	/// Flags that control table source semantics (e.g., temporary table kind)
	/// and CREATE/DROP behavior when supported by the provider.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="TableOptions"/> describes how a mapped table-like source should be
	/// treated during SQL generation.
	/// </para>
	/// <para>
	/// This includes:
	/// </para>
	/// <list type="bullet">
	///   <item>
	///     <description>
	///       DDL behavior for CREATE/DROP when the target exists or does not exist
	///       (when supported by the provider).
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///       Temporary table kind and visibility semantics (structure and/or data scope),
	///       when supported.
	///     </description>
	///   </item>
	/// </list>
	/// <para>
	/// These flags are interpreted by the configured provider and affect SQL shape
	/// and DDL behavior at translation time.
	/// </para>
	/// <para>
	/// Note on temporary semantics:
	/// The terms "local" and "global" represent a cross-provider abstraction.
	/// Actual visibility of table structure and data depends on the database engine.
	/// For example, in Oracle a "GLOBAL TEMPORARY TABLE" has globally visible structure,
	/// while its data remains session- or transaction-scoped.
	/// </para>
	/// <para>
	/// Provider support lists ("Supported by: ...") are informational and may vary
	/// depending on provider version and database capabilities.
	/// </para>
	/// <para>
	/// Prefer <see cref="None"/> to explicitly disable option-driven behavior.
	/// Use <see cref="NotSet"/> to leave the effective behavior to defaults
	/// (provider, mapping, or API-level configuration).
	/// </para>
	/// </remarks>
	[Flags]
	[PublicAPI]
	public enum TableOptions
	{
		/// <summary>
		/// No option value specified.
		/// </summary>
		/// <remarks>
		/// Means "unspecified": the effective behavior is determined by defaults
		/// (provider/mapping/API) for the specific operation.
		/// </remarks>
		NotSet = 0b000000000,

		/// <summary>
		/// Explicitly specifies that no special table options apply.
		/// </summary>
		/// <remarks>
		/// Means "specified as none": disables option-driven table behavior.
		/// The effective SQL is still provider- and mapping-dependent, but without
		/// additional behavior implied by <see cref="TableOptions"/> flags.
		/// </remarks>
		None = 0b000000001,

		/// <summary>
		/// Use <c>CREATE ... IF NOT EXISTS</c> when supported by the provider.
		/// </summary>
		/// <remarks>
		/// Supported by: DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SQL Server, Sybase ASE.
		/// </remarks>
		CreateIfNotExists = 0b000000010,

		/// <summary>
		/// Use <c>DROP ... IF EXISTS</c> when supported by the provider.
		/// </summary>
		/// <remarks>
		/// Supported by: DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SQL Server, Sybase ASE.
		/// </remarks>
		DropIfExists = 0b000000100,

		/// <summary>
		/// Source is a temporary table (not visible to other sessions) when supported.
		/// </summary>
		/// <remarks>
		/// If a provider supports both global and local temporary tables, a local temporary table is used.
		/// Supported by: DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SQL Server, SAP Hana, Sybase ASE.
		/// </remarks>
		IsTemporary = 0b000001000,

		/// <summary>
		/// Temporary table with session-scoped structure (DDL-level visibility limited to the session),
		/// when supported by the provider.
		/// </summary>
		/// <remarks>
		/// Structure visibility does not define data visibility semantics.
		/// Supported by: DB2, Informix, MySql, PostgreSQL, SQLite, SAP Hana, SQL Server, Sybase ASE.
		/// </remarks>
		IsLocalTemporaryStructure = 0b000010000,

		/// <summary>
		/// Temporary table with globally visible structure (DDL-level visibility),
		/// when supported by the provider.
		/// </summary>
		/// <remarks>
		/// Structure visibility does not imply shared data visibility.
		/// Actual data scope is provider-specific.
		/// Supported by: DB2, Firebird, Oracle, SAP Hana, SQL Server, Sybase ASE.
		/// </remarks>
		IsGlobalTemporaryStructure = 0b000100000,

		/// <summary>
		/// Temporary table with session-scoped data visibility,
		/// when supported by the provider.
		/// </summary>
		/// <remarks>
		/// Data is not visible to other sessions.
		/// Supported by: DB2, Informix, MySql, PostgreSQL, SQLite, SAP Hana, SQL Server, Sybase ASE.
		/// </remarks>
		IsLocalTemporaryData = 0b001000000,

		/// <summary>
		/// Temporary table with data visibility semantics defined as "global"
		/// in the provider abstraction.
		/// </summary>
		/// <remarks>
		/// The exact meaning of "global" data visibility is provider-specific.
		/// In some databases (e.g., Oracle), data in global temporary tables
		/// remains session- or transaction-scoped despite globally visible structure.
		/// Supported by: DB2, Firebird, Oracle, SAP Hana, SQL Server, Sybase ASE.
		/// </remarks>
		IsGlobalTemporaryData = 0b010000000,

		/// <summary>
		/// Temporary table with transaction-scoped data visibility,
		/// when supported by the provider.
		/// </summary>
		/// <remarks>
		/// Rows are visible only within the current transaction.
		/// Supported by: Firebird, Oracle, PostgreSQL.
		/// </remarks>
		IsTransactionTemporaryData = 0b100000000,

		/// <summary>
		/// Convenience flag: <see cref="CreateIfNotExists"/> | <see cref="DropIfExists"/>.
		/// </summary>
		CheckExistence = CreateIfNotExists | DropIfExists,

		/// <summary>
		/// Convenience flag: any temporary-related option is set.
		/// </summary>
		IsTemporaryOptionSet = IsTemporary | IsLocalTemporaryStructure | IsGlobalTemporaryStructure | IsLocalTemporaryData | IsGlobalTemporaryData | IsTransactionTemporaryData,
	}
}
