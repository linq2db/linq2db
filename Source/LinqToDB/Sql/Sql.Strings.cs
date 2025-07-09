using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

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

		sealed class CommonConcatWsArgumentsBuilder : IExtensionCallBuilder
		{
			static SqlExpression IsNullExpression(string isNullFormat, ISqlExpression value)
			{
				return new SqlExpression(typeof(string), isNullFormat, value);
			}

			public void Build(ISqExtensionBuilder builder)
			{
				var arguments = (NewArrayExpression)builder.Arguments[1];
				if (arguments.Expressions.Count == 0 && builder.BuilderValue != null)
				{
					builder.ResultExpression = new SqlExpression(typeof(string), "''");
				}
				else if (arguments.Expressions.Count == 1 && builder.BuilderValue != null)
				{
					builder.ResultExpression = IsNullExpression((string)builder.BuilderValue, builder.ConvertExpressionToSql(arguments.Expressions[0])!);
				}
				else
				{
					var items = arguments.Expressions.Select(e => builder.ConvertExpressionToSql(e)!);
					foreach (var item in items)
					{
						builder.AddParameter("argument", item);
					}
				}
			}
		}

		abstract class BaseEmulationConcatWsBuilder : IExtensionCallBuilder
		{
			protected abstract SqlExpression IsNullExpression(ISqlExpression value);
			protected abstract SqlExpression StringConcatExpression(ISqlExpression value1, ISqlExpression value2);
			protected abstract SqlExpression TruncateExpression(ISqlExpression value, ISqlExpression separator);

			public void Build(ISqExtensionBuilder builder)
			{
				var separator = builder.GetExpression(0)!;
				var arguments = (NewArrayExpression)builder.Arguments[1];
				if (arguments.Expressions.Count == 0)
				{
					builder.ResultExpression = new SqlExpression(typeof(string), "''");
				}
				else if (arguments.Expressions.Count == 1)
				{
					builder.ResultExpression = IsNullExpression(builder.ConvertExpressionToSql(arguments.Expressions[0])!);
				}
				else
				{
					var items = arguments.Expressions.Select(e =>
						IsNullExpression(StringConcatExpression(separator, builder.ConvertExpressionToSql(e)!))
					);

					var concatenation =
						items.Aggregate(StringConcatExpression);

					builder.ResultExpression = TruncateExpression(concatenation, separator);
				}
			}
		}

		sealed class OldSqlServerConcatWsBuilder : BaseEmulationConcatWsBuilder
		{
			protected override SqlExpression IsNullExpression(ISqlExpression value)
			{
				return new SqlExpression(typeof(string), "ISNULL({0}, '')", Precedence.Primary, value);
			}

			protected override SqlExpression StringConcatExpression(ISqlExpression value1, ISqlExpression value2)
			{
				return new SqlExpression(typeof(string), "{0} + {1}", value1, value2);
			}

			protected override SqlExpression TruncateExpression(ISqlExpression value, ISqlExpression separator)
			{
				// you can read more about this gore code here:
				// https://stackoverflow.com/questions/2025585
				return new SqlExpression(typeof(string), "SUBSTRING({0}, LEN(CONVERT(NVARCHAR(MAX), {1}) + N'!'), 8000)",
					Precedence.Primary, value, separator);
			}
		}

		sealed class SqliteConcatWsBuilder : BaseEmulationConcatWsBuilder
		{
			protected override SqlExpression IsNullExpression(ISqlExpression value)
			{
				return new SqlExpression(typeof(string), "IFNULL({0}, '')", Precedence.Primary, value);
			}

			protected override SqlExpression StringConcatExpression(ISqlExpression value1, ISqlExpression value2)
			{
				return new SqlExpression(typeof(string), "{0} || {1}", value1, value2);
			}

			protected override SqlExpression TruncateExpression(ISqlExpression value, ISqlExpression separator)
			{
				return new SqlExpression(typeof(string), "SUBSTR({0}, LENGTH({1}) + 1)",
					Precedence.Primary, value, separator);
			}
		}

		/// <summary>
		/// Concatenates NOT NULL strings, using the specified separator between each member.
		/// </summary>
		/// <param name="separator">The string to use as a separator. <paramref name="separator" /> is included in the returned string only if <paramref name="arguments" /> has more than one element.</param>
		/// <param name="arguments">A collection that contains the strings to concatenate.</param>
		/// <returns></returns>
		[Extension(PN.SqlServer2025, "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = "ISNULL({0}, '')", IsAggregate = true)]
		[Extension(PN.SqlServer2022, "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = "ISNULL({0}, '')", IsAggregate = true)]
		[Extension(PN.SqlServer2019, "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = "ISNULL({0}, '')", IsAggregate = true)]
		[Extension(PN.SqlServer2017, "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = "ISNULL({0}, '')", IsAggregate = true)]
		[Extension(PN.PostgreSQL,    "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = null, IsAggregate = true)]
		[Extension(PN.MySql,         "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = null, IsAggregate = true)]
		[Extension(PN.SqlServer,     "", BuilderType = typeof(OldSqlServerConcatWsBuilder), IsAggregate = true)]
		[Extension(PN.SQLite,        "", BuilderType = typeof(SqliteConcatWsBuilder), IsAggregate = true)]
		[Extension(PN.ClickHouse,   "arrayStringConcat([{arguments, ', '}], {separator})", IsAggregate = true, CanBeNull = false)]
		public static string ConcatStrings(
			[ExprParameter(ParameterKind = ExprParameterKind.Values)]        string    separator,
			[ExprParameter(ParameterKind = ExprParameterKind.Values)] params string?[] arguments)
		{
			return string.Join(separator, arguments.Where(a => a != null));
		}

		#endregion
	}
}
