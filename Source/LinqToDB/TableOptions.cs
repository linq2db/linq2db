using System;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

using LinqToDB.SqlQuery;

namespace LinqToDB
{
	/// <summary>
	/// Provides table mapping flags to specify temporary table kind if mapped table is temporary table
	/// and Create/Drop Table API behavior when target table exists/not exists.
	/// </summary>
	[Flags]
	[PublicAPI]
	public enum TableOptions
	{
		NotSet                     = 0b000000000,
		None                       = 0b000000001,
		/// <summary>
		/// IF NOT EXISTS option of the CREATE statement. This option will have effect only for databases that support the option.
		/// <para>Supported by: DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SQL Server, Sybase ASE.</para>
		/// </summary>
		CreateIfNotExists          = 0b000000010,
		/// <summary>
		/// IF EXISTS option of the DROP statement. This option will have effect only for databases that support the option.
		/// <para>Supported by: DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SQL Server, Sybase ASE.</para>
		/// </summary>
		DropIfExists               = 0b000000100,
		/// <summary>
		/// Table is temporary (not visible to other sessions). This option will have effect only for databases that support temporary tables.
		/// If database supports both global and local temporary tables, local table will be used.
		/// <para>Supported by: DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SQL Server, SAP Hana, Sybase ASE.</para>
		/// </summary>
		IsTemporary                = 0b000001000,
		/// <summary>
		/// Table is temporary (table structure is not visible to other sessions). This option will have effect only for databases that support temporary tables.
		/// <para>Supported by: DB2, Informix, MySql, PostgreSQL, SQLite, SAP Hana, SQL Server, Sybase ASE.</para>
		/// </summary>
		IsLocalTemporaryStructure  = 0b000010000,
		/// <summary>
		/// Table is global temporary (table structure is visible from other sessions). This option will have effect only for databases that support temporary tables.
		/// <para>Supported by: DB2, Firebird, Oracle, SAP Hana, SQL Server, Sybase ASE.</para>
		/// </summary>
		IsGlobalTemporaryStructure = 0b000100000,
		/// <summary>
		/// Table data is temporary (table data is not visible to other sessions). This option will have effect only for databases that support temporary tables.
		/// <para>Supported by: DB2, Informix, MySql, PostgreSQL, SQLite, SAP Hana, SQL Server, Sybase ASE.</para>
		/// </summary>
		IsLocalTemporaryData       = 0b001000000,
		/// <summary>
		/// Table data is global temporary (table data is visible from other sessions). This option will have effect only for databases that support temporary tables.
		/// <para>Supported by: DB2, Firebird, Oracle, SAP Hana, SQL Server, Sybase ASE.</para>
		/// </summary>
		IsGlobalTemporaryData      = 0b010000000,
		/// <summary>
		/// Table data is temporary (table data is transaction level visible). This option will have effect only for databases that support temporary tables.
		/// <para>Supported by: Firebird, Oracle, PostgreSQL.</para>
		/// </summary>
		IsTransactionTemporaryData = 0b100000000,

		CheckExistence             = CreateIfNotExists | DropIfExists,
		IsTemporaryOptionSet       = IsTemporary | IsLocalTemporaryStructure | IsGlobalTemporaryStructure | IsLocalTemporaryData | IsGlobalTemporaryData | IsTransactionTemporaryData,
	}

	public static partial class LinqExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSet                        (this TableOptions tableOptions) => tableOptions != TableOptions.NotSet;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTemporaryOptionSet         (this TableOptions tableOptions) => (tableOptions & TableOptions.IsTemporaryOptionSet) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasCreateIfNotExists         (this TableOptions tableOptions) => (tableOptions & TableOptions.CreateIfNotExists) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasDropIfExists              (this TableOptions tableOptions) => (tableOptions & TableOptions.DropIfExists) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsTemporary               (this TableOptions tableOptions) => (tableOptions & TableOptions.IsTemporary) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsLocalTemporaryStructure (this TableOptions tableOptions) => (tableOptions & TableOptions.IsLocalTemporaryStructure) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsGlobalTemporaryStructure(this TableOptions tableOptions) => (tableOptions & TableOptions.IsGlobalTemporaryStructure) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsLocalTemporaryData      (this TableOptions tableOptions) => (tableOptions & TableOptions.IsLocalTemporaryData) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsGlobalTemporaryData     (this TableOptions tableOptions) => (tableOptions & TableOptions.IsGlobalTemporaryData) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsTransactionTemporaryData(this TableOptions tableOptions) => (tableOptions & TableOptions.IsTransactionTemporaryData) != 0;

		public static TableOptions Or(this TableOptions tableOptions, TableOptions additionalOptions)
		{
			return tableOptions == TableOptions.NotSet ? additionalOptions : tableOptions;
		}

		internal static SqlTable Set(this SqlTable table, bool? set, TableOptions tableOptions)
		{
			if (set.HasValue)
			{
				if (set.Value) table.TableOptions |=  tableOptions;
				else           table.TableOptions &= ~tableOptions;
			}

			return table;
		}
	}
}
