using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Expressions;
	using Linq;
	using SqlProvider;
	using SqlQuery;

	public static partial class ClickHouseHints
	{
		public static class Table
		{
			public const string Final = "FINAL";
		}

		public static class Join
		{
			public const string Outer       = "OUTER";
			public const string Semi        = "SEMI";
			public const string Anti        = "ANTI";
			public const string Any         = "ANY";
			public const string AsOf        = "ASOF";
			public const string Global      = "GLOBAL";
			public const string GlobalOuter = Global + " OUTER";
			public const string GlobalSemi  = Global + " SEMI";
			public const string GlobalAnti  = Global + " ANTI";
			public const string GlobalAny   = Global + " ANY";
			public const string GlobalAsOf  = Global + " ASOF";
			public const string All         = "ALL";
			public const string AllOuter    = All + " OUTER";
			public const string AllSemi     = All + " SEMI";
			public const string AllAnti     = All + " ANTI";
			public const string AllAny      = All + " ANY";
			public const string AllAsOf     = All + " ASOF";
		}

		public static class Query
		{
			public const string Settings = "SETTINGS";
		}

		#region TableHint

		sealed class TableHintExtensionBuilder : ISqlTableExtensionBuilder
		{
			void ISqlTableExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension, SqlTable table, string alias)
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
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.TableHint, typeof(TableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		internal static IClickHouseSpecificTable<TSource> TableHint<TSource>(this IClickHouseSpecificTable<TSource> table, [SqlQueryDependent] string hint)
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
		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.TablesInScopeHint, typeof(TableHintExtensionBuilder))]
		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,              typeof(NoneExtensionBuilder))]
		internal static IClickHouseSpecificQueryable<TSource> TablesInScopeHint<TSource>(this IClickHouseSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new ClickHouseSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(TablesInScopeHint, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		#endregion

		#region JoinHint

		/// <summary>
		/// Adds a join hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.JoinHint, typeof(NoneExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,     typeof(NoneExtensionBuilder))]
		internal static IClickHouseSpecificTable<TSource> JoinHint<TSource>(this IClickHouseSpecificTable<TSource> table, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var newTable = new Table<TSource>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(JoinHint, table, hint),
					table.Expression, Expression.Constant(hint))
			);

			return new ClickHouseSpecificTable<TSource>(newTable);
		}

		/// <summary>
		/// Adds a join hint to a generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with join hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.JoinHint, typeof(NoneExtensionBuilder))]
		[Sql.QueryExtension(null,                   Sql.QueryExtensionScope.None,     typeof(NoneExtensionBuilder))]
		internal static IClickHouseSpecificQueryable<TSource> JoinHint<TSource>(this IClickHouseSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new ClickHouseSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(JoinHint, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		#endregion

		#region SubQueryHint

		/// <summary>
		/// Adds a subquery hint to the generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <returns>Query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.SubQueryHint, typeof(HintExtensionBuilder))]
		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,         typeof(NoneExtensionBuilder))]
		internal static IClickHouseSpecificQueryable<TSource> SubQueryHint<TSource>(this IClickHouseSpecificQueryable<TSource> source, [SqlQueryDependent] string hint)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new ClickHouseSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SubQueryHint, source, hint),
					currentSource.Expression, Expression.Constant(hint))));
		}

		#endregion

		#region QueryHint

		/// <summary>
		/// Adds a query hint to the generated query.
		/// </summary>
		/// <typeparam name="TSource">Table record mapping class.</typeparam>
		/// <param name="source">Query source.</param>
		/// <param name="hint">SQL text, added as a database specific hint to generated query.</param>
		/// <param name="hintParameters">Table hint parameters.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel, Pure, IsQueryable]
		[Sql.QueryExtension(ProviderName.ClickHouse, Sql.QueryExtensionScope.QueryHint, typeof(HintWithFormatParametersExtensionBuilder), " ")]
		[Sql.QueryExtension(null,                    Sql.QueryExtensionScope.None,      typeof(NoneExtensionBuilder))]
		internal static IClickHouseSpecificQueryable<TSource> QueryHint<TSource>(
			this                       IClickHouseSpecificQueryable<TSource> source,
			[SqlQueryDependent]        string                                hint,
			[SqlQueryDependent]        string                                hintFormat,
			[SqlQueryDependent] params object?[]                             hintParameters)
			where TSource : notnull
		{
			var currentSource = source.ProcessIQueryable();

			return new ClickHouseSpecificQueryable<TSource>(currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(QueryHint, source, hint, hintFormat, hintParameters),
					currentSource.Expression,
					Expression.Constant(hint),
					Expression.Constant(hintFormat),
					Expression.NewArrayInit(typeof(object), hintParameters.Select(o => Expression.Constant(o, typeof(object)))))));
		}

		#endregion

		/// <summary>
		/// Adds <b>FINAL</b> modifier to FROM Clause.
		/// </summary>
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

		/// <summary>
		/// Adds <b>FINAL</b> modifier to FROM Clause.
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="table"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Adds <b>FINAL</b> modifier to FROM Clause of all the tables in the method scope.
		/// </summary>
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

		/// <summary>
		/// Adds <b>FINAL</b> modifier to FROM Clause of all the tables in the method scope.
		/// </summary>
		[ExpressionMethod(ProviderName.ClickHouse, nameof(FinalQueryHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> FinalHint<TSource>(this IClickHouseSpecificQueryable<TSource> table)
			where TSource : notnull
		{
			return SubQueryHint(table, Table.Final);
		}
		static Expression<Func<IClickHouseSpecificQueryable<TSource>,IClickHouseSpecificQueryable<TSource>>> FinalQueryHintImpl<TSource>()
			where TSource : notnull
		{
			return table => SubQueryHint(table, Table.Final);
		}

		[ExpressionMethod(ProviderName.ClickHouse, nameof(SettingsHintImpl))]
		public static IClickHouseSpecificQueryable<TSource> SettingsHint<TSource>(this IClickHouseSpecificQueryable<TSource> query, string hintFormat, params object?[] hintParameters)
			where TSource : notnull
		{
			return QueryHint(query, Query.Settings, hintFormat, hintParameters);
		}
		static Expression<Func<IClickHouseSpecificQueryable<TSource>,string,object?[],IClickHouseSpecificQueryable<TSource>>> SettingsHintImpl<TSource>()
			where TSource : notnull
		{
			return (query, hintFormat, hintParameters) => QueryHint(query, Query.Settings, hintFormat, hintParameters);
		}
	}
}
