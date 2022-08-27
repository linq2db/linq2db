﻿using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlCe
{
	using Expressions;
	using Linq;
	using SqlProvider;

	public static partial class SqlCeHints
	{
		public static class Table
		{
			public const string HoldLock = "HoldLock";
			public const string NoLock   = "NoLock";
			public const string PagLock  = "PagLock";
			public const string RowLock  = "RowLock";
			public const string TabLock  = "TabLock";
			public const string UpdLock  = "UpdLock";
			public const string XLock    = "XLock";
			public const string Index    = "Index";
		}

		#region SqlCeSpecific Hints

		[ExpressionMethod(nameof(WithIndexImpl))]
		public static ISqlCeSpecificTable<TSource> WithIndex<TSource>(this ISqlCeSpecificTable<TSource> table, string indexName)
			where TSource : notnull
		{
			return TableHint(table, Table.Index, indexName);
		}

		static Expression<Func<ISqlCeSpecificTable<TSource>,string,ISqlCeSpecificTable<TSource>>> WithIndexImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexName) => table.TableHint(Table.Index, indexName);
		}

		[ExpressionMethod(nameof(WithIndex2Impl))]
		public static ISqlCeSpecificTable<TSource> WithIndex<TSource>(this ISqlCeSpecificTable<TSource> table, params string[] indexNames)
			where TSource : notnull
		{
			return table.TableHint(Table.Index, indexNames);
		}

		static Expression<Func<ISqlCeSpecificTable<TSource>,string[],ISqlCeSpecificTable<TSource>>> WithIndex2Impl<TSource>()
			where TSource : notnull
		{
			return (table, indexNames) => table.TableHint(Table.Index, indexNames);
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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.SqlCe, Sql.QueryExtensionScope.TableHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlCeSpecificTable<TSource> TableHint<TSource>(this ISqlCeSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TableHint, table, hint),
				table.Expression, Expression.Constant(hint));

			return table;
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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.SqlCe, Sql.QueryExtensionScope.TableHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlCeSpecificTable<TSource> TableHint<TSource,TParam>(
			this ISqlCeSpecificTable<TSource> table,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] TParam            hintParameter)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameter),
				table.Expression, Expression.Constant(hint), Expression.Constant(hintParameter));

			return table;
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
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.SqlCe, Sql.QueryExtensionScope.TableHint, typeof(HintWithParametersExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static ISqlCeSpecificTable<TSource> TableHint<TSource,TParam>(
			this ISqlCeSpecificTable<TSource> table,
			[SqlQueryDependent] string            hint,
			[SqlQueryDependent] params TParam[]   hintParameters)
			where TSource : notnull
		{
			table.Expression = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameters),
				table.Expression,
				Expression.Constant(hint),
				Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p, typeof(TParam)))));

			return table;
		}

		#endregion

		#region TablesInScopeHint

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.SqlCe, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static ISqlCeSpecificQueryable<TSource> TablesInScopeHint<TSource>(
			this ISqlCeSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new SqlCeSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameter">Table hint parameter.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.SqlCe, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintWithParameterExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static ISqlCeSpecificQueryable<TSource> TablesInScopeHint<TSource,TParam>(
			this ISqlCeSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint,
			[SqlQueryDependent] TParam                hintParameter)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new SqlCeSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint, hintParameter),
					currentSource.Expression, Expression.Constant(hint), Expression.Constant(hintParameter))));
		}

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.SqlCe, Sql.QueryExtensionScope.TablesInScopeHint, typeof(HintWithParametersExtensionBuilder))]
		[Sql.QueryExtension(null,               Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static ISqlCeSpecificQueryable<TSource> TablesInScopeHint<TSource>(
			this ISqlCeSpecificQueryable<TSource> source,
			[SqlQueryDependent] string                hint,
			[SqlQueryDependent] params object[]       hintParameters)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new SqlCeSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.NewArrayInit(typeof(object), hintParameters.Select(Expression.Constant)))));
		}

		#endregion
	}
}
