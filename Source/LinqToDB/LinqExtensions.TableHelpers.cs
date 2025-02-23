using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Reflection;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Assigns table id.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="id">Table ID.</param>
		/// <returns>Table-like query source with new name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> TableID<T>(this ITable<T> table, [SqlQueryDependent] string? id)
			where T : notnull
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var result = ((ITableMutable<T>)table).ChangeTableID(id);
			return result;
		}

		/// <summary>
		/// Overrides table or view name with new name for current query.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Name of table.</param>
		/// <returns>Table-like query source with new name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> TableName<T>(this ITable<T> table, [SqlQueryDependent] string name)
			where T : notnull
		{
			if (table == null) throw new ArgumentNullException(nameof(table));
			if (name == null) throw new ArgumentNullException(nameof(name));

			var result = ((ITableMutable<T>)table).ChangeTableName(name);
			return result;
		}

		/// <summary>
		/// Overrides database name with new name for current query. This call will have effect only for databases that support
		/// database name in fully-qualified table name.
		/// <para>Supported by: Access, DB2, MySQL, PostgreSQL, SAP HANA, SQLite, Informix, SQL Server, Sybase ASE.</para>
		/// <para>Requires schema name (see <see cref="SchemaName{T}(ITable{T}, string)"/>): DB2, SAP HANA, PostgreSQL.</para>
		/// <para>PostgreSQL supports only name of current database.</para>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Name of database.</param>
		/// <returns>Table-like query source with new database name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> DatabaseName<T>(this ITable<T> table, [SqlQueryDependent] string? name)
			where T : notnull
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var result = ((ITableMutable<T>)table).ChangeDatabaseName(name);
			return result;
		}

		/// <summary>
		/// Overrides linked server name with new name for current query. This call will have effect only for databases that support
		/// linked server name in fully-qualified table name.
		/// <para>Supported by: SQL Server, Informix, Oracle, SAP HANA2.</para>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Name of linked server.</param>
		/// <returns>Table-like query source with new linked server name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> ServerName<T>(this ITable<T> table, [SqlQueryDependent] string? name)
			where T : notnull
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var result = ((ITableMutable<T>)table).ChangeServerName(name);
			return result;
		}

		/// <summary>
		/// Overrides owner/schema name with new name for current query. This call will have effect only for databases that support
		/// owner/schema name in fully-qualified table name.
		/// <para>Supported by: DB2, Oracle, PostgreSQL, Informix, SQL Server, Sybase ASE.</para>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Name of owner/schema.</param>
		/// <returns>Table-like query source with new owner/schema name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> SchemaName<T>(this ITable<T> table, [SqlQueryDependent] string? name)
			where T : notnull
		{
			var result = ((ITableMutable<T>)table).ChangeSchemaName(name);
			return result;
		}

		/// <summary>
		/// Replaces access to a table in generated query with SQL expression.
		/// Example below adds hint to a table. Also see <see cref="With{T}(ITable{T}, string)"/> method.
		/// <code>
		/// var tableWithHint = db.Table.WithTableExpression("{0} {1} with (UpdLock)");
		/// </code>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="expression">SQL template to use instead of table name. Template supports two parameters:
		/// <para> - {0} original table name;</para>
		/// <para> - {1} table alias.</para>
		/// </param>
		/// <returns>Table-like query source with new table source expression.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> WithTableExpression<T>(this ITable<T> table, [SqlQueryDependent] string expression)
			where T : notnull
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var newTable = new Table<T>(table.DataContext,
				Expression.Call(
					null,
					Methods.LinqToDB.Table.WithTableExpression.MakeGenericMethod(typeof(T)),
					table.Expression, Expression.Constant(expression))
			);

			return newTable;
		}
	}
}
