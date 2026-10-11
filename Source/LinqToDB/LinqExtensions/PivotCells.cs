using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

using JetBrains.Annotations;

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
	/// type works, where naming <typeparamref name="TSource"/> would be impossible. A result type that cannot name
	/// it either can declare the member as <see cref="IReadOnlyDictionary{TKey,TValue}"/> instead.
	/// </remarks>
	public sealed class PivotCells<TSource, TFor> : IReadOnlyDictionary<string, object?>
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

		internal PivotCells()
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
		/// matches reads what the aggregate yields over no rows: <see langword="null"/> rather than
		/// <c>default(TCell)</c> for <c>SUM</c>, <c>MIN</c>, <c>MAX</c> and <c>AVG</c>, but <c>0</c> for a count.
		/// </summary>
		/// <typeparam name="TCell">Aggregate result type.</typeparam>
		/// <param name="aggregate">Aggregate over the matching rows.</param>
		/// <param name="name">
		/// Names the generated column for a pivoted value; defaults to the value's invariant string form. Required
		/// when a pivot declares more than one cell.
		/// </param>
		/// <returns>The cell set with <paramref name="aggregate"/> added.</returns>
		[Pure]
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

		IDictionary<string, object>? _values;

		/// <summary>The generated cells, keyed by the name each pivoted value was given; empty when there are none.</summary>
		[DynamicColumnsStore]
		public IDictionary<string, object> Values
		{
			get => _values ??= new Dictionary<string, object>();
			set => _values = value;
		}

		/// <summary>Reads a cell, or <see langword="null"/> when no cell carries <paramref name="name"/>.</summary>
		/// <param name="name">Cell name.</param>
		public object? this[string name] => Values.TryGetValue(name, out var value) ? value : null;

		/// <summary>Reads a cell as <typeparamref name="T"/>, or <see langword="default"/> when absent or of another type.</summary>
		/// <typeparam name="T">Expected cell type.</typeparam>
		/// <param name="name">Cell name.</param>
		public T? Get<T>(string name) => this[name] is T value ? value : default;

		#region IReadOnlyDictionary

		int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => Values.Count;

		IEnumerable<string>  IReadOnlyDictionary<string, object?>.Keys   => Values.Keys;
		IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => Values.Values;

		object? IReadOnlyDictionary<string, object?>.this[string key] => Values.TryGetValue(key, out var value)
			? value
			: throw new KeyNotFoundException($"No pivot cell is named '{key}'.");

		bool IReadOnlyDictionary<string, object?>.ContainsKey(string key) => Values.ContainsKey(key);

		bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value)
		{
			if (Values.TryGetValue(key, out var found))
			{
				value = found;
				return true;
			}

			value = null;
			return false;
		}

		IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
			=> ((IEnumerable<KeyValuePair<string, object?>>)Values).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> ((IEnumerable<KeyValuePair<string, object?>>)this).GetEnumerator();

		#endregion
	}
}
