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
	public static partial class Sql
	{
		static string AggregateStrings(string separator, IEnumerable<string> arguments)
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

		[Sql.Extension(PN.SqlServer,    "STRING_AGG({source}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.PostgreSQL,   "STRING_AGG({source}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.SapHana,      "STRING_AGG({source}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.SQLite,       "GROUP_CONCAT({source}, {separator})", IsAggregate = true)]
		[Sql.Extension(PN.MySql,        "GROUP_CONCAT({source}, {separator})", IsAggregate = true)]
		[Sql.Extension(PN.Oracle,       "LISTAGG({source}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.OracleNative, "LISTAGG({source}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2,          "LISTAGG({source}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2LUW,       "LISTAGG({source}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2zOS,       "LISTAGG({source}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.Firebird,     "LIST({source}, {separator})",         IsAggregate = true)]
		public static string StringAggregate(
			[ExprParameter] [NotNull] this IQueryable<string> source,
			[ExprParameter] [NotNull] string separator)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (separator == null) throw new ArgumentNullException(nameof(separator));

			return source.Provider.Execute<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StringAggregate, source, separator),
					new[] { source.Expression, Expression.Constant(separator) }
				));
		}

		[Sql.Extension(PN.SqlServer,    "STRING_AGG({selector}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.PostgreSQL,   "STRING_AGG({selector}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.SapHana,      "STRING_AGG({selector}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.SQLite,       "GROUP_CONCAT({selector}, {separator})", IsAggregate = true)]
		[Sql.Extension(PN.MySql,        "GROUP_CONCAT({selector}, {separator})", IsAggregate = true)]
		[Sql.Extension(PN.Oracle,       "LISTAGG({selector}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.OracleNative, "LISTAGG({selector}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2,          "LISTAGG({selector}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2LUW,       "LISTAGG({selector}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2zOS,       "LISTAGG({selector}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.Firebird,     "LIST({selector}, {separator})",         IsAggregate = true)]
		public static string StringAggregate<T>(
			                [NotNull] this IEnumerable<T> source,
			[ExprParameter] [NotNull] string separator,
			[ExprParameter] [NotNull] Func<T, string> selector)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (separator == null) throw new ArgumentNullException(nameof(separator));
			if (selector  == null) throw new ArgumentNullException(nameof(selector));

			return AggregateStrings(separator, source.Select(selector));
		}

		[Sql.Extension(PN.SqlServer,    "STRING_AGG({selector}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.PostgreSQL,   "STRING_AGG({selector}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.SapHana,      "STRING_AGG({selector}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.SQLite,       "GROUP_CONCAT({selector}, {separator})", IsAggregate = true)]
		[Sql.Extension(PN.MySql,        "GROUP_CONCAT({selector}, {separator})", IsAggregate = true)]
		[Sql.Extension(PN.Oracle,       "LISTAGG({selector}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.OracleNative, "LISTAGG({selector}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2,          "LISTAGG({selector}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2LUW,       "LISTAGG({selector}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2zOS,       "LISTAGG({selector}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.Firebird,     "LIST({selector}, {separator})",         IsAggregate = true)]
		public static string StringAggregate<T>(
			                [NotNull] this IQueryable<T> source,
			[ExprParameter] [NotNull] string separator,
			[ExprParameter] [NotNull] Expression<Func<T, string>> selector)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (separator == null) throw new ArgumentNullException(nameof(separator));
			if (selector  == null) throw new ArgumentNullException(nameof(selector));

			return source.Provider.Execute<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StringAggregate, source, separator, selector),
					new[] { source.Expression, Expression.Constant(separator), Expression.Quote(selector) }
				));
		}

		[Sql.Extension(PN.SqlServer,    "STRING_AGG({source}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.PostgreSQL,   "STRING_AGG({source}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.SapHana,      "STRING_AGG({source}, {separator})",   IsAggregate = true)]
		[Sql.Extension(PN.SQLite,       "GROUP_CONCAT({source}, {separator})", IsAggregate = true)]
		[Sql.Extension(PN.MySql,        "GROUP_CONCAT({source}, {separator})", IsAggregate = true)]
		[Sql.Extension(PN.Oracle,       "LISTAGG({source}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.OracleNative, "LISTAGG({source}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2,          "LISTAGG({source}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2LUW,       "LISTAGG({source}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.DB2zOS,       "LISTAGG({source}, {separator})",      IsAggregate = true)]
		[Sql.Extension(PN.Firebird,     "LIST({source}, {separator})",         IsAggregate = true)]
		public static string StringAggregate(
			[ExprParameter] [NotNull] this IEnumerable<string> source,
			[ExprParameter] [NotNull] string separator)
		{
			return AggregateStrings(separator, source);
		}

		class SqliteConcatWsBuilder : Sql.IExtensionCallBuilder
		{
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
					builder.ResultExpression = new SqlExpression(typeof(string), "IFNULL({0}, '')", builder.ConvertExpressionToSql(arguments.Expressions[0]));
				}
				else
				{
					var items = arguments.Expressions.Select(e =>
						new SqlExpression(typeof(string), "IFNULL({0} || {1}, '')", Precedence.Primary, separator,
							builder.ConvertExpressionToSql(e))
					);

					var concatenation =
						items.Aggregate((i1, i2) => new SqlExpression(typeof(string), "{0} || {1}", i1, i2));

					builder.ResultExpression = new SqlExpression(typeof(string), "SUBSTR({0}, LENGTH({1}) + 1)",
						Precedence.Primary, concatenation, separator);
				}
			}
		}

		[Sql.Extension(PN.SqlServer,    "CONCAT_WS({separator}, {argument, ', '})")]
		[Sql.Extension(PN.Oracle,       "CONCAT_WS({separator}, {argument, ', '})")]
		[Sql.Extension(PN.OracleNative, "CONCAT_WS({separator}, {argument, ', '})")]
		[Sql.Extension(PN.PostgreSQL,   "CONCAT_WS({separator}, {argument, ', '})")]
		[Sql.Extension(PN.SapHana,      "CONCAT_WS({separator}, {argument, ', '})")]
		[Sql.Extension(PN.MySql,        "CONCAT_WS({separator}, {argument, ', '})")]
		[Sql.Extension(PN.SQLite,       "", BuilderType = typeof(SqliteConcatWsBuilder))]
		public static string ConcatWS(
			[ExprParameter]             [NotNull] string separator,
			[ExprParameter("argument")] [NotNull] params string[] arguments)
		{
			return string.Join(separator, arguments.Where(a => a != null));
		}
		
	}
}
