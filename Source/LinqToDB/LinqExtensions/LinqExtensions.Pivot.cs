using System;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.Linq;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Rotates distinct values of the <c>FOR</c> column(s) into columns, aggregating a value per
		/// (grouping, pivoted-value) cell. Emits native <c>PIVOT</c> on providers that support it
		/// (SQL Server, Oracle, DuckDB); everywhere else it is lowered to <c>GROUP BY</c> + conditional
		/// aggregation (<c>SUM(CASE WHEN …)</c>). The set of pivoted values must be a compile-time constant.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <typeparam name="TResult">Result record type; each pivoted column is a named member.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="pivot">
		/// Expression-bodied projection: pass-through / grouping columns come from <see cref="IPivotBuilder{TSource}.Key"/>,
		/// and each pivoted column is produced by an aggregate marker (e.g. <see cref="IPivotBuilder{TSource}.Sum{TValue,TFor}"/>).
		/// </param>
		/// <returns>Query with one row per grouping and one column per pivoted value.</returns>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> Pivot<TSource, TResult>(
			this            IQueryable<TSource>                               source,
			[InstantHandle] Expression<Func<IPivotBuilder<TSource>, TResult>> pivot)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(pivot);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Pivot, source, pivot),
				currentSource.Expression,
				Expression.Quote(pivot));

			return currentSource.Provider.CreateQuery<TResult>(expr);
		}
	}
}
