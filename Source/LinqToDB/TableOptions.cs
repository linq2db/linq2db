using System;

using JetBrains.Annotations;
using LinqToDB.SqlQuery;

namespace LinqToDB
{
	[Flags]
	[PublicAPI]
	public enum TableOptions
	{
		NotSet            = 0b00000,
		None              = 0b00001,
		/// <summary>
		/// Table is temporary. This option will have effect only for databases that support temporary tables.
		/// <para>Supported by: DB2, Informix, MySql, Oracle, PostgreSQL, SQLite, SQL Server, Sybase ASE.</para>
		/// </summary>
		IsTemporary       = 0b00010,
		/// <summary>
		/// Table is global temporary. This option will have effect only for databases that support temporary tables.
		/// <para>Supported by: DB2, Firebird, SQL Server, Sybase ASE.</para>
		/// </summary>
		IsGlobalTemporary = 0b00100,
		/// <summary>
		/// IF NOT EXISTS option of the CREATE statement. This option will have effect only for databases that support the option.
		/// <para>Supported by: DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SQL Server, Sybase ASE.</para>
		/// </summary>
		CreateIfNotExists = 0b01000,
		/// <summary>
		/// IF EXISTS option of the DROP statement. This option will have effect only for databases that support the option.
		/// <para>Supported by: DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SQL Server, Sybase ASE.</para>
		/// </summary>
		DropIfExists      = 0b10000,
	}

	public static partial class LinqExtensions
	{
		public static bool IsSet(this TableOptions tableOptions)
		{
			return tableOptions != LinqToDB.TableOptions.NotSet;
		}

		public static bool HasIsTemporary(this TableOptions tableOptions)
		{
			return (tableOptions & LinqToDB.TableOptions.IsTemporary) != 0;
		}

		public static bool HasIsGlobalTemporary(this TableOptions tableOptions)
		{
			return (tableOptions & LinqToDB.TableOptions.IsGlobalTemporary) != 0;
		}

		public static bool HasCreateIfNotExists(this TableOptions tableOptions)
		{
			return (tableOptions & LinqToDB.TableOptions.CreateIfNotExists) != 0;
		}

		public static bool HasDropIfExists(this TableOptions tableOptions)
		{
			return (tableOptions & LinqToDB.TableOptions.DropIfExists) != 0;
		}

		public static TableOptions Or(this TableOptions tableOptions, TableOptions additionalOptions)
		{
			return tableOptions == LinqToDB.TableOptions.NotSet ? additionalOptions : tableOptions;
		}

		internal static SqlTable Set(this SqlTable table, bool set, TableOptions tableOptions)
		{
			if (set) table.TableOptions |=  tableOptions;
			else     table.TableOptions &= ~tableOptions;

			return table;
		}
	}
}
