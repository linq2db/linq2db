using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Extensions;

namespace LinqToDB
{
	/// <summary>
	/// One cell template of a dynamic pivot: an aggregate over the source rows carrying a pivoted value, crossed
	/// with every pivoted value to produce one output column per (template, value) pair. Declared statically -
	/// only the pivoted values are runtime data. Built through
	/// <see cref="PivotCellFactory{TSource,TFor}.Cell{TCell}"/>.
	/// </summary>
	/// <typeparam name="TSource">Source record type.</typeparam>
	/// <typeparam name="TFor">Type of the pivoted (<c>FOR</c>) column.</typeparam>
	public sealed class PivotCell<TSource, TFor>
	{
		PivotCell(LambdaExpression aggregate, bool liftResult, Func<TFor, string>? name)
		{
			Aggregate  = aggregate;
			LiftResult = liftResult;
			Name       = name;
		}

		// Func<IEnumerable<TSource>, TCell>, applied to the rows matching one pivoted value.
		internal LambdaExpression    Aggregate  { get; }
		internal bool                LiftResult { get; }
		internal Func<TFor, string>? Name       { get; }

		internal static PivotCell<TSource, TFor> Cell<TCell>(Expression<Func<IEnumerable<TSource>, TCell>> aggregate, Func<TFor, string>? name = null)
		{
			ArgumentNullException.ThrowIfNull(aggregate);

			// A cell no row matches must read null rather than default(TCell), which is what a non-nullable
			// result type would otherwise materialize.
			return new PivotCell<TSource, TFor>(aggregate, !typeof(TCell).IsNullableOrReferenceType, name);
		}
	}
}
