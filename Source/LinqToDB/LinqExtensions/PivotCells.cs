using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB
{
	/// <summary>
	/// The cells of a dynamic pivot, in both of its roles. While the query is built it collects cell templates -
	/// each an aggregate over the source rows carrying one pivoted value, crossed with every pivoted value to
	/// produce one generated column per (template, value) pair. Once the query has run, the same type carries
	/// those generated cells, addressed by the name each was given.
	/// </summary>
	/// <typeparam name="TSource">Source record type.</typeparam>
	/// <typeparam name="TFor">Type of the pivoted (<c>FOR</c>) column.</typeparam>
	/// <remarks>
	/// It is handed to the cell lambda of the <see cref="LinqExtensions"/> <c>Pivot</c> overloads, by which point
	/// both type arguments are already inferred from the query - so a pivot over a join projected into an anonymous
	/// type works, where naming <typeparamref name="TSource"/> would be impossible.
	/// </remarks>
	public sealed class PivotCells<TSource, TFor>
	{
		internal sealed class Template
		{
			public Template(LambdaExpression aggregate, bool liftResult, Func<TFor, string>? name)
			{
				Aggregate  = aggregate;
				LiftResult = liftResult;
				Name       = name;
			}

			// Func<IEnumerable<TSource>, TCell>, applied to the rows matching one pivoted value.
			public LambdaExpression    Aggregate  { get; }
			public bool                LiftResult { get; }
			public Func<TFor, string>? Name       { get; }
		}

		internal static readonly PivotCells<TSource, TFor> Empty = new();

		readonly Template[] _templates;

		/// <summary>Creates an empty cell set.</summary>
		public PivotCells()
		{
			_templates = [];
		}

		PivotCells(Template[] templates)
		{
			_templates = templates;
		}

		internal Template[] Templates => _templates;

		/// <summary>
		/// Adds a cell computed by an aggregate over the source rows carrying the pivoted value - <c>SUM</c>,
		/// <c>MAX</c>, a distinct count, or anything else the provider can translate over a grouping. A cell no row
		/// matches reads <see langword="null"/>.
		/// </summary>
		/// <typeparam name="TCell">Aggregate result type.</typeparam>
		/// <param name="aggregate">Aggregate over the matching rows.</param>
		/// <param name="name">Names the generated column for a pivoted value. Required when a pivot declares more than one cell.</param>
		/// <returns>The cell set with <paramref name="aggregate"/> added.</returns>
		public PivotCells<TSource, TFor> Cell<TCell>(Expression<Func<IEnumerable<TSource>, TCell>> aggregate, Func<TFor, string>? name = null)
		{
			ArgumentNullException.ThrowIfNull(aggregate);

			var templates = new Template[_templates.Length + 1];

			Array.Copy(_templates, templates, _templates.Length);

			// A cell no row matches must read null rather than default(TCell), which is what a non-nullable
			// result type would otherwise materialize.
			templates[_templates.Length] = new Template(aggregate, !typeof(TCell).IsNullableOrReferenceType, name);

			return new PivotCells<TSource, TFor>(templates);
		}

		/// <summary>The generated cells, keyed by the name each pivoted value was given.</summary>
		[DynamicColumnsStore]
		public IDictionary<string, object> Values { get; set; } = null!;

		/// <summary>Reads a cell, or <see langword="null"/> when no cell carries <paramref name="name"/>.</summary>
		/// <param name="name">Cell name.</param>
		public object? this[string name] => Values != null && Values.TryGetValue(name, out var value) ? value : null;

		/// <summary>Reads a cell as <typeparamref name="T"/>, or <see langword="default"/> when absent or of another type.</summary>
		/// <typeparam name="T">Expected cell type.</typeparam>
		/// <param name="name">Cell name.</param>
		public T? Get<T>(string name) => this[name] is T value ? value : default;
	}
}
