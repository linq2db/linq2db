using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Extensions;

namespace LinqToDB
{
	/// <summary>
	/// A cell template for <see cref="PivotBuilder{TSource,TFor}.Cells"/>: an aggregate built outside the query, or a set
	/// of cells decided when the query runs.
	/// </summary>
	/// <typeparam name="TSource">Grouped row type.</typeparam>
	/// <typeparam name="TFor">Type of the pivoted column.</typeparam>
	public sealed class PivotCell<TSource, TFor>
	{
		PivotCell(LambdaExpression aggregate, bool liftResult, Func<TFor, string>? name)
		{
			Aggregate  = aggregate;
			LiftResult = liftResult;
			Name       = name;
		}

		// Func<IEnumerable<TSource>, TCell>, applied to the rows carrying one pivoted value.
		internal LambdaExpression    Aggregate  { get; }
		internal bool                LiftResult { get; }
		internal Func<TFor, string>? Name       { get; }

		/// <summary>
		/// Creates a cell computed by an aggregate over the group's rows carrying a pivoted value, with the same rules as
		/// <see cref="PivotBuilder{TSource,TFor}.Cell{TCell}"/>.
		/// </summary>
		/// <typeparam name="TCell">Aggregate result type.</typeparam>
		/// <param name="aggregate">Aggregate over the matching rows.</param>
		/// <param name="name">Names the cell for a pivoted value; defaults to the value's invariant string form.</param>
		/// <returns>The cell template.</returns>
		public static PivotCell<TSource, TFor> Create<TCell>(Expression<Func<IEnumerable<TSource>, TCell>> aggregate, Func<TFor, string>? name = null)
		{
			ArgumentNullException.ThrowIfNull(aggregate);

			// A cell no row matches must read null rather than default(TCell).
			return new(aggregate, !typeof(TCell).IsNullableOrReferenceType, name);
		}
	}
}
