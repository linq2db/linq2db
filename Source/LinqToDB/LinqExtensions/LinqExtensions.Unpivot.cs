using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Linq;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Rotates a fixed set of columns into rows, producing one output row per (source-row, column).
		/// Emits native <c>UNPIVOT</c> on providers that support it (SQL Server, Oracle, DuckDB); everywhere
		/// else it is lowered to a <c>UNION ALL</c> derived table. NULL value cells are excluded (matching
		/// native <c>UNPIVOT</c>); use the
		/// <see cref="Unpivot{TSource,TValue,TResult}(IQueryable{TSource},UnpivotNulls,Expression{Func{TSource,string,TValue,TResult}},Expression{Func{TSource,TValue}},Expression{Func{TSource,TValue}}[])"/>
		/// overload to keep them.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <typeparam name="TValue">Common type of the unpivoted columns.</typeparam>
		/// <typeparam name="TResult">Result record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="resultSelector">Projection over the source row, the unpivoted column's name, and its value.</param>
		/// <param name="column">First column to unpivot (at least one is required).</param>
		/// <param name="columns">Additional columns to unpivot.</param>
		/// <returns>Query producing one row per (source-row, unpivoted column).</returns>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> Unpivot<TSource, TValue, TResult>(
			this            IQueryable<TSource>                                source,
			[InstantHandle] Expression<Func<TSource, string, TValue, TResult>> resultSelector,
			[InstantHandle] Expression<Func<TSource, TValue>>                  column,
			[InstantHandle] params Expression<Func<TSource, TValue>>[]         columns)
		{
			return Unpivot(source, UnpivotNulls.ExcludeNulls, resultSelector, column, columns);
		}

		/// <summary>
		/// Rotates a fixed set of columns into rows, producing one output row per (source-row, column), with
		/// explicit control over NULL value cells.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <typeparam name="TValue">Common type of the unpivoted columns.</typeparam>
		/// <typeparam name="TResult">Result record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="nulls">Whether rows with a NULL value cell are excluded (default) or kept.</param>
		/// <param name="resultSelector">Projection over the source row, the unpivoted column's name, and its value.</param>
		/// <param name="column">First column to unpivot (at least one is required).</param>
		/// <param name="columns">Additional columns to unpivot.</param>
		/// <returns>Query producing one row per (source-row, unpivoted column).</returns>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> Unpivot<TSource, TValue, TResult>(
			this            IQueryable<TSource>                                source,
			                UnpivotNulls                                       nulls,
			[InstantHandle] Expression<Func<TSource, string, TValue, TResult>> resultSelector,
			[InstantHandle] Expression<Func<TSource, TValue>>                  column,
			[InstantHandle] params Expression<Func<TSource, TValue>>[]         columns)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(resultSelector);
			ArgumentNullException.ThrowIfNull(column);
			ArgumentNullException.ThrowIfNull(columns);

			var currentSource = source.ProcessIQueryable();

			var columnsArray = Expression.NewArrayInit(
				typeof(Expression<Func<TSource, TValue>>),
				columns.Select(static c => (Expression)Expression.Quote(c)));

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Unpivot, source, nulls, resultSelector, column, columns),
				currentSource.Expression,
				Expression.Constant(nulls),
				Expression.Quote(resultSelector),
				Expression.Quote(column),
				columnsArray);

			return currentSource.Provider.CreateQuery<TResult>(expr);
		}

		/// <summary>
		/// Multi-value UNPIVOT (two value columns per group): rotates named groups of two columns into rows.
		/// Each group contributes one output row per source row carrying the group's name and the two column
		/// values. Lowered to a portable <c>UNION ALL</c> derived table on every provider; NULL rows are kept.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <typeparam name="TValue">Common type of the unpivoted columns.</typeparam>
		/// <typeparam name="TResult">Result record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="resultSelector">Projection over the source row, the group name, and the two column values.</param>
		/// <param name="groups">The named column groups; each contributes one output row per source row.</param>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> Unpivot<TSource, TValue, TResult>(
			this            IQueryable<TSource>                                        source,
			[InstantHandle] Expression<Func<TSource, string, TValue, TValue, TResult>> resultSelector,
			[InstantHandle] params (string name, Expression<Func<TSource, TValue>> column1, Expression<Func<TSource, TValue>> column2)[] groups)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(resultSelector);
			ArgumentNullException.ThrowIfNull(groups);

			return BuildMultiValueUnpivot<TSource, TValue, TResult>(source, resultSelector,
				groups.Select(static g => (g.name, new LambdaExpression[] { g.column1, g.column2 })).ToArray());
		}

		/// <summary>
		/// Multi-value UNPIVOT (three value columns per group): rotates named groups of three columns into rows.
		/// Lowered to a portable <c>UNION ALL</c> derived table on every provider; NULL rows are kept.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <typeparam name="TValue">Common type of the unpivoted columns.</typeparam>
		/// <typeparam name="TResult">Result record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="resultSelector">Projection over the source row, the group name, and the three column values.</param>
		/// <param name="groups">The named column groups; each contributes one output row per source row.</param>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> Unpivot<TSource, TValue, TResult>(
			this            IQueryable<TSource>                                                source,
			[InstantHandle] Expression<Func<TSource, string, TValue, TValue, TValue, TResult>> resultSelector,
			[InstantHandle] params (string name, Expression<Func<TSource, TValue>> column1, Expression<Func<TSource, TValue>> column2, Expression<Func<TSource, TValue>> column3)[] groups)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(resultSelector);
			ArgumentNullException.ThrowIfNull(groups);

			return BuildMultiValueUnpivot<TSource, TValue, TResult>(source, resultSelector,
				groups.Select(static g => (g.name, new LambdaExpression[] { g.column1, g.column2, g.column3 })).ToArray());
		}

		static readonly MethodInfo _unpivotMultiMethodInfo =
			typeof(LinqExtensions).GetMethod(nameof(UnpivotMulti), BindingFlags.Static | BindingFlags.NonPublic)!;

		// Emits the UnpivotMulti marker call; UnpivotBuilder rewrites it to native multi-value UNPIVOT
		// (Oracle/DuckDB) or a portable UNION ALL derived table.
		static IQueryable<TResult> BuildMultiValueUnpivot<TSource, TValue, TResult>(
			IQueryable<TSource>                       source,
			LambdaExpression                          resultSelector,
			(string name, LambdaExpression[] columns)[] groups)
		{
			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_unpivotMultiMethodInfo.MakeGenericMethod(typeof(TSource), typeof(TValue), typeof(TResult)),
				currentSource.Expression,
				Expression.Quote(resultSelector),
				Expression.Constant(groups, typeof((string, LambdaExpression[])[])));

			return currentSource.Provider.CreateQuery<TResult>(expr);
		}

		// Query marker for multi-value UNPIVOT — never executed; recognized by UnpivotBuilder.
		internal static IQueryable<TResult> UnpivotMulti<TSource, TValue, TResult>(
			IQueryable<TSource>                       source,
			LambdaExpression                          resultSelector,
			(string name, LambdaExpression[] columns)[] groups)
			=> throw new InvalidOperationException("UnpivotMulti is a query marker and must not be invoked directly.");
	}
}
