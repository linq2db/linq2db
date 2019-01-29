using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.Linq;

namespace LinqToDB
{
	public static partial class Sql
	{
		[Sql.Extension("STRING_AGG({source}, {separator})", IsAggregate = true)]
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

		[Sql.Extension("STRING_AGG({selector}, {separator})", IsAggregate = true)]
		public static string StringAggregate<T>(
			                [NotNull] this IEnumerable<T> source,
			[ExprParameter] [NotNull] string separator,
			[ExprParameter] [NotNull] Func<T, string> selector)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (separator == null) throw new ArgumentNullException(nameof(separator));
			if (selector  == null) throw new ArgumentNullException(nameof(selector));

			return string.Join(separator, source.Select(selector));
		}

		[Sql.Extension("STRING_AGG({selector}, {separator})", IsAggregate = true)]
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

		[Sql.Extension("STRING_AGG({source}, {separator})", IsAggregate = true)]
		public static string StringAggregate(
			[ExprParameter] [NotNull] this IEnumerable<string> source,
			[ExprParameter] [NotNull] string separator)
		{
			return string.Join(separator, source);
		}

		[Sql.Extension("CONCAT_WS({separator}, {argument, ', '})")]
		public static string ConcatWS(
			[ExprParameter]             [NotNull] string separator,
			[ExprParameter("argument")] [NotNull] params string[] arguments)
		{
			return string.Join(separator, arguments);
		}
		
	}
}
