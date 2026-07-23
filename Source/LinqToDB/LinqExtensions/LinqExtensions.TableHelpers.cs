using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Metadata;
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
		/// <param name="id">Logical table source identifier used later by <see cref="Sql.TableAlias(string)"/>, <see cref="Sql.TableName(string)"/>, or <see cref="Sql.TableSpec(string)"/>.</param>
		/// <returns>Table-like query source with the assigned table identifier.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// <para>
		/// LinqToDB generates table aliases during SQL translation, and a rendered table name can come from
		/// mapping configuration, table-name overrides, or provider-specific SQL builder rules. Do not hard-code
		/// those generated identifiers in hint text. This method is the first half of the mechanism: assign
		/// a logical id to a table source with <c>TableID(...)</c>, then use <c>Sql.TableAlias</c>,
		/// <c>Sql.TableName</c>, or <c>Sql.TableSpec</c> with the same id in hint/custom SQL APIs that accept
		/// <c>Sql.SqlID</c> values.
		/// </para>
		/// <para>
		/// APIs that can use this mechanism include provider hint methods with <c>Sql.SqlID</c> parameters,
		/// such as MySQL and PostgreSQL <c>SubQueryTableHint(...)</c>, SQL Server <c>OptionTableHint(...)</c>,
		/// Oracle/MySQL optimizer hints that target specific table references, and format-parameter hint APIs
		/// such as ClickHouse <c>SettingsHint(...)</c>.
		/// </para>
		/// <para>
		/// The <c>id</c> value is not emitted as SQL by itself. It is a translation-time key used
		/// to resolve the exact alias, table name, or table specification generated for this table source.
		/// </para>
		/// <para>
		/// The identifier affects SQL semantics and is emitted into SQL text according to provider rules when
		/// resolved through <c>Sql.SqlID</c>.
		/// </para>
		/// </remarks>
		[AiTags(Groups = AiGroup.Configuration, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[LinqTunnel]
		[Pure]
		public static ITable<T> TableID<T>(this ITable<T> table, [SqlQueryDependent] string? id)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(table);

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
		/// </remarks>
		[AiTags(Groups = AiGroup.Configuration, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[LinqTunnel]
		[Pure]
		public static ITable<T> TableName<T>(this ITable<T> table, [SqlQueryDependent] string name)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(table);
			ArgumentNullException.ThrowIfNull(name);

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
		/// </remarks>
		[AiTags(Groups = AiGroup.Configuration, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[LinqTunnel]
		[Pure]
		public static ITable<T> DatabaseName<T>(this ITable<T> table, [SqlQueryDependent] string? name)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(table);

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
		/// </remarks>
		[AiTags(Groups = AiGroup.Configuration, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[LinqTunnel]
		[Pure]
		public static ITable<T> ServerName<T>(this ITable<T> table, [SqlQueryDependent] string? name)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(table);

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
		/// </remarks>
		[AiTags(Groups = AiGroup.Configuration, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
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
		/// </remarks>
		[AiTags(Groups = AiGroup.Configuration, Execution = AiExecution.Deferred, Composability = AiComposability.Composable, Affects = AiAffects.SqlSemantics, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
		[LinqTunnel]
		[Pure]
		public static ITable<T> WithTableExpression<T>(this ITable<T> table, [SqlQueryDependent] string expression)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(expression);

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
