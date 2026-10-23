using System;
using System.Collections.Generic;
using System.Globalization;

using JetBrains.Annotations;

using LinqToDB.Internal.Common;

namespace LinqToDB
{
	/// <summary>
	/// A pivot of a group, started by <see cref="LinqExtensions.Pivot{TKey,TSource,TFor}"/>. Add cells with
	/// <see cref="Cell{TCell}"/> or <see cref="Cells"/> and end the chain with <see cref="AsDictionary"/> or
	/// <see cref="AsDynamic"/>, or project each pivoted value with <see cref="Select{T}"/>. The chain is translated to SQL
	/// as part of a query; every member throws when invoked in memory.
	/// </summary>
	/// <typeparam name="TSource">Grouped row type.</typeparam>
	/// <typeparam name="TFor">Type of the pivoted column.</typeparam>
	public sealed class PivotBuilder<TSource, TFor>
	{
		PivotBuilder()
		{
		}

		/// <summary>
		/// Adds a cell per pivoted value, computed by an aggregate over the group's rows carrying that value -
		/// <c>SUM</c>, <c>MAX</c>, a distinct count, or anything else the provider can translate over a grouping. A cell no
		/// row matches reads what the aggregate yields over no rows: <see langword="null"/> rather than
		/// <c>default(TCell)</c> for <c>SUM</c>, <c>MIN</c>, <c>MAX</c> and <c>AVG</c>, but <c>0</c> for a count.
		/// </summary>
		/// <typeparam name="TCell">Aggregate result type.</typeparam>
		/// <param name="aggregate">
		/// Aggregate over the matching rows, written inline. One built elsewhere is passed through <see cref="Cells"/> and
		/// <see cref="PivotCell{TSource,TFor}.Create{TCell}"/>.
		/// </param>
		/// <param name="name">
		/// Names the cell for a pivoted value; defaults to the value's invariant string form. Required on every cell when
		/// the pivot declares more than one. It runs when the query is built, once per pivoted value, and may not read the
		/// group or another query parameter.
		/// </param>
		/// <returns>The builder with the cell added.</returns>
		[Pure]
		public PivotBuilder<TSource, TFor> Cell<TCell>(Func<IEnumerable<TSource>, TCell> aggregate, Func<TFor, string>? name = null)
			=> throw InvokedInMemory(nameof(Cell));

		/// <summary>
		/// Adds a cell per pivoted value for each of <paramref name="cells"/>, a set that can be decided when the query
		/// runs. Each cell follows the rules of <see cref="Cell{TCell}"/>.
		/// </summary>
		/// <param name="cells">
		/// The cell templates, read when the query is built: each execution reads the current set. They may not read the
		/// group or another query parameter.
		/// </param>
		/// <returns>The builder with the cells added.</returns>
		[Pure]
		public PivotBuilder<TSource, TFor> Cells([InstantHandle] IEnumerable<PivotCell<TSource, TFor>> cells)
			=> throw InvokedInMemory(nameof(Cells));

		/// <summary>Ends the pivot with its cells, addressable by name.</summary>
		/// <returns>The cells of one group.</returns>
		[Pure]
		public PivotCells AsDictionary()
			=> throw InvokedInMemory(nameof(AsDictionary));

		/// <summary>
		/// Ends the pivot with its cells, read as dynamic members (<c>row.Cells.Y2010</c>); a name no cell carries throws
		/// the runtime binder's exception. The object is a <see cref="PivotCells"/>.
		/// </summary>
		/// <returns>The cells of one group.</returns>
		[Pure]
		public dynamic AsDynamic()
			=> throw InvokedInMemory(nameof(AsDynamic));

		/// <summary>
		/// Ends the pivot with one statically typed item per pivoted value, in value order, projected by
		/// <paramref name="selector"/> from the value and the group's rows carrying it. It takes no cells.
		/// </summary>
		/// <typeparam name="T">Item type.</typeparam>
		/// <param name="selector">
		/// Projects a pivoted value and its rows, written inline. The members' types are the selector's own, so an
		/// aggregate no row matches reads what its declared type materializes from <c>NULL</c>: <c>0</c> for a
		/// non-nullable <c>rows.Sum(x =&gt; x.Amount)</c>, <see langword="null"/> once lifted as
		/// <c>rows.Sum(x =&gt; (decimal?)x.Amount)</c>.
		/// </param>
		/// <returns>One item per pivoted value.</returns>
		[Pure]
		public IReadOnlyList<T> Select<T>(Func<TFor, IEnumerable<TSource>, T> selector)
			=> throw InvokedInMemory(nameof(Select));

		static LinqToDBException InvokedInMemory(string member)
			=> new(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_QueryMarkerInvoked, member));
	}
}
