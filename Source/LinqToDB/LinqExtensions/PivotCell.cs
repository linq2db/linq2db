using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	/// <summary>
	/// One cell template of a dynamic pivot: an aggregate over a source column, crossed with every pivoted
	/// value to produce one output column per (template, value) pair. Declared statically - only the pivoted
	/// values are runtime data.
	/// </summary>
	/// <typeparam name="TSource">Source record type.</typeparam>
	/// <typeparam name="TFor">Type of the pivoted (<c>FOR</c>) column.</typeparam>
	public sealed class PivotCell<TSource, TFor>
	{
		PivotCell(PivotAggregate aggregate, LambdaExpression value, Func<TFor, string>? name)
		{
			Aggregate = aggregate;
			Value     = value;
			Name      = name;
		}

		internal PivotAggregate      Aggregate { get; }
		internal LambdaExpression    Value     { get; }
		internal Func<TFor, string>? Name      { get; }

		/// <summary>A <c>SUM</c> cell.</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value. Required when a pivot declares more than one cell.</param>
		public static PivotCell<TSource, TFor> Sum<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> Create(PivotAggregate.Sum, value, name);

		/// <summary>A <c>MIN</c> cell (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public static PivotCell<TSource, TFor> Min<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> Create(PivotAggregate.Min, value, name);

		/// <summary>A <c>MAX</c> cell (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public static PivotCell<TSource, TFor> Max<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> Create(PivotAggregate.Max, value, name);

		/// <summary>An <c>AVG</c> cell (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public static PivotCell<TSource, TFor> Avg<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> Create(PivotAggregate.Avg, value, name);

		/// <summary>A <c>COUNT</c> cell: counts the rows matching the pivoted value (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Type of the referenced column; only the row match is counted.</typeparam>
		/// <param name="value">Column the cell is associated with.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public static PivotCell<TSource, TFor> Count<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> Create(PivotAggregate.Count, value, name);

		static PivotCell<TSource, TFor> Create<TCell>(PivotAggregate aggregate, Expression<Func<TSource, TCell>> value, Func<TFor, string>? name)
		{
			ArgumentNullException.ThrowIfNull(value);

			return new PivotCell<TSource, TFor>(aggregate, value, name);
		}
	}
}
