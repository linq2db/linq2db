using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.Linq;
using LinqToDB.SqlQuery;
using PN = LinqToDB.ProviderName;

namespace LinqToDB
{
	public static class StringAggregateExtensions
	{
		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering")]
		[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause", ChainPrecedence = 0)]
		[Sql.Extension("{expr}", TokenName = "order_item")]
		public static string OrderBy<T, TKey>([NotNull] this Sql.IStringAggregateNotOrdered<T> aggregate, [ExprParameter] Expression<Func<T, TKey>> expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			return aggregate.Query.Provider.Execute<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate, expr),
					new Expression[] { Expression.Constant(aggregate), Expression.Quote(expr) }
				));
		}

		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering")]
		[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause", ChainPrecedence = 0)]
		[Sql.Extension("{aggregate}", TokenName = "order_item")]
		public static string OrderBy([ExprParameter] this Sql.IStringAggregateNotOrdered<string> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			return aggregate.Query.Provider.Execute<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate),
					new Expression[] { Expression.Constant(aggregate) }
				));
		}

		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering")]
		[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause", ChainPrecedence = 0)]
		[Sql.Extension("{expr} DESC", TokenName = "order_item")]
		public static string OrderByDescending<T, TKey>(this Sql.IStringAggregateNotOrdered<T> aggregate, [ExprParameter] Expression<Func<T, TKey>> expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			return aggregate.Query.Provider.Execute<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate, expr),
					new Expression[] { Expression.Constant(aggregate), Expression.Quote(expr) }
				));
		}

		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering")]
		[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause", ChainPrecedence = 0)]
		[Sql.Extension("{aggregate} DESC", TokenName = "order_item")]
		public static string OrderByDescending([ExprParameter] this Sql.IStringAggregateNotOrdered<string> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			return aggregate.Query.Provider.Execute<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate),
					new Expression[] { Expression.Constant(aggregate) }
				));
		}

		[Sql.Extension(                 "",                               TokenName = "aggregation_ordering", ChainPrecedence = 0)]
		[Sql.Extension(PN.Oracle,       "WITHIN GROUP (ORDER BY ROWNUM)", TokenName = "aggregation_ordering", ChainPrecedence = 0)]
		[Sql.Extension(PN.OracleNative, "WITHIN GROUP (ORDER BY ROWNUM)", TokenName = "aggregation_ordering", ChainPrecedence = 0)]
		public static string DefaultOrder<T>(this Sql.IStringAggregateNotOrdered<T> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			return aggregate.Query.Provider.Execute<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(DefaultOrder, aggregate), 
					Expression.Constant(aggregate)));
		}
	}

	public static partial class Sql
	{
		#region StringAggregate

		public interface IStringAggregateNotOrdered<out T> : IQueryableContainer
		{
		}

		internal class StringAggregateNotOrderedImpl<T> : IStringAggregateNotOrdered<T>
		{
			public StringAggregateNotOrderedImpl([NotNull] IQueryable<string> query)
			{
				Query = query ?? throw new ArgumentNullException(nameof(query));
			}

			public IQueryable Query { get; }
		}

		[Sql.Extension(PN.SqlServer2017, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",  IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.PostgreSQL,    "STRING_AGG({source}, {separator})",                            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SapHana,       "STRING_AGG({source}, {separator})",                            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SQLite,        "GROUP_CONCAT({source}, {separator})",                          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.MySql,         "GROUP_CONCAT({source} SEPARATOR {separator})",                 IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Oracle,        "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",     IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.OracleNative,  "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",     IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2,           "LISTAGG({source}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2LUW,        "LISTAGG({source}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2zOS,        "LISTAGG({source}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Firebird,      "LIST({source}, {separator})",                                  IsAggregate = true, ChainPrecedence = 10)]
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

		[Sql.Extension(PN.SqlServer2017, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",  IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.PostgreSQL,    "STRING_AGG({selector}, {separator})",                            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SapHana,       "STRING_AGG({selector}, {separator})",                            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SQLite,        "GROUP_CONCAT({selector}, {separator})",                          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.MySql,         "GROUP_CONCAT({selector} SEPARATOR {separator})",                 IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Oracle,        "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",     IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.OracleNative,  "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",     IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2,           "LISTAGG({selector}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2LUW,        "LISTAGG({selector}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2zOS,        "LISTAGG({selector}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Firebird,      "LIST({selector}, {separator})",                                  IsAggregate = true, ChainPrecedence = 10)]
		public static IStringAggregateNotOrdered<T> StringAggregate<T>(
			                [NotNull] this IEnumerable<T> source,
			[ExprParameter] [NotNull] string separator,
			[ExprParameter] [NotNull] Func<T, string> selector)
		{
			throw new LinqException($"'{nameof(StringAggregate)}' is server-side method.");
		}

		[Sql.Extension(PN.SqlServer2017, "STRING_AGG({selector}, {separator}){_}{aggregation_ordering?}",  IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.PostgreSQL,    "STRING_AGG({selector}, {separator})",                            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SapHana,       "STRING_AGG({selector}, {separator})",                            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SQLite,        "GROUP_CONCAT({selector}, {separator})",                          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.MySql,         "GROUP_CONCAT({selector} SEPARATOR {separator})",                 IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Oracle,        "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",     IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.OracleNative,  "LISTAGG({selector}, {separator}){_}{aggregation_ordering?}",     IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2,           "LISTAGG({selector}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2LUW,        "LISTAGG({selector}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2zOS,        "LISTAGG({selector}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Firebird,      "LIST({selector}, {separator})",                                  IsAggregate = true, ChainPrecedence = 10)]
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

		[Sql.Extension(PN.SqlServer2017, "STRING_AGG({source}, {separator}){_}{aggregation_ordering?}",  IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.PostgreSQL,    "STRING_AGG({source}, {separator})",                            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SapHana,       "STRING_AGG({source}, {separator})",                            IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.SQLite,        "GROUP_CONCAT({source}, {separator})",                          IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.MySql,         "GROUP_CONCAT({source} SEPARATOR {separator})",                 IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Oracle,        "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",     IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.OracleNative,  "LISTAGG({source}, {separator}){_}{aggregation_ordering?}",     IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2,           "LISTAGG({source}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2LUW,        "LISTAGG({source}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.DB2zOS,        "LISTAGG({source}, {separator})",                               IsAggregate = true, ChainPrecedence = 10)]
		[Sql.Extension(PN.Firebird,      "LIST({source}, {separator})",                                  IsAggregate = true, ChainPrecedence = 10)]

		public static IStringAggregateNotOrdered<string> StringAggregate(
			[ExprParameter] [NotNull] this IEnumerable<string> source,
			[ExprParameter] [NotNull] string separator)
		{
			throw new LinqException($"'{nameof(StringAggregate)}' is server-side method.");
		}

		#endregion

		#region ConcatWS

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

		[Sql.Extension(PN.SqlServer2017, "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = "ISNULL({0}, '')")]
		[Sql.Extension(PN.PostgreSQL,    "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = null)]
		[Sql.Extension(PN.MySql,         "CONCAT_WS({separator}, {argument, ', '})", BuilderType = typeof(CommonConcatWsArgumentsBuilder), BuilderValue = null)]
		[Sql.Extension(PN.SqlServer,     "", BuilderType = typeof(OldSqlServerConcatWsBuilder))]
		[Sql.Extension(PN.SQLite,        "", BuilderType = typeof(SqliteConcatWsBuilder))]
		public static string ConcatWS(
			[ExprParameter] [NotNull] string separator,
			                [NotNull] params string[] arguments)
		{
			return string.Join(separator, arguments.Where(a => a != null));
		}
		
		#endregion
	}
}
