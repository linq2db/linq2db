using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Async;
	using DataProvider;
	using Expressions;
	using Linq;
	using Linq.Builder;
	using Reflection;
	using SqlProvider;

	/// <summary>
	/// Contains extension methods for LINQ queries.
	/// </summary>
	[PublicAPI]
	public static partial class LinqExtensions
	{
		#region Table Helpers

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
			if (name  == null) throw new ArgumentNullException(nameof(name));

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

		#endregion

		#region TableHint

		/// <summary>
		/// Adds a table hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.MySql,  Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		public static ITable<TSource> With<TSource>(this ITable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return newTable;
		}

		/// <summary>
		/// Adds a table hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.MySql,  Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		public static ITable<TSource> TableHint<TSource>(this ITable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return newTable;
		}

		/// <summary>
		/// Adds a table hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameter">Table hint parameter.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.MySql,  Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.TableHint, typeof(HintWithParameterExtensionBuilder))]
		public static ITable<TSource> TableHint<TSource,TParam>(
			this ITable<TSource>       table,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] TParam hintParameter)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameter),
					table.Expression, Expression.Constant(hint), Expression.Constant(hintParameter))
			);

			return newTable;
		}

		/// <summary>
		/// Adds a table hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder), " ", " ")]
		[Sql.QueryExtension(ProviderName.MySql,  Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder), " ", ", ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.TableHint, typeof(HintWithParametersExtensionBuilder))]
		public static ITable<TSource> TableHint<TSource,TParam>(
			this ITable<TSource>                table,
			[SqlQueryDependent] string          hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameters),
					table.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p, typeof(TParam)))))
			);

			return newTable;
		}

		#endregion

		#region TablesInScopeHint

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.MySql,  Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		public static IQueryable<TSource> TablesInScopeHint<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint),
				currentSource.Expression, Expression.Constant(hint));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameter">Table hint parameter.</param>
		/// <returns>Query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.MySql,  Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintWithParameterExtensionBuilder))]
		public static IQueryable<TSource> TablesInScopeHint<TSource,TParam>(
			this IQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] TParam hintParameter)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint, hintParameter),
				currentSource.Expression, Expression.Constant(hint), Expression.Constant(hintParameter));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder), " ", " ")]
		[Sql.QueryExtension(ProviderName.MySql,  Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder), " ", ", ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintWithParametersExtensionBuilder))]
		public static IQueryable<TSource> TablesInScopeHint<TSource>(
			this IQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params object[] hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint, hintParameters),
				currentSource.Expression,
				Expression.Constant(hint),
				Expression.NewArrayInit(typeof(object), hintParameters.Select(Expression.Constant)));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region IndexHint

		/// <summary>
		/// Adds an index hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Table-like query source with index hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.IndexHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.MySql,  Sql.QueryExtensionScope.IndexHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.IndexHint, typeof(HintExtensionBuilder))]
		public static ITable<TSource> IndexHint<TSource>(this ITable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(IndexHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return newTable;
		}

		/// <summary>
		/// Adds an index hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameter">Table hint parameter.</param>
		/// <returns>Table-like query source with index hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.IndexHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(ProviderName.MySql,  Sql.QueryExtensionScope.IndexHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.IndexHint, typeof(HintWithParameterExtensionBuilder))]
		public static ITable<TSource> IndexHint<TSource,TParam>(
			this ITable<TSource>       table,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] TParam hintParameter)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(IndexHint, table, hint, hintParameter),
					table.Expression, Expression.Constant(hint), Expression.Constant(hintParameter))
			);

			return newTable;
		}

		/// <summary>
		/// Adds an index hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Table-like query source with index hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.IndexHint, typeof(TableSpecHintExtensionBuilder), " ", " ")]
		[Sql.QueryExtension(ProviderName.MySql,  Sql.QueryExtensionScope.IndexHint, typeof(HintWithParametersExtensionBuilder))]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.IndexHint, typeof(HintWithParametersExtensionBuilder))]
		public static ITable<TSource> IndexHint<TSource,TParam>(
			this ITable<TSource>                table,
			[SqlQueryDependent] string          hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(IndexHint, table, hint, hintParameters),
					table.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p, typeof(TParam)))))
			);

			return newTable;
		}

		#endregion

		#region JoinHint

		/// <summary>
		/// Adds a join hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(Sql.QueryExtensionScope.JoinHint, typeof(NoneExtensionBuilder))]
		public static IQueryable<TSource> JoinHint<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(JoinHint, source, hint),
				currentSource.Expression, Expression.Constant(hint));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region SubQueryHint

		/// <summary>
		/// Adds a query hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.SubQueryHint, typeof(HintExtensionBuilder))]
		public static IQueryable<TSource> SubQueryHint<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(SubQueryHint, source, hint),
				currentSource.Expression, Expression.Constant(hint));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Adds a query hint to the generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Hint parameter type</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameter">Hint parameter.</param>
		/// <returns>Query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.SubQueryHint, typeof(HintWithParameterExtensionBuilder))]
		public static IQueryable<TSource> SubQueryHint<TSource,TParam>(
			this IQueryable<TSource>   source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] TParam hintParameter)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(SubQueryHint, source, hint, hintParameter),
				currentSource.Expression,
				Expression.Constant(hint),
				Expression.Constant(hintParameter));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Adds a query hint to the generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Table-like query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.SubQueryHint, typeof(HintWithParametersExtensionBuilder))]
		public static IQueryable<TSource> SubQueryHint<TSource, TParam>(
			this IQueryable<TSource>   source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(SubQueryHint, source, hint, hintParameters),
				currentSource.Expression,
				Expression.Constant(hint),
				Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p))));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region QueryHint

		/// <summary>
		/// Adds a query hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.QueryHint, typeof(HintExtensionBuilder))]
		public static IQueryable<TSource> QueryHint<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(QueryHint, source, hint),
				currentSource.Expression, Expression.Constant(hint));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Adds a query hint to the generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Hint parameter type</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameter">Hint parameter.</param>
		/// <returns>Query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParameterExtensionBuilder))]
		public static IQueryable<TSource> QueryHint<TSource,TParam>(
			this IQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] TParam hintParameter)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(QueryHint, source, hint, hintParameter),
				currentSource.Expression,
				Expression.Constant(hint),
				Expression.Constant(hintParameter));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Adds a query hint to the generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Table-like query source with hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.Oracle, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParametersExtensionBuilder), " ")]
		[Sql.QueryExtension(null,                Sql.QueryExtensionScope.QueryHint, typeof(HintWithParametersExtensionBuilder))]
		public static IQueryable<TSource> QueryHint<TSource, TParam>(
			this IQueryable<TSource> source,
			[SqlQueryDependent] string hint,
			[SqlQueryDependent] params TParam[] hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(QueryHint, source, hint, hintParameters),
				currentSource.Expression,
				Expression.Constant(hint),
				Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p))));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Scalar Select

		/// <summary>
		/// Loads scalar value or record from database without explicit table source.
		/// Could be usefull for function calls, querying of database variables or properties, subqueries, execution of code on server side.
		/// </summary>
		/// <typeparam name="T">Type of result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="selector">Value selection expression.</param>
		/// <returns>Requested value.</returns>
		[Pure]
		public static T Select<T>(
			                this IDataContext   dataContext,
			[InstantHandle] Expression<Func<T>> selector)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (selector    == null) throw new ArgumentNullException(nameof(selector));

			var q = new ExpressionQueryImpl<T>(dataContext, selector);

			foreach (var item in q)
				return item;

			throw new InvalidOperationException();
		}

		/// <summary>
		/// Loads scalar value or record from database without explicit table source asynchronously.
		/// Could be usefull for function calls, querying of database variables or properties, subqueries, execution of code on server side.
		/// </summary>
		/// <typeparam name="T">Type of result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="selector">Value selection expression.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Requested value.</returns>
		[Pure]
		public static async Task<T> SelectAsync<T>(
			                this IDataContext   dataContext,
			[InstantHandle] Expression<Func<T>> selector,
			                CancellationToken token = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (selector    == null) throw new ArgumentNullException(nameof(selector));

			var q = new ExpressionQueryImpl<T>(dataContext, selector);

			var read = false;
			var item = default(T)!; // this is fine, as we never return it

			await q.ForEachUntilAsync(r =>
			{
				read = true;
				item = r;
				return false;
			}, token).ConfigureAwait(false);

			if (read)
				return item;

			throw new InvalidOperationException();
		}

		#endregion

		#region Delete

		/// <summary>
		/// Executes delete operation, using source query as filter for records, that should be deleted.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <returns>Number of deleted records.</returns>
		public static int Delete<T>(this IQueryable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Delete.DeleteQueryable.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes delete operation asynchronously, using source query as filter for records, that should be deleted.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of deleted records.</returns>
		public static Task<int> DeleteAsync<T>(this IQueryable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Delete.DeleteQueryable.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes delete operation, using source query as initial filter for records, that should be deleted, and predicate expression as additional filter.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <param name="predicate">Filter expression, to specify what records from source should be deleted.</param>
		/// <returns>Number of deleted records.</returns>
		public static int Delete<T>(
			                this IQueryable<T>       source,
			[InstantHandle] Expression<Func<T,bool>> predicate)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Delete.DeleteQueryablePredicate.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(predicate));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes delete operation asynchronously, using source query as initial filter for records, that should be deleted, and predicate expression as additional filter.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <param name="predicate">Filter expression, to specify what records from source should be deleted.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of deleted records.</returns>
		public static Task<int> DeleteAsync<T>(
			           this IQueryable<T>            source,
			[InstantHandle] Expression<Func<T,bool>> predicate,
			CancellationToken                        token = default)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Delete.DeleteQueryablePredicate.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(predicate));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Update

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateTarget.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update-from-source operation asynchronously against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static Task<int> UpdateAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                 token = default)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateTarget.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<T>(this IQueryable<T> source, [InstantHandle] Expression<Func<T,T>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateSetter.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update operation asynchronously using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static Task<int> UpdateAsync<T>(
			           this IQueryable<T>         source,
			[InstantHandle] Expression<Func<T,T>> setter,
			CancellationToken                     token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateSetter.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes update operation using source query as record filter with additional filter expression.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="predicate">Filter expression, to specify what records from source query should be updated.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<T>(
			                this IQueryable<T>       source,
			[InstantHandle] Expression<Func<T,bool>> predicate,
			[InstantHandle] Expression<Func<T,T>>    setter)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));
			if (setter    == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdatePredicateSetter.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(predicate), Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update operation asynchronously using source query as record filter with additional filter expression.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="predicate">Filter expression, to specify what records from source query should be updated.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static Task<int> UpdateAsync<T>(
			           this IQueryable<T>            source,
			[InstantHandle] Expression<Func<T,bool>> predicate,
			[InstantHandle] Expression<Func<T,T>>    setter,
			CancellationToken                        token = default)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));
			if (setter    == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdatePredicateSetter.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(predicate), Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes update operation for already configured update query.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Update query.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<T>(this IUpdatable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((Updatable<T>)source).Query;

			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateUpdatable.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update operation asynchronously for already configured update query.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Update query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static Task<int> UpdateAsync<T>(this IUpdatable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var q = ((Updatable<T>)source).Query;

			var currentSource = q.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateUpdatable.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// Also see <seealso cref="Update{TSource, TTarget}(IQueryable{TSource}, ITable{TTarget}, Expression{Func{TSource, TTarget}})"/> method.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table selection expression.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			[InstantHandle] Expression<Func<TSource,TTarget>> target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateTargetFuncSetter.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression, Expression.Quote(target), Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update-from-source operation asynchronously against target table.
		/// Also see <seealso cref="UpdateAsync{TSource, TTarget}(IQueryable{TSource}, ITable{TTarget}, Expression{Func{TSource, TTarget}}, CancellationToken)"/> method.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table selection expression.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static Task<int> UpdateAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			[InstantHandle] Expression<Func<TSource,TTarget>> target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                 token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateTargetFuncSetter.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression, Expression.Quote(target), Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		internal sealed class Updatable<T> : IUpdatable<T>
		{
			public Updatable(IQueryable<T> query)
			{
				Query = query;
			}

			public IQueryable<T> Query;

			public override string ToString()
			{
				return Query.ToString()!;
			}
		}

		/// <summary>
		/// Casts <see cref="IQueryable{T}"/> query to <see cref="IUpdatable{T}"/> query.
		/// </summary>
		/// <typeparam name="T">Query record type.</typeparam>
		/// <param name="source">Source <see cref="IQueryable{T}"/> query.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> AsUpdatable<T>(this IQueryable<T> source)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var query = currentSource.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.AsUpdatable.MakeGenericMethod(typeof(T)),
					currentSource.Expression));

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="update">Updated field setter expression. Uses updated record as parameter.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			                this IQueryable<T>     source,
			[InstantHandle] Expression<Func<T,TV>> extract,
			[InstantHandle] Expression<Func<T,TV>> update)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update  == null) throw new ArgumentNullException(nameof(update));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.SetQueryablePrev.MakeGenericMethod(typeof(T), typeof(TV)),
				currentSource.Expression, Expression.Quote(extract), Expression.Quote(update));

			var query = currentSource.Provider.CreateQuery<T>(expr);
			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="update">Updated field setter expression. Uses updated record as parameter.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			                this IUpdatable<T>     source,
			[InstantHandle] Expression<Func<T,TV>> extract,
			[InstantHandle] Expression<Func<T,TV>> update)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update  == null) throw new ArgumentNullException(nameof(update));

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.SetUpdatablePrev.MakeGenericMethod(typeof(T), typeof(TV)),
					query.Expression, Expression.Quote(extract), Expression.Quote(update)));

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="update">Updated field setter expression.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			                this IQueryable<T>     source,
			[InstantHandle] Expression<Func<T,TV>> extract,
			[InstantHandle] Expression<Func<TV>>   update)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update  == null) throw new ArgumentNullException(nameof(update));

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.SetQueryableExpression.MakeGenericMethod(typeof(T), typeof(TV)),
					source.Expression, Expression.Quote(extract), Expression.Quote(update)));

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="update">Updated field setter expression.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			                this IUpdatable<T>     source,
			[InstantHandle] Expression<Func<T,TV>> extract,
			[InstantHandle] Expression<Func<TV>>   update)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update  == null) throw new ArgumentNullException(nameof(update));

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.SetUpdatableExpression.MakeGenericMethod(typeof(T), typeof(TV)),
					query.Expression, Expression.Quote(extract), Expression.Quote(update)));

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="value">Value, assigned to updated field.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			                 this IQueryable<T>     source,
			[InstantHandle]  Expression<Func<T,TV>> extract,
			[SkipIfConstant] TV                     value)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.SetQueryableValue.MakeGenericMethod(typeof(T), typeof(TV)),
				currentSource.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV)));

			var query = currentSource.Provider.CreateQuery<T>(expr);

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="value">Value, assigned to updated field.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			                 this IUpdatable<T>     source,
			[InstantHandle]  Expression<Func<T,TV>> extract,
			[SkipIfConstant] TV                     value)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.SetUpdatableValue.MakeGenericMethod(typeof(T), typeof(TV)),
					query.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV))));

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds update field expression to query. It can be any expression with string interpolation.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="setExpression">Custom update expression.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		/// <example>
		/// The following example shows how to append string value to appropriate field.
		/// <code>
		///		db.Users.Where(u => u.UserId == id)
		///			.Set(u => $"{u.Name}" += {str}")
		///			.Update();
		/// </code>
		/// </example>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T>(
			                this IQueryable<T>         source,
			[InstantHandle] Expression<Func<T,string>> setExpression)
		{
			if (source        == null) throw new ArgumentNullException(nameof(source));
			if (setExpression == null) throw new ArgumentNullException(nameof(setExpression));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.SetQueryableSetCustom.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(setExpression));

			var query = currentSource.Provider.CreateQuery<T>(expr);
			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds update field expression to query. It can be any expression with string interpolation.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="setExpression">Custom update expression.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		/// <example>
		/// The following example shows how to append string value to appropriate field.
		/// <code>
		///		db.Users.Where(u => u.UserId == id)
		///			.AsUpdatable()
		///			.Set(u => $"{u.Name}" += {str}")
		///			.Update();
		/// </code>
		/// </example>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T>(
			                this IUpdatable<T>         source,
			[InstantHandle] Expression<Func<T,string>> setExpression)
		{
			if (source        == null) throw new ArgumentNullException(nameof(source));
			if (setExpression == null) throw new ArgumentNullException(nameof(setExpression));

			var query = ((Updatable<T>)source).Query;

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.SetUpdatableSetCustom.MakeGenericMethod(typeof(T)),
				query.Expression, Expression.Quote(setExpression));

			query = query.Provider.CreateQuery<T>(expr);
			return new Updatable<T>(query);
		}

		#endregion

		#region Insert

		/// <summary>
		/// Inserts single record into target table.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<T>(
			                this ITable<T>      target,
			[InstantHandle] Expression<Func<T>> setter)
			where T : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Insert, target, setter),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<T>(
			                this ITable<T>      target,
			[InstantHandle] Expression<Func<T>> setter,
			CancellationToken                   token = default)
			where T : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Insert, target, setter),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static object InsertWithIdentity<T>(
			                this ITable<T>      target,
			[InstantHandle] Expression<Func<T>> setter)
			where T : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentQuery = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, target, setter),
				currentQuery.Expression, Expression.Quote(setter));

			return currentQuery.Execute<object>(expr);
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static int InsertWithInt32Identity<T>(
			                this ITable<T>      target,
			[InstantHandle] Expression<Func<T>> setter)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<int>(InsertWithIdentity(target, setter));
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static long InsertWithInt64Identity<T>(
			                this ITable<T>      target,
			[InstantHandle] Expression<Func<T>> setter)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<long>(InsertWithIdentity(target, setter));
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static decimal InsertWithDecimalIdentity<T>(
			                this ITable<T>      target,
			[InstantHandle] Expression<Func<T>> setter)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<decimal>(InsertWithIdentity(target, setter));
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static Task<object> InsertWithIdentityAsync<T>(
			                this ITable<T>      target,
			[InstantHandle] Expression<Func<T>> setter,
			CancellationToken                   token = default)
			where T : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, target, setter),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<object>(expr, token);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<int> InsertWithInt32IdentityAsync<T>(
			                this ITable<T>      target,
			[InstantHandle] Expression<Func<T>> setter,
			CancellationToken                   token = default)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<int>(
				await InsertWithIdentityAsync(target, setter, token).ConfigureAwait(false)
			);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<long> InsertWithInt64IdentityAsync<T>(
			                this ITable<T>      target,
			[InstantHandle] Expression<Func<T>> setter,
			CancellationToken                   token = default)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<long>(
				await InsertWithIdentityAsync(target, setter, token).ConfigureAwait(false)
			);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<decimal> InsertWithDecimalIdentityAsync<T>(
			                this ITable<T>      target,
			[InstantHandle] Expression<Func<T>> setter,
			CancellationToken                   token = default)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<decimal>(
				await InsertWithIdentityAsync(target, setter, token).ConfigureAwait(false)
			);
		}

		#region ValueInsertable

		internal sealed class ValueInsertable<T> : IValueInsertable<T>
		{
			public ValueInsertable(IQueryable<T> query)
			{
				Query = query;
			}

			public IQueryable<T> Query;

			public override string ToString()
			{
				return Query.ToString()!;
			}
		}

		/// <summary>
		/// Starts insert operation LINQ query definition.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="target">Target table.</param>
		/// <returns>Insertable source query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Into<T>(this IDataContext dataContext, ITable<T> target)
			where T : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			var currentSource = target.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Into, dataContext, target),
				SqlQueryRootExpression.Create(dataContext), currentSource.Expression);

			var v = currentSource.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(v);
		}

		/// <summary>
		/// Starts insert operation LINQ query definition.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="source">Target table.</param>
		/// <returns>Insertable source query.</returns>
		public static IValueInsertable<T> AsValueInsertable<T>(this ITable<T> source)
			where T : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Insert.T.AsValueInsertable.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			var query = currentSource.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(query);
		}

		/// <summary>
		/// Starts insert operation LINQ query definition from field setter expression.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Source table to insert to.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T,TV>(
			                this ITable<T>         source,
			[InstantHandle] Expression<Func<T,TV>> field,
			[InstantHandle] Expression<Func<TV>>   value)
			where T : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));
			if (value  == null) throw new ArgumentNullException(nameof(value));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				currentSource.Expression, Expression.Quote(field), Expression.Quote(value));

			var q = currentSource.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(q);
		}

		/// <summary>
		/// Starts insert operation LINQ query definition from field setter expression.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Source table to insert to.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T,TV>(
			                 this ITable<T>         source,
			[InstantHandle]  Expression<Func<T,TV>> field,
			[SkipIfConstant] TV                     value)
			where T : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				currentSource.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TV)));

			var q = currentSource.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(q);
		}

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T,TV>(
			                this IValueInsertable<T> source,
			[InstantHandle] Expression<Func<T,TV>>   field,
			[InstantHandle] Expression<Func<TV>>     value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));
			if (value  == null) throw new ArgumentNullException(nameof(value));

			var query = ((ValueInsertable<T>)source).Query;

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				query.Expression, Expression.Quote(field), Expression.Quote(value));

			var q = query.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(q);
		}

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T,TV>(
			                 this IValueInsertable<T> source,
			[InstantHandle]  Expression<Func<T,TV>>   field,
			[SkipIfConstant] TV                       value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));

			var query = ((ValueInsertable<T>)source).Query;

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TV)));

			var q = query.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(q);
		}

		/// <summary>
		/// Executes insert query.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<T>(this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Insert.VI.Insert.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes insert query asynchronously.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<T>( this IValueInsertable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Insert.VI.Insert.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		[Pure]
		public static object InsertWithIdentity<T>(this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source),
				currentSource.Expression);

			return currentSource.Execute<object>(expr);
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static int? InsertWithInt32Identity<T>( this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static long? InsertWithInt64Identity<T>( this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static decimal? InsertWithDecimalIdentity<T>( this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static Task<object> InsertWithIdentityAsync<T>(
			 this IValueInsertable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source),
				currentSource.Expression);

			return currentSource.ExecuteAsync<object>(expr, token);
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<int?> InsertWithInt32IdentityAsync<T>(
			 this IValueInsertable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<long?> InsertWithInt64IdentityAsync<T>(
			 this IValueInsertable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<decimal?> InsertWithDecimalIdentityAsync<T>(
			 this IValueInsertable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		#endregion

		#region SelectInsertable

		/// <summary>
		/// Inserts records from source query into target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Insert, source, target, setter),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                 token = default)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Insert, source, target, setter),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static object InsertWithIdentity<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source, target, setter),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.Execute<object>(expr);
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static int? InsertWithInt32Identity<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<int?>(
				InsertWithIdentity(currentSource, target, setter));
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static long? InsertWithInt64Identity<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<long?>(
				InsertWithIdentity(currentSource, target, setter));
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static decimal? InsertWithDecimalIdentity<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				InsertWithIdentity(currentSource, target, setter));
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static Task<object> InsertWithIdentityAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                 token = default)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source, target, setter),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<object>(expr, token);
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static async Task<int?> InsertWithInt32IdentityAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                 token = default)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<int?>(
				await InsertWithIdentityAsync(currentSource, target, setter, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static async Task<long?> InsertWithInt64IdentityAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                 token = default)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<long?>(
				await InsertWithIdentityAsync(currentSource, target, setter, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static async Task<decimal?> InsertWithDecimalIdentityAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                 token = default)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				await InsertWithIdentityAsync(currentSource, target, setter, token).ConfigureAwait(false));
		}

		internal sealed class SelectInsertable<T,TT> : ISelectInsertable<T,TT>
		{
			public SelectInsertable(IQueryable<T> query)
			{
				Query = query;
			}

			public IQueryable<T> Query;

			public override string ToString()
			{
				return Query.ToString()!;
			}
		}

		/// <summary>
		/// Converts LINQ query into insert query with source query data as data to insert.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <returns>Insertable source query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource,TTarget> Into<TSource,TTarget>(
			 this IQueryable<TSource> source,
			 ITable<TTarget>          target)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Into, source, target),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression);

			var q = currentSource.Provider.CreateQuery<TSource>(expr);
			return new SelectInsertable<TSource,TTarget>(q);
		}

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TTarget">Target record type</typeparam>
		/// <typeparam name="TValue">Field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression. Accepts source record as parameter.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource,TTarget> Value<TSource,TTarget,TValue>(
			                this ISelectInsertable<TSource,TTarget> source,
			[InstantHandle] Expression<Func<TTarget,TValue>>        field,
			[InstantHandle] Expression<Func<TSource,TValue>>        value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));
			if (value  == null) throw new ArgumentNullException(nameof(value));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				query.Expression, Expression.Quote(field), Expression.Quote(value));

			var q = query.Provider.CreateQuery<TSource>(expr);
			return new SelectInsertable<TSource,TTarget>(q);
		}

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TTarget">Target record type</typeparam>
		/// <typeparam name="TValue">Field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource,TTarget> Value<TSource,TTarget,TValue>(
			                this ISelectInsertable<TSource,TTarget> source,
			[InstantHandle] Expression<Func<TTarget,TValue>>        field,
			[InstantHandle] Expression<Func<TValue>>                value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));
			if (value  == null) throw new ArgumentNullException(nameof(value));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;
			var expr = Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Value, source, field, value),
					query.Expression, Expression.Quote(field), Expression.Quote(value));

			var q = query.Provider.CreateQuery<TSource>(expr);
			return new SelectInsertable<TSource,TTarget>(q);
		}

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TTarget">Target record type</typeparam>
		/// <typeparam name="TValue">Field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource,TTarget> Value<TSource,TTarget,TValue>(
			                this ISelectInsertable<TSource,TTarget> source,
			[InstantHandle] Expression<Func<TTarget,TValue>>        field,
			TValue                                                  value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TValue)));

			var q = query.Provider.CreateQuery<TSource>(expr);
			return new SelectInsertable<TSource,TTarget>(q);
		}

		/// <summary>
		/// Executes configured insert query.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<TSource,TTarget>(this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Insert.SI.Insert.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes configured insert query asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<TSource,TTarget>(
			 this ISelectInsertable<TSource,TTarget> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Insert.SI.Insert.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static object InsertWithIdentity<TSource,TTarget>( this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source),
				currentSource.Expression);

			return currentSource.Execute<object>(expr);
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static int? InsertWithInt32Identity<TSource,TTarget>( this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(
				InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static long? InsertWithInt64Identity<TSource,TTarget>( this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(
				InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static decimal? InsertWithDecimalIdentity<TSource,TTarget>( this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<object> InsertWithIdentityAsync<TSource,TTarget>(
			 this ISelectInsertable<TSource,TTarget> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source),
				currentSource.Expression);

			return currentSource.ExecuteAsync<object>(expr, token);
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<int?> InsertWithInt32IdentityAsync<TSource,TTarget>(
			 this ISelectInsertable<TSource,TTarget> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<long?> InsertWithInt64IdentityAsync<TSource,TTarget>(
			 this ISelectInsertable<TSource,TTarget> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<decimal?> InsertWithDecimalIdentityAsync<TSource,TTarget>(
			 this ISelectInsertable<TSource,TTarget> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		#endregion

		#endregion

		#region InsertOrUpdate

		static readonly MethodInfo _insertOrUpdateMethodInfo =
			MemberHelper.MethodOf(() => InsertOrUpdate<int>(null!,null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Inserts new record into target table or updates existing record if record with the same primary key value already exists in target table.
		/// When <c>null</c> value or expression without field setters passed to <paramref name="onDuplicateKeyUpdateSetter"/>, this method
		/// implements <c>INSERT IF NOT EXISTS</c> logic.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <returns>Number of affected records.</returns>
		public static int InsertOrUpdate<T>(
			                this ITable<T>          target,
			[InstantHandle] Expression<Func<T>>     insertSetter,
			[InstantHandle] Expression<Func<T,T?>>? onDuplicateKeyUpdateSetter)
			where T : notnull
		{
			if (target                     == null) throw new ArgumentNullException(nameof(target));
			if (insertSetter               == null) throw new ArgumentNullException(nameof(insertSetter));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_insertOrUpdateMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(insertSetter),
				onDuplicateKeyUpdateSetter != null ? Expression.Quote(onDuplicateKeyUpdateSetter) : Expression.Constant(null, typeof(Expression<Func<T, T>>)));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Asynchronously inserts new record into target table or updates existing record if record with the same primary key value already exists in target table.
		/// When <c>null</c> value or expression without field setters passed to <paramref name="onDuplicateKeyUpdateSetter"/>, this method
		/// implements <c>INSERT IF NOT EXISTS</c> logic.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertOrUpdateAsync<T>(
			                this ITable<T>          target,
			[InstantHandle] Expression<Func<T>>     insertSetter,
			[InstantHandle] Expression<Func<T,T?>>? onDuplicateKeyUpdateSetter,
			CancellationToken                       token = default)
			where T : notnull
		{
			if (target                     == null) throw new ArgumentNullException(nameof(target));
			if (insertSetter               == null) throw new ArgumentNullException(nameof(insertSetter));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_insertOrUpdateMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(insertSetter), onDuplicateKeyUpdateSetter != null ? Expression.Quote(onDuplicateKeyUpdateSetter) : Expression.Constant(null, typeof(Expression<Func<T, T>>)));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		static readonly MethodInfo _insertOrUpdateMethodInfo2 =
			MemberHelper.MethodOf(() => InsertOrUpdate<int>(null!,null!,null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Inserts new record into target table or updates existing record if record with the same key value already exists in target table.
		/// When <c>null</c> value or expression without field setters passed to <paramref name="onDuplicateKeyUpdateSetter"/>, this method
		/// implements <c>INSERT IF NOT EXISTS</c> logic.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <param name="keySelector">Key fields selector to specify what fields and values must be used as key fields for selection between insert and update operations.
		/// Expression supports only target table record new expression with field initializers for each key field. Assigned key field value will be used as key value by operation type selector.</param>
		/// <returns>Number of affected records.</returns>
		public static int InsertOrUpdate<T>(
			                this ITable<T>          target,
			[InstantHandle] Expression<Func<T>>     insertSetter,
			[InstantHandle] Expression<Func<T,T?>>? onDuplicateKeyUpdateSetter,
			[InstantHandle] Expression<Func<T>>     keySelector)
			where T : notnull
		{
			if (target                     == null) throw new ArgumentNullException(nameof(target));
			if (insertSetter               == null) throw new ArgumentNullException(nameof(insertSetter));
			if (keySelector                == null) throw new ArgumentNullException(nameof(keySelector));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_insertOrUpdateMethodInfo2.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Quote(insertSetter),
				onDuplicateKeyUpdateSetter != null ? Expression.Quote(onDuplicateKeyUpdateSetter) : Expression.Constant(null, typeof(Expression<Func<T, T>>)),
				Expression.Quote(keySelector));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Asynchronously inserts new record into target table or updates existing record if record with the same key value already exists in target table.
		/// When <c>null</c> value or expression without field setters passed to <paramref name="onDuplicateKeyUpdateSetter"/>, this method
		/// implements <c>INSERT IF NOT EXISTS</c> logic.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <param name="keySelector">Key fields selector to specify what fields and values must be used as key fields for selection between insert and update operations.
		/// Expression supports only target table record new expression with field initializers for each key field. Assigned key field value will be used as key value by operation type selector.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertOrUpdateAsync<T>(
			                this ITable<T>          target,
			[InstantHandle] Expression<Func<T>>     insertSetter,
			[InstantHandle] Expression<Func<T,T?>>? onDuplicateKeyUpdateSetter,
			[InstantHandle] Expression<Func<T>>     keySelector,
			CancellationToken                       token = default)
			where T : notnull
		{
			if (target                     == null) throw new ArgumentNullException(nameof(target));
			if (insertSetter               == null) throw new ArgumentNullException(nameof(insertSetter));
			if (keySelector                == null) throw new ArgumentNullException(nameof(keySelector));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_insertOrUpdateMethodInfo2.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Quote(insertSetter),
				onDuplicateKeyUpdateSetter != null ? Expression.Quote(onDuplicateKeyUpdateSetter) : Expression.Constant(null, typeof(Expression<Func<T, T>>)),
				Expression.Quote(keySelector));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Drop

		static readonly MethodInfo _dropMethodInfo2 = MemberHelper.MethodOf(() => Drop<int>(null!, true)).GetGenericMethodDefinition();

		/// <summary>
		/// Drops database table.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Dropped table.</param>
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently catched and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static int Drop<T>( this ITable<T> target, bool throwExceptionIfNotExists = true)
			where T : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_dropMethodInfo2.MakeGenericMethod(typeof(T)),
				currentSource.Expression, ExpressionInstances.Boolean(throwExceptionIfNotExists));

			try
			{
				return currentSource.Execute<int>(expr);
			}
			catch when (!throwExceptionIfNotExists)
			{
			}

			return 0;
		}

		/// <summary>
		/// Drops database table asynchronously.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Dropped table.</param>
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently catched and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static async Task<int> DropAsync<T>(
			this ITable<T>    target,
			bool              throwExceptionIfNotExists = true,
			CancellationToken token = default)
			where T : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_dropMethodInfo2.MakeGenericMethod(typeof(T)),
				currentSource.Expression, ExpressionInstances.Boolean(throwExceptionIfNotExists));

			try
			{
				return await currentSource.ExecuteAsync<int>(expr, token).ConfigureAwait(false);
			}
			catch when (!throwExceptionIfNotExists)
			{
			}

			return 0;
		}

		#endregion

		#region Truncate

		static readonly MethodInfo _truncateMethodInfo = MemberHelper.MethodOf(() => Truncate<int>(null!, true)).GetGenericMethodDefinition();

		/// <summary>
		/// Truncates database table.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Truncated table.</param>
		/// <param name="resetIdentity">Performs reset identity column.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static int Truncate<T>( this ITable<T> target, bool resetIdentity = true)
			where T : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_truncateMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression, ExpressionInstances.Boolean(resetIdentity));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Truncates database table asynchronously.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Truncated table.</param>
		/// <param name="resetIdentity">Performs reset identity column.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static Task<int> TruncateAsync<T>(
			this ITable<T>    target,
			bool              resetIdentity = true,
			CancellationToken token         = default)
			where T : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_truncateMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression, ExpressionInstances.Boolean(resetIdentity));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Take / Skip / ElementAt

		static readonly MethodInfo _takeMethodInfo = MemberHelper.MethodOf(() => Take<int>(null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Limits number of records, returned from query.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">Expression that defines number of records to select.</param>
		/// <returns>Query with limit applied.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Take<TSource>(
			           this IQueryable<TSource>   source,
			[InstantHandle] Expression<Func<int>> count)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (count  == null) throw new ArgumentNullException(nameof(count));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_takeMethodInfo.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(count));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		static readonly MethodInfo _takeMethodInfo2 = MemberHelper.MethodOf(() => Take<int>(null!,null!,TakeHints.Percent)).GetGenericMethodDefinition();

		/// <summary>
		/// Limits number of records, returned from query. Allows to specify TAKE clause hints.
		/// Using this method may cause runtime <see cref="LinqException"/> if take hints are not supported by database.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">Expression that defines SQL TAKE parameter value.</param>
		/// <param name="hints"><see cref="TakeHints"/> hints for SQL TAKE clause.</param>
		/// <returns>Query with limit applied.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Take<TSource>(
			                    this IQueryable<TSource>   source,
			[InstantHandle]          Expression<Func<int>> count,
			[SqlQueryDependent]      TakeHints             hints)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (count  == null) throw new ArgumentNullException(nameof(count));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_takeMethodInfo2.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(count), Expression.Constant(hints));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		static readonly MethodInfo _takeMethodInfo3 = MemberHelper.MethodOf(() => Take<int>(null!,0,TakeHints.Percent)).GetGenericMethodDefinition();

		/// <summary>
		/// Limits number of records, returned from query. Allows to specify TAKE clause hints.
		/// Using this method may cause runtime <see cref="LinqException"/> if take hints are not supported by database.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">SQL TAKE parameter value.</param>
		/// <param name="hints"><see cref="TakeHints"/> hints for SQL TAKE clause.</param>
		/// <returns>Query with limit applied.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Take<TSource>(
			      this IQueryable<TSource> source,
			                    int                 count,
			[SqlQueryDependent] TakeHints           hints)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_takeMethodInfo3.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, ExpressionInstances.Int32(count), Expression.Constant(hints));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		static readonly MethodInfo _skipMethodInfo = MemberHelper.MethodOf(() => Skip<int>(null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Ignores first N records from source query.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">Expression that defines number of records to skip.</param>
		/// <returns>Query without skipped records.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Skip<TSource>(
			           this IQueryable<TSource>   source,
			[InstantHandle] Expression<Func<int>> count)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (count  == null) throw new ArgumentNullException(nameof(count));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_skipMethodInfo.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(count));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Selects record at specified position from source query.
		/// If query doesn't return enough records, <see cref="InvalidOperationException"/> will be thrown.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <exception cref="InvalidOperationException">Source query doesn't have record with specified index.</exception>
		/// <returns>Record at specified position.</returns>
		[Pure]
		public static TSource ElementAt<TSource>(
			           this IQueryable<TSource>   source,
			[InstantHandle] Expression<Func<int>> index)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (index  == null) throw new ArgumentNullException(nameof(index));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.ElementAtLambda.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(index));

			return currentSource.Execute<TSource>(expr);
		}

		/// <summary>
		/// Selects record at specified position from source query asynchronously.
		/// If query doesn't return enough records, <see cref="InvalidOperationException"/> will be thrown.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <exception cref="InvalidOperationException">Source query doesn't have record with specified index.</exception>
		/// <returns>Record at specified position.</returns>
		[Pure]
		public static Task<TSource> ElementAtAsync<TSource>(
			           this IQueryable<TSource>   source,
			[InstantHandle] Expression<Func<int>> index,
			CancellationToken                     token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (index  == null) throw new ArgumentNullException(nameof(index));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.ElementAtLambda.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(index));

			return currentSource.ExecuteAsync<TSource>(expr, token);
		}

		static readonly MethodInfo _elementAtOrDefaultMethodInfo = MemberHelper.MethodOf(() => ElementAtOrDefault<int>(null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Selects record at specified position from source query.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <returns>Record at specified position or default value, if source query doesn't have record with such index.</returns>
		[Pure]
		public static TSource ElementAtOrDefault<TSource>(
			           this IQueryable<TSource>   source,
			[InstantHandle] Expression<Func<int>> index)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (index  == null) throw new ArgumentNullException(nameof(index));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_elementAtOrDefaultMethodInfo.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(index));

			return currentSource.Execute<TSource>(expr);
		}

		/// <summary>
		/// Selects record at specified position from source query asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Record at specified position or default value, if source query doesn't have record with such index.</returns>
		[Pure]
		public static Task<TSource> ElementAtOrDefaultAsync<TSource>(
			           this IQueryable<TSource>   source,
			[InstantHandle] Expression<Func<int>> index,
			                CancellationToken     token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (index  == null) throw new ArgumentNullException(nameof(index));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_elementAtOrDefaultMethodInfo.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(index));

			return currentSource.ExecuteAsync<TSource>(expr, token);
		}

		#endregion

		#region Having

		static readonly MethodInfo _setMethodInfo7 = MemberHelper.MethodOf(() => Having((IQueryable<int>)null!,null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Filters source query using HAVING SQL clause.
		/// In general you don't need to use this method as linq2db is able to propely identify current context for
		/// <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/> method and generate
		/// HAVING clause.
		/// <a href="https://github.com/linq2db/linq2db/issues/133">More details</a>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query to filter.</param>
		/// <param name="predicate">Filtering expression.</param>
		/// <returns>Filtered query.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Having<TSource>(
			                this IQueryable<TSource>       source,
			[InstantHandle] Expression<Func<TSource,bool>> predicate)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_setMethodInfo7.MakeGenericMethod(typeof(TSource)),
				currentSource.Expression, Expression.Quote(predicate));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region IOrderedQueryable

		/// <summary>
		/// Adds ascending sort expression to a query.
		/// If query already sorted, existing sorting will be preserved and updated with new sort.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TKey">Sort expression type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Sort expression selector.</param>
		/// <returns>Sorted query.</returns>
		[LinqTunnel]
		[Pure]
		public static IOrderedQueryable<TSource> ThenOrBy<TSource, TKey>(
			           this IQueryable<TSource>            source,
			[InstantHandle] Expression<Func<TSource,TKey>> keySelector)
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenOrBy, source, keySelector),
				currentSource.Expression, Expression.Quote(keySelector));

			return (IOrderedQueryable<TSource>)currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Adds descending sort expression to a query.
		/// If query already sorted, existing sorting will be preserved and updated with new sort.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TKey">Sort expression type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Sort expression selector.</param>
		/// <returns>Sorted query.</returns>
		[LinqTunnel]
		[Pure]
		public static IOrderedQueryable<TSource> ThenOrByDescending<TSource, TKey>(
			           this IQueryable<TSource>             source,
			[InstantHandle] Expression<Func<TSource, TKey>> keySelector)
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenOrByDescending, source, keySelector),
				currentSource.Expression, Expression.Quote(keySelector));

			return (IOrderedQueryable<TSource>)currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Removes ordering from current query.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <returns>Unsorted query.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> RemoveOrderBy<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(RemoveOrderBy, source), currentSource.Expression);

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Stub helpers

		internal static TOutput AsQueryable<TOutput,TInput>(TInput source)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region SqlJoin

		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="joinType">Type of join.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> Join<TSource>(
			           this     IQueryable<TSource>             source,
			[SqlQueryDependent] SqlJoinType                     joinType,
			[InstantHandle]     Expression<Func<TSource, bool>> predicate)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Join, source, joinType, predicate),
				currentSource.Expression,
				Expression.Constant(joinType),
				Expression.Quote(predicate));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="joinType">Type of join.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> Join<TOuter, TInner, TResult>(
			               this IQueryable<TOuter>                        outer,
			                    IQueryable<TInner>                        inner,
			[SqlQueryDependent] SqlJoinType                               joinType,
			[InstantHandle]     Expression<Func<TOuter, TInner, bool>>    predicate,
			[InstantHandle]     Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			if (outer          == null) throw new ArgumentNullException(nameof(outer));
			if (inner          == null) throw new ArgumentNullException(nameof(inner));
			if (predicate      == null) throw new ArgumentNullException(nameof(predicate));
			if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

			var currentSource = outer.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.JoinTypePredicateSelector.MakeGenericMethod(typeof(TOuter), typeof(TInner), typeof(TResult)),
				currentSource.Expression,
				inner.Expression,
				Expression.Constant(joinType),
				Expression.Quote(predicate),
				Expression.Quote(resultSelector));

			return currentSource.Provider.CreateQuery<TResult>(expr);
		}

		/// <summary>
		/// Defines inner join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> InnerJoin<TSource>(
			           this IQueryable<TSource>             source,
			[InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Inner, predicate);
		}

		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> InnerJoin<TOuter, TInner, TResult>(
			           this IQueryable<TOuter>                        outer,
			                IQueryable<TInner>                        inner,
			[InstantHandle] Expression<Func<TOuter, TInner, bool>>    predicate,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Inner, predicate, resultSelector);
		}

		/// <summary>
		/// Defines left outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> LeftJoin<TSource>(
			           this IQueryable<TSource>             source,
			[InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Left, predicate);
		}

		/// <summary>
		/// Defines left outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> LeftJoin<TOuter, TInner, TResult>(
			           this IQueryable<TOuter>                        outer,
			                IQueryable<TInner>                        inner,
			[InstantHandle] Expression<Func<TOuter, TInner, bool>>    predicate,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Left, predicate, resultSelector);
		}

		/// <summary>
		/// Defines right outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> RightJoin<TSource>(
			           this IQueryable<TSource>             source,
			[InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Right, predicate);
		}

		/// <summary>
		/// Defines right outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> RightJoin<TOuter, TInner, TResult>(
			           this IQueryable<TOuter>                        outer,
			                IQueryable<TInner>                        inner,
			[InstantHandle] Expression<Func<TOuter, TInner, bool>>    predicate,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Right, predicate, resultSelector);
		}

		/// <summary>
		/// Defines full outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> FullJoin<TSource>(
			           this IQueryable<TSource>             source,
			[InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Full, predicate);
		}

		/// <summary>
		/// Defines full outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> FullJoin<TOuter, TInner, TResult>(
			           this IQueryable<TOuter>                        outer,
			                IQueryable<TInner>                        inner,
			[InstantHandle] Expression<Func<TOuter, TInner, bool>>    predicate,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Full, predicate, resultSelector);
		}

		/// <summary>
		/// Defines cross join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> CrossJoin<TOuter, TInner, TResult>(
			           this IQueryable<TOuter>                        outer,
			                IQueryable<TInner>                        inner,
			[InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			if (outer          == null) throw new ArgumentNullException(nameof(outer));
			if (inner          == null) throw new ArgumentNullException(nameof(inner));
			if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

			var currentSource = outer.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(CrossJoin, outer, inner, resultSelector),
				currentSource.Expression,
				inner.Expression,
				Expression.Quote(resultSelector));

			return currentSource.Provider.CreateQuery<TResult>(expr);
		}

		#endregion

		#region CTE

		internal static IQueryable<T> AsCte<T>(IQueryable<T> cteTable, IQueryable<T> cteBody, string? tableName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Specifies a temporary named result set, known as a common table expression (CTE).
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <returns>Common table expression.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> AsCte<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsCte, source),
				currentSource.Expression);

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Specifies a temporary named result set, known as a common table expression (CTE).
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="name">Common table expression name.</param>
		/// <returns>Common table expression.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> AsCte<TSource>(
			this IQueryable<TSource> source,
			string?                  name)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsCte, source, name),
				currentSource.Expression, Expression.Constant(name ?? string.Empty));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region AsQueryable

		/// <summary>Converts a generic <see cref="IEnumerable{T}" /> to Linq To DB query.</summary>
		/// <param name="source">A sequence to convert.</param>
		/// <param name="dataContext">Database connection context.</param>
		/// <typeparam name="TElement">The type of the elements of <paramref name="source" />.</typeparam>
		/// <returns>An <see cref="IQueryable{T}" /> that represents the input sequence.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="source" /> is <see langword="null" />.</exception>
		public static IQueryable<TElement> AsQueryable<TElement>(
			this IEnumerable<TElement> source,
			IDataContext               dataContext)
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			if (source is IQueryable<TElement> already)
				return (IQueryable<TElement>)(ProcessSourceQueryable?.Invoke(already) ?? already);

			var query = new ExpressionQueryImpl<TElement>(dataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsQueryable, source, dataContext),
					Expression.Constant(source),
					SqlQueryRootExpression.Create(dataContext)
				));

			return query;
		}

		#endregion

		#region AsSubQuery

		/// <summary>
		/// Defines that sub-query is mandatory for <paramref name="source"/> query and cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> AsSubQuery<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSubQuery, source), source.Expression);

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Defines that sub-query is mandatory for <paramref name="grouping"/> query and cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TKey">The type of the key of the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <typeparam name="TElement">The type of the values in the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <param name="grouping">Source data query.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TKey> AsSubQuery<TKey, TElement>(this IQueryable<IGrouping<TKey,TElement>> grouping)
		{
			if (grouping == null) throw new ArgumentNullException(nameof(grouping));

			var currentSource = grouping.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSubQuery, grouping),
				currentSource.Expression);

			return currentSource.Provider.CreateQuery<TKey>(expr);
		}

		/// <summary>
		/// Defines that sub-query is mandatory for <paramref name="source"/> query and cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="queryName">Query name.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> AsSubQuery<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] string queryName)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSubQuery, source, queryName),
				currentSource.Expression,
				Expression.Constant(queryName));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Defines that sub-query is mandatory for <paramref name="grouping"/> query and cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TKey">The type of the key of the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <typeparam name="TElement">The type of the values in the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <param name="grouping">Source data query.</param>
		/// <param name="queryName">Query name.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TKey> AsSubQuery<TKey, TElement>(this IQueryable<IGrouping<TKey,TElement>> grouping, [SqlQueryDependent] string queryName)
		{
			if (grouping == null) throw new ArgumentNullException(nameof(grouping));

			var currentSource = grouping.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(AsSubQuery, grouping, queryName),
				currentSource.Expression,
				Expression.Constant(queryName));

			return currentSource.Provider.CreateQuery<TKey>(expr);
		}

		#endregion

		#region QueryName

		/// <summary>
		/// Defines query name for specified sub-query. The query cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="queryName">Query name.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> QueryName<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] string queryName)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(QueryName, source, queryName),
				currentSource.Expression,
				Expression.Constant(queryName));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Defines query name for specified sub-query. The query cannot be removed during the query optimization.
		/// </summary>
		/// <typeparam name="TKey">The type of the key of the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <typeparam name="TElement">The type of the values in the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <param name="grouping">Source data query.</param>
		/// <param name="queryName">Query name.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TKey> QueryName<TKey,TElement>(this IQueryable<IGrouping<TKey,TElement>> grouping, [SqlQueryDependent] string queryName)
		{
			if (grouping == null) throw new ArgumentNullException(nameof(grouping));

			var currentSource = grouping.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(QueryName, grouping, queryName),
				currentSource.Expression,
				Expression.Constant(queryName));

			return currentSource.Provider.CreateQuery<TKey>(expr);
		}

		#endregion

		#region InlineParameters

		/// <summary>
		/// Inline parameters in query which can be converted to SQL Literal.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <returns>Query with inlined parameters.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> InlineParameters<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InlineParameters, source),
				currentSource.Expression);

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Disable Grouping Guard

		/// <summary>
		/// Disables grouping guard for particular <paramref name="grouping"/> query.
		/// </summary>
		/// <typeparam name="TKey">The type of the key of the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <typeparam name="TElement">The type of the values in the <see cref="IGrouping{TKey, TElement}" />.</typeparam>
		/// <param name="grouping">Source data query.</param>
		/// <returns>Query with suppressed grouping guard.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<IGrouping<TKey, TElement>> DisableGuard<TKey, TElement>(this IQueryable<IGrouping<TKey, TElement>> grouping)
		{
			if (grouping == null) throw new ArgumentNullException(nameof(grouping));

			var currentSource = grouping.ProcessIQueryable();
			
			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DisableGuard, grouping),
				currentSource.Expression);

			return currentSource.Provider.CreateQuery<IGrouping<TKey, TElement>>(expr);
		}

		#endregion

		#region HasUniqueKey

		/// <summary>
		/// Records unique key for IQueryable. It allows sub-query to be optimized out in LEFT JOIN if columns from sub-query are not used in final projection and predicate.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TKey">Key type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="keySelector">A function to specify which fields are unique.</param>
		/// <returns>Query converted into sub-query.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> HasUniqueKey<TSource, TKey>(
			 this IQueryable<TSource>             source,
			      Expression<Func<TSource, TKey>> keySelector)
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(HasUniqueKey, source, keySelector),
				currentSource.Expression,
				Expression.Quote(keySelector));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Set operators

		static Expression GetSourceExpression<TSource>(IEnumerable<TSource> source)
		{
			if (source is IQueryable<TSource> queryable)
				return queryable.Expression;
			return Expression.Constant(source, typeof (IEnumerable<TSource>));
		}

		/// <summary>Concatenates two sequences, similar to <see cref="Queryable.Concat{TSource}"/>.</summary>
		/// <param name="source1">The first sequence to concatenate.</param>
		/// <param name="source2">The sequence to concatenate to the first sequence.</param>
		/// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
		/// <returns>An <see cref="IQueryable{T}" /> that contains the concatenated elements of the two input sequences.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
		public static IQueryable<TSource> UnionAll<TSource>(
			 this IQueryable<TSource>  source1,
			      IEnumerable<TSource> source2)
		{
			if (source1 == null) throw new ArgumentNullException(nameof(source1));
			if (source2 == null) throw new ArgumentNullException(nameof(source2));

			return source1.Concat(source2);
		}

		/// <summary>Produces the set difference of two sequences.</summary>
		/// <param name="source1">An <see cref="IQueryable{T}" /> whose elements that are not also in <paramref name="source2" /> will be returned.</param>
		/// <param name="source2">An <see cref="IEnumerable{T}" /> whose elements that also occur in the first sequence will not appear in the returned sequence.</param>
		/// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
		/// <returns>An <see cref="IQueryable{T}" /> that contains the set difference of the two sequences.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
		public static IQueryable<TSource> ExceptAll<TSource>(
			 this IQueryable<TSource>  source1,
			      IEnumerable<TSource> source2)
		{
			if (source1 == null) throw new ArgumentNullException(nameof(source1));
			if (source2 == null) throw new ArgumentNullException(nameof(source2));

			var currentSource = source1.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ExceptAll, source1, source2),
				currentSource.Expression,
				GetSourceExpression(source2));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>Produces the set intersection of two sequences.</summary>
		/// <param name="source1">A sequence whose elements that also appear in <paramref name="source2" /> are returned.</param>
		/// <param name="source2">A sequence whose elements that also appear in the first sequence are returned.</param>
		/// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
		/// <returns>A sequence that contains the set intersection of the two sequences.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="source1" /> or <paramref name="source2" /> is <see langword="null" />.</exception>
		public static IQueryable<TSource> IntersectAll<TSource>(
			 this IQueryable<TSource>  source1,
			      IEnumerable<TSource> source2)
		{
			if (source1 == null) throw new ArgumentNullException(nameof(source1));
			if (source2 == null) throw new ArgumentNullException(nameof(source2));

			var currentSource = source1.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(IntersectAll, source1, source2),
				currentSource.Expression,
				GetSourceExpression(source2));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Query Filters

		/// <summary>
		/// Disables Query Filters in current query.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="entityTypes">Optional types with which filters should be disabled.</param>
		/// <returns>Query with disabled filters.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> IgnoreFilters<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] params Type[] entityTypes)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(IgnoreFilters, source, entityTypes), currentSource.Expression, Expression.Constant(entityTypes));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion

		#region Tests

		/// <summary>
		/// Generates test source code for specified query.
		/// This method could be usefull to debug queries and attach test code to linq2db issue reports.
		/// </summary>
		/// <param name="query">Query to test.</param>
		/// <param name="mangleNames">Should we use real names for used types, members and namespace or generate obfuscated names.</param>
		/// <returns>Test source code.</returns>
		public static string GenerateTestString<T>(this IQueryable<T> query, bool mangleNames = false)
		{
			return new ExpressionTestGenerator(mangleNames, Internals.GetDataContext(query) ?? throw new ArgumentException("Query is not a Linq To DB query", nameof(query)))
				.GenerateSourceString(query.Expression);
		}

		#endregion

		#region Queryable Helpers

		/// <summary>
		/// Gets or sets callback for preprocessing query before execution.
		/// Useful for intercepting queries.
		/// </summary>
		public static Func<IQueryable,IQueryable>? ProcessSourceQueryable { get; set; }

		public static IExtensionsAdapter? ExtensionsAdapter { get; set; }

		#endregion

		#region Eager Loading helpers

		/// <summary>
		/// Marks SelectQuery as Distinct.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <returns>Distinct query.</returns>
		[LinqTunnel]
		[Pure]
		internal static IQueryable<TSource> SelectDistinct<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(SelectDistinct, source), currentSource.Expression);

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		#endregion;

		#region Tag

		/// <summary>
		/// Adds a tag comment before generated query.
		/// <code>
		/// The example below will produce following code before generated query: /* my tag */\r\n
		/// db.Table.TagQuery("my tag");
		/// </code>
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="tagValue">Tag text to be added as comment before generated query.</param>
		/// <returns>Query with tag.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> TagQuery<TSource>(this IQueryable<TSource> source, [SqlQueryDependent] string tagValue)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (tagValue == null) throw new ArgumentNullException(nameof(tagValue));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TagQuery, source, tagValue),
				source.Expression,
				Expression.Constant(tagValue));

			return currentSource.Provider.CreateQuery<TSource>(expr);
		}

		/// <summary>
		/// Adds a tag comment before generated query for table.
		/// <code>
		/// The example below will produce following code before generated query: /* my tag */\r\n
		/// db.Table.TagQuery("my tag");
		/// </code>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="tagValue">Tag text to be added as comment before generated query.</param>
		/// <returns>Table-like query source with tag.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> TagQuery<T>(this ITable<T> table, [SqlQueryDependent] string tagValue) where T : notnull
		{
			if (table == null) throw new ArgumentNullException(nameof(table));
			if (tagValue == null) throw new ArgumentNullException(nameof(tagValue));

			var newTable = new Table<T>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TagQuery, table, tagValue),
					table.Expression, Expression.Constant(tagValue))
			);

			return newTable;
		}

		#endregion

		#region Helpers
		internal static IQueryable<T> ProcessIQueryable<T>(this IQueryable<T> source)
		{
			return (IQueryable<T>)(ProcessSourceQueryable?.Invoke(source) ?? source);
		}

		internal static IQueryProviderAsync GetLinqToDBSource<T>(this IQueryable<T> source, [CallerMemberName] string? method = null)
		{
			if (source.ProcessIQueryable() is not IQueryProviderAsync query)
				return ThrowInvalidSource(method);

			return query;
		}

		[DoesNotReturn]
		private static IQueryProviderAsync ThrowInvalidSource(string? method)
		{
			throw new LinqException($"LinqToDB method '{method}' called on non-LinqToDB IQueryable.");
		}
		#endregion
	}
}
