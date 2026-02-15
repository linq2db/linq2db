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

		/// <summary>
		/// Creates a server-side aggregate function that concatenates the elements of a sequence of nullable strings within an <see cref="IQueryable"/> query,
		/// using the specified separator between each element.
		/// </summary>
		/// <param name="source">Queryable sequence of nullable strings. Cannot be <see langword="null"/>.</param>
		/// <param name="separator">Separator string. Cannot be <see langword="null"/>.</param>
		/// <returns>
		/// An <see cref="IAggregateFunctionNotOrdered{TSource, TResult}"/> expression representing the aggregation in the query. The database produces the final concatenated string.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="separator"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// LEGACY: Retained for backward compatibility.
		/// Prefer using one of the following (both forms can execute client-side or be translated when used inside an <see cref="IQueryable"/>):
		/// <para><c>string.Join(separator, source.Where(s => s != null))</c></para>
		/// <para><c>Sql.ConcatStrings(separator, source)</c></para>
		/// </remarks>
		public static IAggregateFunctionNotOrdered<string?, string> StringAggregate(
			this IQueryable<string?> source,
			string                   separator)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(separator);

			var query = source.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StringAggregate, source, separator),
					source.Expression, Expression.Constant(separator)));

			return new AggregateFunctionNotOrderedImpl<string?, string>(query);
		}

		/// <summary>
		/// Creates a server-side aggregate that concatenates projected nullable string values from a queryable sequence, using the specified separator.
		/// </summary>
		/// <param name="source">Queryable source sequence. Cannot be <see langword="null"/>.</param>
		/// <param name="separator">Separator string. Cannot be <see langword="null"/>.</param>
		/// <param name="selector">Projection producing nullable string values. Cannot be <see langword="null"/>.</param>
		/// <remarks>
		/// LEGACY: Retained for backward compatibility.
		/// Prefer using one of the following (both can run client-side or be translated when part of an <see cref="IQueryable"/>):
		/// <para><c>string.Join(separator, source.Select(selector).Where(s => s != null))</c></para>
		/// <para><c>Sql.ConcatStrings(separator, source.Select(selector))</c></para>
		/// </remarks>
		public static IAggregateFunctionNotOrdered<T, string> StringAggregate<T>(
			this IQueryable<T>           source,
			string                       separator,
			Expression<Func<T, string?>> selector)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(separator);
			ArgumentNullException.ThrowIfNull(selector);

			var query = source.Provider.CreateQuery<string>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StringAggregate, source, separator, selector),
					source.Expression, Expression.Constant(separator), Expression.Quote(selector)));

			return new AggregateFunctionNotOrderedImpl<T, string>(query);
		}

		/// <summary>
		/// Server-side aggregate placeholder for concatenation of nullable strings. This overload cannot execute on an <see cref="IEnumerable{T}"/> and will always throw.
		/// Use only inside an <see cref="IQueryable"/> expression tree.
		/// </summary>
		/// <param name="source">Sequence (must be part of query translation). Cannot be <see langword="null"/>.</param>
		/// <param name="separator">Separator string. Cannot be <see langword="null"/>.</param>
		/// <remarks>
		/// LEGACY. Use <c>string.Join(separator, source.Where(s => s != null))</c> or <c>Sql.ConcatStrings(separator, source)</c> (both forms support client or server execution).
		/// </remarks>
		public static IAggregateFunctionNotOrdered<string?, string> StringAggregate(
			this IEnumerable<string?> source,
			string separator)
			=> throw new ServerSideOnlyException(nameof(StringAggregate));

		/// <summary>
		/// Server-side aggregate placeholder for concatenation of projected nullable strings. This overload cannot execute on an <see cref="IEnumerable{T}"/> and will always throw.
		/// Use only inside an <see cref="IQueryable"/> expression tree.
		/// </summary>
		/// <param name="source">Sequence (must be part of query translation). Cannot be <see langword="null"/>.</param>
		/// <param name="separator">Separator string. Cannot be <see langword="null"/>.</param>
		/// <param name="selector">Projection producing nullable string values.</param>
		/// <remarks>
		/// LEGACY. Use <c>string.Join(separator, source.Select(selector).Where(s => s != null))</c> or <c>Sql.ConcatStrings(separator, source.Select(selector))</c> (both client/server capable).
		/// </remarks>
		public static IAggregateFunctionNotOrdered<T, string> StringAggregate<T>(
			this IEnumerable<T> source,
			string              separator,
			Func<T, string?>    selector)
			=> throw new ServerSideOnlyException(nameof(StringAggregate));

		#endregion

		#region ConcatStrings

		/// <summary>
		/// Concatenates NOT NULL strings, using the specified separator between each member.
		/// </summary>
		/// <param name="separator">The string to use as a separator. <paramref name="separator" /> is included in the returned string only if <paramref name="arguments" /> has more than one element.</param>
		/// <param name="arguments">A collection that contains the strings to concatenate.</param>
		/// <returns>
		/// A string that consists of the elements in <paramref name="arguments"/> delimited by the <paramref name="separator"/> string. If <paramref name="arguments"/> has only one element, the separator is not included.
		/// </returns>
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
		/// Null values are skipped without adding extra separators.
		/// </summary>
		/// <param name="separator">Separator inserted between consecutive non-null values.</param>
		/// <param name="arguments">Sequence of nullable strings to concatenate.</param>
		/// <returns>
		/// Concatenated string, or <see langword="null"/> if the sequence contains only <see langword="null"/> values or is empty.
		/// </returns>
		/// <remarks>
		/// Can be evaluated client-side or translated server-side when used inside an <see cref="IQueryable"/>.
		/// </remarks>
		public static string? ConcatStringsNullable(string separator, IEnumerable<string?> arguments)
		{
			var result = arguments.Aggregate((v1, v2) =>
			{
                return (v1, v2) switch
                {
                    (null, null) => null,
                    ({ } _v1, null) => _v1,
                    (null, { } _v2) => _v2,
                    ({ } _v1, { } _v2) => v1 + separator + v2,
                };
			});

			return result;
		}

		#endregion
	}
}
