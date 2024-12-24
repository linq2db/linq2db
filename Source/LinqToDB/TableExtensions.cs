using System;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Expressions;

namespace LinqToDB
{
	/// <summary>
	/// Contains extension methods for LINQ queries.
	/// </summary>
	[PublicAPI]
	public static class TableExtensions
	{
		#region Table Helpers

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

		/// <summary>
		/// Builds table name for <paramref name="table"/>.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="table">Table instance.</param>
		/// <returns>Table name.</returns>
		public static string GetTableName<T>(this ITable<T> table)
			where T : notnull
		{
			return table.DataContext.CreateSqlProvider()
				.BuildObjectName(new (), new (table.TableName, Server: table.ServerName, Database: table.DatabaseName, Schema: table.SchemaName), tableOptions: table.TableOptions)
				.ToString();
		}

		#endregion

		// internal API
		internal static IDataProvider GetDataProvider<T>(this ITable<T> table)
			where T : notnull
		{
			if (table.DataContext is DataConnection dataConnection)
				return dataConnection.DataProvider;
			if (table.DataContext is DataContext dataContext)
				return dataContext.DataProvider;

			throw new ArgumentException($"Data context must be of {nameof(DataConnection)} or {nameof(DataContext)} type.", nameof(table));
		}

		// internal API
		internal static DataConnection GetDataConnection<T>(this ITable<T> table)
			where T : notnull
		{
			if (table.DataContext is DataConnection dataConnection)
				return dataConnection;
			if (table.DataContext is DataContext dataContext)
				return dataContext.GetDataConnection();

			throw new ArgumentException($"Data context must be of {nameof(DataConnection)} or {nameof(DataContext)} type.", nameof(table));
		}

		// internal API
		internal static bool TryGetDataConnection<T>(this ITable<T> table, [NotNullWhen(true)] out DataConnection? dataConnection)
			where T : notnull
		{
			if (table.DataContext is DataConnection dc)
				dataConnection = dc;
			else if (table.DataContext is DataContext dataContext)
				dataConnection = dataContext.GetDataConnection();
			else
				dataConnection = null;

			return dataConnection != null;
		}
	}
}
