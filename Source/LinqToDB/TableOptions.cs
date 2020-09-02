using System;

using JetBrains.Annotations;

namespace LinqToDB
{
	[Flags]
	[PublicAPI]
	public enum TableOptions
	{
		NotSet            = default,
		None              = 0b0001,
		/// <summary>
		/// Table is temporary. This option will have effect only for databases that support temporary tables.
		/// <para>Supported by: DB2, Oracle, PostgreSQL, SQLite, SQL Server, Sybase ASE.</para>
		/// </summary>
		IsTemporary       = 0b0010,
		/// <summary>
		/// Table is global temporary. This option will have effect only for databases that support temporary tables.
		/// <para>Supported by: DB2, Firebird, SQL Server, Sybase ASE.</para>
		/// </summary>
		IsGlobalTemporary = 0b0100,
		/// <summary>
		/// IF NOT EXISTS option of the CREATE statement. This option will have effect only for databases that support the option.
		/// <para>Supported by: DB2, Firebird, PostgreSQL, SQLite.</para>
		/// </summary>
		CreateIfNotExists = 0b1000,
	}

	public static partial class LinqExtensions
	{
		public static bool IsSet(this TableOptions tableOptions)
		{
			return tableOptions != LinqToDB.TableOptions.NotSet;
		}

		public static TableOptions Or(this TableOptions tableOptions, TableOptions additionalOptions)
		{
			return tableOptions == LinqToDB.TableOptions.NotSet ? additionalOptions : tableOptions;
		}
	}
}
