using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlQuery;

using PN = LinqToDB.ProviderName;

namespace LinqToDB
{
	public static partial class Sql
	{
		#region StringAggregate

		sealed class StringAggSql2017Builder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				ISqlExpression data;
				if (builder.Arguments.Length == 2)
					data = builder.GetExpression("source")!;
				else
					data = builder.GetExpression("selector")!;

				// https://github.com/linq2db/linq2db/issues/1765
				var descriptor = QueryHelper.GetColumnDescriptor(data);
				if (descriptor != null)
				{
					var dbDataType = descriptor.GetDbDataType(true);
					if (dbDataType.DataType != DataType.Undefined)
					{
						var separator = builder.GetExpression("separator");

						if (separator is SqlValue value && value.ValueType.DataType == DataType.Undefined)
							value.ValueType = value.ValueType.WithDataType(dbDataType.DataType);
						else if (separator is SqlParameter parameter && parameter.Type.DataType == DataType.Undefined)
							parameter.Type = parameter.Type.WithDataType(dbDataType.DataType);
					}
				}
			}
		}

		sealed class StringAggSapHanaBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var separator = builder.GetExpression("separator");

				// SAP HANA doesn't support parameters as separators
				if (separator is SqlParameter parameter)
					parameter.IsQueryParameter = false;
			}
		}

		[Extension(PN.SqlServer2025, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2022, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2019, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2017, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.PostgreSQL,    "STRING_AGG({source}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.SapHana,       "STRING_AGG({source}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSapHanaBuilder))]
		[Extension(PN.SQLite,        "GROUP_CONCAT({source}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.MySql,         "GROUP_CONCAT({source}{_}{order_by_clause?} SEPARATOR {separator})", IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.Oracle,        "LISTAGG({source}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.OracleNative,  "LISTAGG({source}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.DB2,           "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.DB2LUW,        "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.DB2zOS,        "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.Firebird,      "LIST({source}, {separator})",                                       IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.ClickHouse,    "arrayStringConcat(groupArray({source}), {separator})",              IsAggregate = true, ChainPrecedence = 10, CanBeNull = false)]
		[Extension("{source}",        TokenName = "aggregate")]
		public static IAggregateFunctionNotOrdered<string?, string> StringAggregate(
			[ExprParameter] this IQueryable<string?> source,
			[ExprParameter]      string              separator)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (separator == null) throw new ArgumentNullException(nameof(separator));

			var query = source.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StringAggregate, source, separator),
					source.Expression, Expression.Constant(separator)));

			return new AggregateFunctionNotOrderedImpl<string?, string>(query);
		}

		[Extension(PN.SqlServer2025, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2022, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2019, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2017, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.PostgreSQL,    "STRING_AGG({selector}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.SapHana,       "STRING_AGG({selector}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSapHanaBuilder))]
		[Extension(PN.SQLite,        "GROUP_CONCAT({selector}, {separator})",                               IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.MySql,         "GROUP_CONCAT({selector}{_}{order_by_clause?} SEPARATOR {separator})", IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.Oracle,        "LISTAGG({selector}, {separator}) {aggregation_ordering}",             IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.OracleNative,  "LISTAGG({selector}, {separator}) {aggregation_ordering}",             IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.DB2,           "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.DB2LUW,        "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.DB2zOS,        "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.Firebird,      "LIST({selector}, {separator})",                                       IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.ClickHouse,    "arrayStringConcat(groupArray({selector}), {separator})",              IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, CanBeNull = false)]
		public static IAggregateFunctionNotOrdered<T, string> StringAggregate<T>(
							this IEnumerable<T>   source,
			[ExprParameter]      string           separator,
			[ExprParameter]      Func<T, string?> selector)
			=> throw new ServerSideOnlyException(nameof(StringAggregate));

		[Extension(PN.SqlServer2025, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2022, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2019, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2017, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.PostgreSQL,    "STRING_AGG({selector}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.SapHana,       "STRING_AGG({selector}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSapHanaBuilder))]
		[Extension(PN.SQLite,        "GROUP_CONCAT({selector}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.MySql,         "GROUP_CONCAT({selector}{_}{order_by_clause?} SEPARATOR {separator})", IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.Oracle,        "LISTAGG({selector}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.OracleNative,  "LISTAGG({selector}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.DB2,           "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.DB2LUW,        "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.DB2zOS,        "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.Firebird,      "LIST({selector}, {separator})",                                       IsAggregate = true, ChainPrecedence = 10)]
		[Extension(PN.ClickHouse,    "arrayStringConcat(groupArray({selector}), {separator})",              IsAggregate = true, ChainPrecedence = 10, CanBeNull = false)]
		public static IAggregateFunctionNotOrdered<T, string> StringAggregate<T>(
							this IQueryable<T> source,
			[ExprParameter] string separator,
			[ExprParameter] Expression<Func<T, string?>> selector)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (separator == null) throw new ArgumentNullException(nameof(separator));
			if (selector  == null) throw new ArgumentNullException(nameof(selector));

			var query = source.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StringAggregate, source, separator, selector),
					source.Expression, Expression.Constant(separator), Expression.Quote(selector)));

			return new AggregateFunctionNotOrderedImpl<T, string>(query);
		}

		[Extension(PN.SqlServer2025, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2022, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2019, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.SqlServer2017, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Extension(PN.PostgreSQL,    "STRING_AGG({source}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.SapHana,       "STRING_AGG({source}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSapHanaBuilder))]
		[Extension(PN.SQLite,        "GROUP_CONCAT({source}, {separator})",                               IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.MySql,         "GROUP_CONCAT({source}{_}{order_by_clause?} SEPARATOR {separator})", IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.Oracle,        "LISTAGG({source}, {separator}) {aggregation_ordering}",             IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.OracleNative,  "LISTAGG({source}, {separator}) {aggregation_ordering}",             IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.DB2,           "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.DB2LUW,        "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.DB2zOS,        "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.Firebird,      "LIST({source}, {separator})",                                       IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10)]
		[Extension(PN.ClickHouse,    "arrayStringConcat(groupArray({source}), {separator})",              IsAggregate = true, ServerSideOnly = true, ChainPrecedence = 10, CanBeNull = false)]
		public static IAggregateFunctionNotOrdered<string?, string> StringAggregate(
			[ExprParameter] this IEnumerable<string?> source,
			[ExprParameter] string separator)
			=> throw new ServerSideOnlyException(nameof(StringAggregate));

		#endregion

		#region ConcatStrings

		/// <summary>
		/// Concatenates NOT NULL strings, using the specified separator between each member.
		/// </summary>
		/// <param name="separator">The string to use as a separator. <paramref name="separator" /> is included in the returned string only if <paramref name="arguments" /> has more than one element.</param>
		/// <param name="arguments">A collection that contains the strings to concatenate.</param>
		/// <returns></returns>
		public static string ConcatStrings(string separator, params string?[] arguments)
		{
			return string.Join(separator, arguments.Where(a => a != null));
		}

		/// <summary>
		/// Concatenates NOT NULL strings, using the specified separator between each member.
		/// </summary>
		/// <param name="separator">The string to use as a separator. <paramref name="separator" /> is included in the returned string only if <paramref name="arguments" /> has more than one element.</param>
		/// <param name="arguments">A collection that contains the strings to concatenate.</param>
		/// <returns></returns>
		public static string? ConcatStrings(string separator, IEnumerable<string?> arguments)
		{
			return string.Join(separator, arguments.Where(a => a != null));
		}

		/// <summary>
		/// Concatenates NOT NULL strings, using the specified separator between each member. Returns NULL if all arguments are NULL.
		/// </summary>
		/// <param name="separator">The string to use as a separator. <paramref name="separator" /> is included in the returned string only if <paramref name="arguments" /> has more than one element.</param>
		/// <param name="arguments">A collection that contains the strings to concatenate.</param>
		/// <returns></returns>
		public static string? ConcatStringsNullable(string separator, IEnumerable<string?> arguments)
		{
			var result = arguments.Aggregate((v1, v2) =>
			{
				if (v1 == null && v2 == null)
					return null;
				if (v1 == null)
					return v2;
				if (v2 == null)
					return v1;
				return v1 + separator + v2;
			});

			return result;
		}

		#endregion
	}
}
