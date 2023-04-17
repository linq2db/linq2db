using System;
using System.Linq.Expressions;
using System.Text;
using JetBrains.Annotations;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Expressions;
	using Linq;
	using SqlProvider;

	public static class ClickHouseHints
	{
		public static class Table
		{
			public const string Final = "FINAL";
		}

		#region TableHint

		sealed class TableHintExtensionBuilder : ISqlTableExtensionBuilder
		{
			void ISqlTableExtensionBuilder.Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension, SqlTable table, string alias)
			{
				if (stringBuilder.Length > 0 && stringBuilder[^1] != ' ')
					stringBuilder.Append(' ');

				var args = sqlQueryExtension.Arguments;
				var hint = (SqlValue)args["hint"];

				stringBuilder.Append((string)hint.Value!);
			}
		}

		/// <summary>
		/// Adds a table hint to a table in generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.TableHint, typeof(TableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		public static IClickHouseSpecificTable<TSource> TableHint<TSource>(this IClickHouseSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TableHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return new ClickHouseSpecificTable<TSource>(newTable);
		}

//		/// <summary>
//		/// Adds a table hint to a table in generated query.
//		/// </summary>
//		/// <typeparam name="TSource">Table record mapping class.</typeparam>
//		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
//		/// <param name="table">Table-like query source.</param>
//		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
//		/// <param name="hintParameter">Table hint parameter.</param>
//		/// <returns>Table-like query source with table hints.</returns>
//		[LinqTunnel, Pure]
//		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.TableHint, typeof(TableHintExtensionBuilder))]
//		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
//		public static IClickHouseSpecificTable<TSource> TableHint<TSource,TParam>(
//			this IClickHouseSpecificTable<TSource> table,
//			[SqlQueryDependent] string        hint,
//			[SqlQueryDependent] TParam        hintParameter)
//			where TSource : notnull
//		{
//			var newTable = new Table<TSource>(table.DataContext,
//				Expression.Call(
//					null,
//					MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameter),
//					table.Expression, Expression.Constant(hint), Expression.Constant(hintParameter))
//			);
//
//			return new ClickHouseSpecificTable<TSource>(newTable);
//		}

//		/// <summary>
//		/// Adds a table hint to a table in generated query.
//		/// </summary>
//		/// <typeparam name="TSource">Table record mapping class.</typeparam>
//		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
//		/// <param name="table">Table-like query source.</param>
//		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
//		/// <param name="hintParameters">Table hint parameters.</param>
//		/// <returns>Table-like query source with table hints.</returns>
//		[LinqTunnel, Pure]
//		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.TableHint, typeof(TableSpecHintExtensionBuilder), " ", ", ")]
//		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
//		public static IClickHouseSpecificTable<TSource> TableHint<TSource,TParam>(
//			this IClickHouseSpecificTable<TSource>   table,
//			[SqlQueryDependent] string          hint,
//			[SqlQueryDependent] params TParam[] hintParameters)
//			where TSource : notnull
//		{
//			var newTable = new Table<TSource>(table.DataContext,
//				Expression.Call(
//					null,
//					MethodHelper.GetMethodInfo(TableHint, table, hint, hintParameters),
//					table.Expression,
//					Expression.Constant(hint),
//					Expression.NewArrayInit(typeof(TParam), hintParameters.Select(p => Expression.Constant(p, typeof(TParam)))))
//			);
//
//			return new ClickHouseSpecificTable<TSource>(newTable);
//		}

		#endregion

		#region TablesInScopeHint

		/// <summary>
		/// Adds a table hint to all the tables in the method scope.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with table hints.</returns>
		[LinqTunnel, Pure]
		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		public static IClickHouseSpecificQueryable<TSource> TablesInScopeHint<TSource>(this IClickHouseSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return new ClickHouseSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

//		/// <summary>
//		/// Adds a table hint to all the tables in the method scope.
//		/// </summary>
//		/// <typeparam name="TSource">Table record mapping class.</typeparam>
//		/// <typeparam name="TParam">Table hint parameter type.</typeparam>
//		/// <param name="source">Query source.</param>
//		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
//		/// <param name="hintParameter">Table hint parameter.</param>
//		/// <returns>Query source with table hints.</returns>
//		[LinqTunnel, Pure]
//		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableHintExtensionBuilder))]
//		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
//		public static IClickHouseSpecificQueryable<TSource> TablesInScopeHint<TSource,TParam>(
//			this IClickHouseSpecificQueryable<TSource> source,
//			[SqlQueryDependent] string            hint,
//			[SqlQueryDependent] TParam            hintParameter)
//			where TSource : notnull
//		{
//			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;
//
//			return new ClickHouseSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
//				Expression.Call(
//					null,
//					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint, hintParameter),
//					currentSource.Expression, Expression.Constant(hint), Expression.Constant(hintParameter))));
//		}

//		/// <summary>
//		/// Adds a table hint to all the tables in the method scope.
//		/// </summary>
//		/// <typeparam name="TSource">Table record mapping class.</typeparam>
//		/// <param name="source">Query source.</param>
//		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
//		/// <param name="hintParameters">Table hint parameters.</param>
//		/// <returns>Query source with table hints.</returns>
//		[LinqTunnel, Pure]
//		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableSpecHintExtensionBuilder), " ", ", ")]
//		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
//		public static IClickHouseSpecificQueryable<TSource> TablesInScopeHint<TSource>(
//			this IClickHouseSpecificQueryable<TSource> source,
//			[SqlQueryDependent] string            hint,
//			[SqlQueryDependent] params object[]   hintParameters)
//			where TSource : notnull
//		{
//			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;
//
//			return new ClickHouseSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
//				Expression.Call(
//					null,
//					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint, hintParameters),
//					currentSource.Expression,
//					Expression.Constant(hint),
//					Expression.NewArrayInit(typeof(object), hintParameters.Select(Expression.Constant)))));
//		}

		#endregion

		[ExpressionMethod(ProviderName.ClickHouse, nameof(FinalHintImpl))]
		public static IClickHouseSpecificTable<TSource> FinalHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return TableHint(table, Table.Final);
		}
		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> FinalHintImpl<TSource>()
			where TSource : notnull
		{
			return table => TableHint(table, Table.Final);
		}

		[ExpressionMethod(ProviderName.ClickHouse, nameof(FinalInScopeHintImpl2))]
		public static IClickHouseSpecificTable<TSource> FinalInScopeHint<TSource>(this IClickHouseSpecificTable<TSource> table)
			where TSource : notnull
		{
			return TableHint(table, Table.Final);
		}
		static Expression<Func<IClickHouseSpecificTable<TSource>,IClickHouseSpecificTable<TSource>>> FinalInScopeHintImpl2<TSource>()
			where TSource : notnull
		{
			return table => TableHint(table, Table.Final);
		}

		[ExpressionMethod(ProviderName.ClickHouse, nameof(FinalInScopeHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> FinalInScopeHint<TSource>(this IClickHouseSpecificQueryable<TSource> table)
			where TSource : notnull
		{
			return TablesInScopeHint(table, Table.Final);
		}
		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> FinalInScopeHintImpl<TSource>()
			where TSource : notnull
		{
			return table => TablesInScopeHint(table, Table.Final);
		}
	}
}
