using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB
{
	/// <summary>
	/// Builds the cell templates of a dynamic pivot. It is handed to the cell lambdas of the
	/// <see cref="LinqExtensions"/> <c>Pivot</c> overloads, by which point both type arguments are already inferred
	/// from the query - so a pivot over a join projected into an anonymous type works, where naming
	/// <typeparamref name="TSource"/> would be impossible.
	/// </summary>
	/// <typeparam name="TSource">Source record type.</typeparam>
	/// <typeparam name="TFor">Type of the pivoted (<c>FOR</c>) column.</typeparam>
	public sealed class PivotCellFactory<TSource, TFor>
	{
		internal static readonly PivotCellFactory<TSource, TFor> Instance = new();

		PivotCellFactory()
		{
		}

		/// <summary>
		/// A cell computed by an aggregate over the source rows carrying the pivoted value - <c>SUM</c>, <c>MAX</c>,
		/// a distinct count, or anything else the provider can translate over a grouping. A cell no row matches
		/// reads <see langword="null"/>.
		/// </summary>
		/// <typeparam name="TCell">Aggregate result type.</typeparam>
		/// <param name="aggregate">Aggregate over the matching rows.</param>
		/// <param name="name">Names the generated column for a pivoted value. Required when a pivot declares more than one cell.</param>
		public PivotCell<TSource, TFor> Cell<TCell>(Expression<Func<IEnumerable<TSource>, TCell>> aggregate, Func<TFor, string>? name = null)
			=> PivotCell<TSource, TFor>.Cell(aggregate, name);
	}
}
