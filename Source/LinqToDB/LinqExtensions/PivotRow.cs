using System.Collections.Generic;

using LinqToDB.Mapping;

namespace LinqToDB
{
	/// <summary>
	/// Result row of a dynamic pivot: a typed grouping key plus the generated cells, addressed by name.
	/// Use it when the pivoted column set is decided at query-build time and there is no compile-time type to
	/// project into; supply your own result type with a <see cref="DynamicColumnsStoreAttribute"/> member when
	/// you want named static members alongside the cells.
	/// </summary>
	/// <typeparam name="TKey">Type of the grouping key. May be a composite (anonymous or tuple) type.</typeparam>
	public sealed class PivotRow<TKey>
	{
		/// <summary>The grouping key this row aggregates.</summary>
		public TKey Key { get; set; } = default!;

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
