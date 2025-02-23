using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.TableHint, typeof(HintWithParameterExtensionBuilder))]
		public static ITable<TSource> TableHint<TSource, TParam>(
			this ITable<TSource> table,
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder), " ", ", ")]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.TableHint, typeof(HintWithParametersExtensionBuilder))]
		public static ITable<TSource> TableHint<TSource, TParam>(
			this ITable<TSource> table,
			[SqlQueryDependent] string hint,
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder))]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintWithParameterExtensionBuilder))]
		public static IQueryable<TSource> TablesInScopeHint<TSource, TParam>(
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder), " ", ", ")]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintWithParametersExtensionBuilder))]
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.IndexHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.IndexHint, typeof(HintExtensionBuilder))]
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.IndexHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.IndexHint, typeof(HintWithParameterExtensionBuilder))]
		public static ITable<TSource> IndexHint<TSource, TParam>(
			this ITable<TSource> table,
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
		[Sql.QueryExtension(ProviderName.MySql, Sql.QueryExtensionScope.IndexHint, typeof(HintWithParametersExtensionBuilder))]
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.IndexHint, typeof(HintWithParametersExtensionBuilder))]
		public static ITable<TSource> IndexHint<TSource, TParam>(
			this ITable<TSource> table,
			[SqlQueryDependent] string hint,
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
		public static IQueryable<TSource> SubQueryHint<TSource, TParam>(
			this IQueryable<TSource> source,
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
			this IQueryable<TSource> source,
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
		public static IQueryable<TSource> QueryHint<TSource, TParam>(
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
		[Sql.QueryExtension(null, Sql.QueryExtensionScope.QueryHint, typeof(HintWithParametersExtensionBuilder))]
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
	}
}
