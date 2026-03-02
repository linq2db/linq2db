using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Assigns a table identifier for query translation.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="id">Table identifier. Provider-specific meaning (e.g. used for table hints or table routing).</param>
		/// <returns>Table-like query source with the assigned table identifier.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The identifier affects SQL semantics and is emitted into SQL text according to provider rules.
		/// <para>
		/// <b>AI:</b>
		/// Group=TableConfiguration
		/// Execution=Deferred
		/// Composability=Composable
		/// Affects=SqlSemantics
		/// Pipeline=ExpressionTree,SqlAST,SqlText
		/// Provider=ProviderDefined
		/// </para>
		/// </remarks>
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
		/// Overrides the table or view name for the current query.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Table or view name.</param>
		/// <returns>Table-like query source with the overridden table name.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The name affects SQL semantics and is emitted into SQL text according to provider rules.
		/// <para>
		/// <b>AI:</b>
		/// Group=TableConfiguration
		/// Execution=Deferred
		/// Composability=Composable
		/// Affects=SqlSemantics
		/// Pipeline=ExpressionTree,SqlAST,SqlText
		/// Provider=ProviderDefined
		/// </para>
		/// </remarks>
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
		/// Overrides the database name for the current query.
		/// This call affects only providers that support database name as part of a fully qualified table name.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Database name.</param>
		/// <returns>Table-like query source with the overridden database name.</returns>
		/// <remarks>
		/// Provider support: Access, DB2, MySQL, PostgreSQL, SAP HANA, SQLite, Informix, SQL Server, Sybase ASE.
		/// <para>
		/// Requires schema name (see <see cref="SchemaName{T}(ITable{T}, string)"/>): DB2, SAP HANA, PostgreSQL.
		/// </para>
		/// <para>
		/// PostgreSQL supports only the current database name.
		/// </para>
		/// <para>
		/// Execution is deferred and the method is composable.
		/// The name affects SQL semantics and is emitted into SQL text according to provider rules.
		/// </para>
		/// <para>
		/// <b>AI:</b>
		/// Group=TableConfiguration
		/// Execution=Deferred
		/// Composability=Composable
		/// Affects=SqlSemantics
		/// Pipeline=ExpressionTree,SqlAST,SqlText
		/// Provider=ProviderDefined
		/// </para>
		/// </remarks>
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
		/// Overrides the linked server name for the current query.
		/// This call affects only providers that support linked server name as part of a fully qualified table name.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Linked server name.</param>
		/// <returns>Table-like query source with the overridden linked server name.</returns>
		/// <remarks>
		/// Provider support: SQL Server, Informix, Oracle, SAP HANA2.
		/// <para>
		/// Execution is deferred and the method is composable.
		/// The name affects SQL semantics and is emitted into SQL text according to provider rules.
		/// </para>
		/// <para>
		/// <b>AI:</b>
		/// Group=TableConfiguration
		/// Execution=Deferred
		/// Composability=Composable
		/// Affects=SqlSemantics
		/// Pipeline=ExpressionTree,SqlAST,SqlText
		/// Provider=ProviderDefined
		/// </para>
		/// </remarks>
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
		/// Overrides the owner/schema name for the current query.
		/// This call affects only providers that support owner/schema name as part of a fully qualified table name.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Owner or schema name.</param>
		/// <returns>Table-like query source with the overridden owner/schema name.</returns>
		/// <remarks>
		/// Provider support: DB2, Oracle, PostgreSQL, Informix, SQL Server, Sybase ASE.
		/// <para>
		/// Execution is deferred and the method is composable.
		/// The name affects SQL semantics and is emitted into SQL text according to provider rules.
		/// </para>
		/// <para>
		/// <b>AI:</b>
		/// Group=TableConfiguration
		/// Execution=Deferred
		/// Composability=Composable
		/// Affects=SqlSemantics
		/// Pipeline=ExpressionTree,SqlAST,SqlText
		/// Provider=ProviderDefined
		/// </para>
		/// </remarks>
		[LinqTunnel]
		[Pure]
		public static ITable<T> SchemaName<T>(this ITable<T> table, [SqlQueryDependent] string? name)
			where T : notnull
		{
			var result = ((ITableMutable<T>)table).ChangeSchemaName(name);
			return result;
		}

		/// <summary>
		/// Replaces the table reference in generated SQL with a user-provided SQL template.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="expression">
		/// SQL template to use instead of the table name.
		/// Template supports two placeholders:
		/// <para>- <c>{0}</c>: original table name;</para>
		/// <para>- <c>{1}</c>: table alias.</para>
		/// </param>
		/// <returns>Table-like query source with the overridden table source expression.</returns>
		/// <remarks>
		/// This API is typically used to inject provider-specific table syntax (e.g. hints).
		/// For a dedicated hint API see <see cref="With{T}(ITable{T}, string)"/>.
		/// <code>
		/// var tableWithHint = db.Table.WithTableExpression("{0} {1} with (UpdLock)");
		/// </code>
		/// Execution is deferred and the method is composable.
		/// The template affects SQL semantics and is emitted into SQL text according to provider rules.
		/// <para>
		/// <b>AI:</b>
		/// Group=TableConfiguration
		/// Execution=Deferred
		/// Composability=Composable
		/// Affects=SqlSemantics
		/// Pipeline=ExpressionTree,SqlAST,SqlText
		/// Provider=ProviderDefined
		/// </para>
		/// </remarks>
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
