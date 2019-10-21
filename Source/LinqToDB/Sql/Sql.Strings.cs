using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;

using PN = LinqToDB.ProviderName;

namespace LinqToDB
{
	using Linq;
	using LinqToDB.Common;
	using SqlQuery;

	public static class StringAggregateExtensions
	{
		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[Sql.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[Sql.Extension("{expr}",                           TokenName = "order_item")]
		public static Sql.IStringAggregateOrdered<T> OrderBy<T, TKey>(
							[NotNull] this Sql.IStringAggregateNotOrdered<T> aggregate, 
			[ExprParameter] [NotNull]      Expression<Func<T, TKey>>         expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate, expr),
					new Expression[] { Expression.Constant(aggregate), Expression.Quote(expr) }
				));

			return new Sql.StringAggregateNotOrderedImpl<T>(query);
		}

		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[Sql.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[Sql.Extension("{aggregate}",                      TokenName = "order_item")]
		public static Sql.IStringAggregate<string> OrderBy(
			[NotNull] [ExprParameter] this Sql.IStringAggregateNotOrdered<string> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			var query = aggregate.Query.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate), Expression.Constant(aggregate)
				));

			return new Sql.StringAggregateNotOrderedImpl<string>(query);
		}

		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[Sql.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[Sql.Extension("{expr} DESC",                      TokenName = "order_item")]
		public static Sql.IStringAggregateOrdered<T> OrderByDescending<T, TKey>(
							[NotNull] this Sql.IStringAggregateNotOrdered<T> aggregate, 
			[ExprParameter] [NotNull]      Expression<Func<T, TKey>>         expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate, expr),
					new Expression[] { Expression.Constant(aggregate), Expression.Quote(expr) }
				));

			return new Sql.StringAggregateNotOrderedImpl<T>(query);
		}

		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[Sql.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[Sql.Extension("{aggregate} DESC",                 TokenName = "order_item")]
		public static Sql.IStringAggregate<string> OrderByDescending(
			[NotNull] [ExprParameter] this Sql.IStringAggregateNotOrdered<string> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			var query = aggregate.Query.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate),
					Expression.Constant(aggregate)
				));

			return new Sql.StringAggregateNotOrderedImpl<string>(query);
		}

		[Sql.Extension("{expr}", TokenName = "order_item")]
		public static Sql.IStringAggregateOrdered<T> ThenBy<T, TKey>(
							[NotNull] this Sql.IStringAggregateOrdered<T> aggregate, 
			[ExprParameter] [NotNull]      Expression<Func<T, TKey>>      expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ThenBy, aggregate, expr),
					new Expression[] { Expression.Constant(aggregate), Expression.Quote(expr) }
				));

			return new Sql.StringAggregateNotOrderedImpl<T>(query);
		}

		[Sql.Extension("{expr} DESC", TokenName = "order_item")]
		public static Sql.IStringAggregateOrdered<T> ThenByDescending<T, TKey>(
							[NotNull] this Sql.IStringAggregateOrdered<T> aggregate, 
			[ExprParameter] [NotNull]      Expression<Func<T, TKey>>      expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ThenByDescending, aggregate, expr),
					new Expression[] { Expression.Constant(aggregate), Expression.Quote(expr) }
				));

			return new Sql.StringAggregateNotOrderedImpl<T>(query);
		}

		// For Oracle we always define at least one ordering by rownum. If ordering defined explicitly, this definition will be replaced.
		[Sql.Extension(PN.Oracle,       "WITHIN GROUP (ORDER BY ROWNUM)", TokenName = "aggregation_ordering", ChainPrecedence = 0, IsAggregate = true)]
		[Sql.Extension(PN.OracleNative, "WITHIN GROUP (ORDER BY ROWNUM)", TokenName = "aggregation_ordering", ChainPrecedence = 0, IsAggregate = true)]
		[Sql.Extension(                  "",                                                                  ChainPrecedence = 0, IsAggregate = true)]
		public static string ToValue<T>([NotNull] this Sql.IStringAggregate<T> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			return aggregate.Query.Provider.Execute<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ToValue, aggregate), 
					Expression.Constant(aggregate)));
		}
	}

	public static partial class Sql
	{
		#region StringAggregate

		public interface IStringAggregate<out T> : IQueryableContainer
		{

		}

		public interface IStringAggregateNotOrdered<out T> : IStringAggregate<T>
		{
		}

		public interface IStringAggregateOrdered<out T> : IStringAggregate<T>
		{
		}

		internal class StringAggregateNotOrderedImpl<T> : IStringAggregateNotOrdered<T>, IStringAggregateOrdered<T>
		{
			public StringAggregateNotOrderedImpl([NotNull] IQueryable<string> query)
			{
				Query = query ?? throw new ArgumentNullException(nameof(query));
			}

			public IQueryable Query { get; }
		}

		class StringAggSql2017Builder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				ISqlExpression data;
				if (builder.Arguments.Length == 2)
					data = builder.GetExpression("source");
				else
					data = builder.GetExpression("selector");

				// https://github.com/linq2db/linq2db/issues/1765
				if (data is SqlField field && field.DataType != DataType.Undefined)
				{
					var separator = builder.GetExpression("separator");

					if (separator is SqlValue value && value.ValueType.DataType == DataType.Undefined)
						value.ValueType = value.ValueType.WithDataType(field.DataType);
					else if (separator is SqlParameter parameter && parameter.DataType == DataType.Undefined)
						parameter.DataType = field.DataType;
				}
			}
		}

		class StringAggSapHanaBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var separator = builder.GetExpression("separator");

				// SAP HANA doesn't support parameters as separators
				if (separator is SqlParameter parameter)
					parameter.IsQueryParameter = false;
			}
		}

		[Sql.Extension(PN.SqlServer2017, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Sql.Extension(PN.PostgreSQL,    "STRING_AGG({source}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SapHana,       "STRING_AGG({source}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSapHanaBuilder))]
		[Sql.Extension(PN.SQLite,        "GROUP_CONCAT({source}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.MySql,         "GROUP_CONCAT({source}{_}{order_by_clause?} SEPARATOR {separator})", IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Oracle,        "LISTAGG({source}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.OracleNative,  "LISTAGG({source}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2,           "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2LUW,        "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2zOS,        "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Firebird,      "LIST({source}, {separator})",                                       IsAggregate = true, ChainPrecedence = 10)]
		public static IStringAggregateNotOrdered<string> StringAggregate(
			[ExprParameter] [NotNull] this IQueryable<string> source,
			[ExprParameter] [NotNull] string separator)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (separator == null) throw new ArgumentNullException(nameof(separator));

			var query = source.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StringAggregate, source, separator),
					new[] { source.Expression, Expression.Constant(separator) }
				));

			return new StringAggregateNotOrderedImpl<string>(query);
		}

		[Sql.Extension(PN.SqlServer2017, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Sql.Extension(PN.PostgreSQL,    "STRING_AGG({selector}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SapHana,       "STRING_AGG({selector}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSapHanaBuilder))]
		[Sql.Extension(PN.SQLite,        "GROUP_CONCAT({selector}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.MySql,         "GROUP_CONCAT({selector}{_}{order_by_clause?} SEPARATOR {separator})", IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Oracle,        "LISTAGG({selector}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.OracleNative,  "LISTAGG({selector}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2,           "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2LUW,        "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2zOS,        "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Firebird,      "LIST({selector}, {separator})",                                       IsAggregate = true, ChainPrecedence = 10)]
		public static IStringAggregateNotOrdered<T> StringAggregate<T>(
							[NotNull] this IEnumerable<T> source,
			[ExprParameter] [NotNull] string separator,
			[ExprParameter] [NotNull] Func<T, string> selector)
		{
			throw new LinqException($"'{nameof(StringAggregate)}' is server-side method.");
		}

		[Sql.Extension(PN.SqlServer2017, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Sql.Extension(PN.PostgreSQL,    "STRING_AGG({selector}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SapHana,       "STRING_AGG({selector}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSapHanaBuilder))]
		[Sql.Extension(PN.SQLite,        "GROUP_CONCAT({selector}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.MySql,         "GROUP_CONCAT({selector}{_}{order_by_clause?} SEPARATOR {separator})", IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Oracle,        "LISTAGG({selector}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.OracleNative,  "LISTAGG({selector}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2,           "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2LUW,        "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2zOS,        "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Firebird,      "LIST({selector}, {separator})",                                       IsAggregate = true, ChainPrecedence = 10)]
		public static IStringAggregateNotOrdered<T> StringAggregate<T>(
							[NotNull] this IQueryable<T> source,
			[ExprParameter] [NotNull] string separator,
			[ExprParameter] [NotNull] Expression<Func<T, string>> selector)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (separator == null) throw new ArgumentNullException(nameof(separator));
			if (selector  == null) throw new ArgumentNullException(nameof(selector));

			var query = source.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StringAggregate, source, separator, selector),
					new[] { source.Expression, Expression.Constant(separator), Expression.Quote(selector) }
				));

			return new StringAggregateNotOrderedImpl<T>(query);
		}

		[Sql.Extension(PN.SqlServer2017, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",       IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSql2017Builder))]
		[Sql.Extension(PN.PostgreSQL,    "STRING_AGG({source}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SapHana,       "STRING_AGG({source}, {separator}{_}{order_by_clause?})",            IsAggregate = true, ChainPrecedence = 10, BuilderType = typeof(StringAggSapHanaBuilder))]
		[Sql.Extension(PN.SQLite,        "GROUP_CONCAT({source}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.MySql,         "GROUP_CONCAT({source}{_}{order_by_clause?} SEPARATOR {separator})", IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Oracle,        "LISTAGG({source}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.OracleNative,  "LISTAGG({source}, {separator}) {aggregation_ordering}",             IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2,           "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2LUW,        "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2zOS,        "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Firebird,      "LIST({source}, {separator})",                                       IsAggregate = true, ChainPrecedence = 10)]
		public static IStringAggregateNotOrdered<string> StringAggregate(
			[ExprParameter] [NotNull] this IEnumerable<string> source,
			[ExprParameter] [NotNull] string separator)
		{
			throw new LinqException($"'{nameof(StringAggregate)}' is server-side method.");
		}

		#endregion

		#region ConcatStrings

		class CommonConcatWsArgumentsBuilder : Sql.IExtensionCallBuilder
		{
			SqlExpression IsNullExpression(string isNullFormat, ISqlExpression value)
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
					builder.ResultExpression = IsNullExpression((string)builder.BuilderValue, builder.ConvertExpressionToSql(arguments.Expressions[0]));
				}
				else
				{
					var items = arguments.Expressions.Select(builder.ConvertExpressionToSql);
					foreach (var item in items)
					{
						builder.AddParameter("argument", item);
					}
				}
			}
		}

		abstract class BaseEmulationConcatWsBuilder : Sql.IExtensionCallBuilder
		{
			protected abstract SqlExpression IsNullExpression(ISqlExpression value);
			protected abstract SqlExpression StringConcatExpression(ISqlExpression value1, ISqlExpression value2);
			protected abstract SqlExpression TruncateExpression(ISqlExpression value, ISqlExpression separator);

			public void Build(ISqExtensionBuilder builder)
			{
				var separator = builder.GetExpression(0);
				var arguments = (NewArrayExpression)builder.Arguments[1];
				if (arguments.Expressions.Count == 0)
				{
					builder.ResultExpression = new SqlExpression(typeof(string), "''");
				}
				else if (arguments.Expressions.Count == 1)
				{
					builder.ResultExpression = IsNullExpression(builder.ConvertExpressionToSql(arguments.Expressions[0]));
				}
				else
				{
					var items = arguments.Expressions.Select(e =>
						IsNullExpression(StringConcatExpression(separator, builder.ConvertExpressionToSql(e)))
					);

					var concatenation =
						items.Aggregate(StringConcatExpression);

					builder.ResultExpression = TruncateExpression(concatenation, separator);
				}
			}
		}

		class OldSqlServerConcatWsBuilder : BaseEmulationConcatWsBuilder
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
				return new SqlExpression(typeof(string), "SUBSTRING({0}, LEN({1}) + 2, 8000)",
					Precedence.Primary, value, separator);
			}
		}

		class SqliteConcatWsBuilder : BaseEmulationConcatWsBuilder
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
		[Sql.Extension(PN.SqlServer2017, "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = "ISNULL({0}, '')")]
		[Sql.Extension(PN.PostgreSQL,    "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = null)]
		[Sql.Extension(PN.MySql,         "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = null)]
		[Sql.Extension(PN.SqlServer,     "", BuilderType = typeof(OldSqlServerConcatWsBuilder))]
		[Sql.Extension(PN.SQLite,        "", BuilderType = typeof(SqliteConcatWsBuilder))]
		public static string ConcatStrings(
			[ExprParameter] [NotNull] string separator,
							[NotNull] params string?[] arguments)
		{
			return string.Join(separator, arguments.Where(a => a != null));
		}
		
		#endregion
	}
}
