using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	/// <summary>
	/// Creates <see cref="PivotCell{TSource,TFor}"/> templates where the source type cannot be written down - a
	/// join projected into an anonymous type, most of all. It is handed to the cell lambdas of the
	/// <see cref="LinqExtensions"/> <c>Pivot</c> overloads, by which point both type arguments are already
	/// inferred from the query.
	/// </summary>
	/// <typeparam name="TSource">Source record type.</typeparam>
	/// <typeparam name="TFor">Type of the pivoted (<c>FOR</c>) column.</typeparam>
	public sealed class PivotCellFactory<TSource, TFor>
	{
		internal static readonly PivotCellFactory<TSource, TFor> Instance = new();

		PivotCellFactory()
		{
		}

		/// <summary>A <c>SUM</c> cell.</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value. Required when a pivot declares more than one cell.</param>
		public PivotCell<TSource, TFor> Sum<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> PivotCell<TSource, TFor>.Sum(value, name);

		/// <summary>A <c>MIN</c> cell (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public PivotCell<TSource, TFor> Min<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> PivotCell<TSource, TFor>.Min(value, name);

		/// <summary>A <c>MAX</c> cell (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public PivotCell<TSource, TFor> Max<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> PivotCell<TSource, TFor>.Max(value, name);

		/// <summary>An <c>AVG</c> cell (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Aggregated value type.</typeparam>
		/// <param name="value">Column to aggregate.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public PivotCell<TSource, TFor> Avg<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> PivotCell<TSource, TFor>.Avg(value, name);

		/// <summary>A <c>COUNT</c> cell: counts the rows matching the pivoted value (see <see cref="Sum{TCell}"/>).</summary>
		/// <typeparam name="TCell">Type of the referenced column; only the row match is counted.</typeparam>
		/// <param name="value">Column the cell is associated with.</param>
		/// <param name="name">Names the generated column for a pivoted value.</param>
		public PivotCell<TSource, TFor> Count<TCell>(Expression<Func<TSource, TCell>> value, Func<TFor, string>? name = null)
			=> PivotCell<TSource, TFor>.Count(value, name);
	}
}
