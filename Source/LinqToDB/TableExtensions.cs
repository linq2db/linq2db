using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Linq;

namespace LinqToDB
{
	/// <summary>
	/// Contains extension methods for LINQ queries.
	/// </summary>
	[PublicAPI]
	public static class TableExtensions
	{
		/// <summary>
		/// Overrides IsTemporary flag for the current table. This call will have effect only for databases that support
		/// temporary tables.
		/// <para>Supported by: DB2, Oracle, PostgreSQL, Informix, SQL Server, Sybase ASE.</para>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="isTemporary">If true, the current tables will handled as a temporary table.</param>
		/// <returns>Table-like query source with new owner/schema name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> IsTemporary<T>(this ITable<T> table, [SqlQueryDependent] bool isTemporary)
			where T : notnull
		{
			return ((ITableMutable<T>)table).ChangeTableOptions(isTemporary
				? table.TableOptions |  LinqToDB.TableOptions.IsTemporary
				: table.TableOptions & ~LinqToDB.TableOptions.IsTemporary);
		}

		/// <summary>
		/// Overrides IsTemporary flag for the current table. This call will have effect only for databases that support
		/// temporary tables.
		/// <para>Supported by: DB2, Oracle, PostgreSQL, Informix, SQL Server, Sybase ASE.</para>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <returns>Table-like query source with new owner/schema name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> IsTemporary<T>(this ITable<T> table)
			where T : notnull
		{
			return ((ITableMutable<T>)table).ChangeTableOptions(table.TableOptions | LinqToDB.TableOptions.IsTemporary);
		}

		/// <summary>
		/// Overrides TableOptions value for the current table. This call will have effect only for databases that support
		/// the options.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="options"><see cref="TableOptions"/> value.</param>
		/// <returns>Table-like query source with new owner/schema name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> TableOptions<T>(this ITable<T> table, [SqlQueryDependent] TableOptions options)
			where T : notnull
		{
			return ((ITableMutable<T>)table).ChangeTableOptions(options);
		}
	}
}
