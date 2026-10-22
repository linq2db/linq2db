using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using JetBrains.Annotations;

using LinqToDB.Internal.Common;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Starts a pivot of a group: the rows of <paramref name="source"/> are split by the value of
		/// <paramref name="forColumn"/>, and each of <paramref name="values"/> becomes a cell, or an item, of the
		/// projection. The value set is decided when the query runs; every cell stays a statically typed aggregate.
		/// </summary>
		/// <typeparam name="TKey">Grouping key type.</typeparam>
		/// <typeparam name="TSource">Grouped row type.</typeparam>
		/// <typeparam name="TFor">Type of the pivoted column.</typeparam>
		/// <param name="source">The group, as a <c>GroupBy(...).Select(g =&gt; ...)</c> projection receives it.</param>
		/// <param name="forColumn">The column whose values become cells. Written inline.</param>
		/// <param name="values">
		/// The pivoted values, read when the query is built: each execution reads the current set, and a different set is
		/// a different query. It may not read the group or another query parameter. In a compiled query, pass it as an
		/// argument of the compiled delegate: a set the delegate captures is read when the query is first built and not
		/// again.
		/// </param>
		/// <returns>
		/// A builder to add cells to with <see cref="PivotBuilder{TSource,TFor}.Cell{TCell}"/> or
		/// <see cref="PivotBuilder{TSource,TFor}.Cells"/>, ended by <see cref="PivotBuilder{TSource,TFor}.AsDictionary"/>,
		/// <see cref="PivotBuilder{TSource,TFor}.AsDynamic"/> or <see cref="PivotBuilder{TSource,TFor}.Select{T}"/>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// The pivot is translated to SQL as part of a query - one filtered aggregate per (cell, value) pair in the
		/// group's <c>GROUP BY</c> - and throws when invoked in memory.
		/// </para>
		/// <para>
		/// For a value set known at compile time, an ordinary
		/// <c>GroupBy(...).Select(g =&gt; new { ..., V2000 = g.Where(x =&gt; x.Year == 2000).Sum(x =&gt; x.Amount) })</c>
		/// expresses the same query with statically typed members and needs none of this.
		/// </para>
		/// </remarks>
		[Pure]
		public static PivotBuilder<TSource, TFor> Pivot<TKey, TSource, TFor>(
			this            IGrouping<TKey, TSource> source,
			                Func<TSource, TFor>      forColumn,
			[InstantHandle] IEnumerable<TFor>        values)
			=> throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_QueryMarkerInvoked, nameof(Pivot)));
	}
}
