using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;

using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

namespace LinqToDB
{
	/// <summary>
	/// The cells of one group of a pivot, as <see cref="PivotBuilder{TSource,TFor}.AsDictionary"/> and
	/// <see cref="PivotBuilder{TSource,TFor}.AsDynamic"/> return them, addressed by the name each was given.
	/// </summary>
	/// <remarks>
	/// A result type can declare the member as <see cref="IReadOnlyDictionary{TKey,TValue}"/>. That view follows the
	/// dictionary contract: its indexer throws <see cref="KeyNotFoundException"/> for a name no cell carries, where
	/// <c>cells[name]</c> on this type returns <see langword="null"/>. Read as <see langword="dynamic"/>, a name no cell
	/// carries throws the runtime binder's exception. In a query, a cell is a column:
	/// <c>Sql.Property&lt;decimal?&gt;(row.Cells, "Y2010")</c> filters or orders on it.
	/// </remarks>
	[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "named for its pivot role; the dictionary view is secondary")]
	public sealed class PivotCells : DynamicObject, IReadOnlyDictionary<string, object?>
	{
		internal PivotCells()
		{
		}

		IDictionary<string, object?>? _values;

		/// <summary>The cells, keyed by the name each pivoted value was given; empty when there are none.</summary>
		[DynamicColumnsStore]
		public IDictionary<string, object?> Store
		{
			get => _values ??= new Dictionary<string, object?>();
			private set => _values = value;
		}

		/// <summary>Reads a cell, or <see langword="null"/> when no cell carries <paramref name="name"/>.</summary>
		/// <param name="name">Cell name.</param>
		public object? this[string name] => Store.TryGetValue(name, out var value) ? value : null;

		/// <summary>Reads a cell as <typeparamref name="T"/>, or <see langword="default"/> when absent or of another type.</summary>
		/// <typeparam name="T">Expected cell type.</typeparam>
		/// <param name="name">Cell name.</param>
		public T? Get<T>(string name) => this[name] is T value ? value : default;

		#region DynamicObject

		/// <inheritdoc/>
		public override bool TryGetMember(GetMemberBinder binder, out object? result)
		{
			ArgumentNullException.ThrowIfNull(binder);

			return Store.TryGetValue(binder.Name, out result);
		}

		/// <inheritdoc/>
		public override IEnumerable<string> GetDynamicMemberNames() => Store.Keys;

		#endregion

		#region IReadOnlyDictionary

		int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => Store.Count;

		IEnumerable<string>  IReadOnlyDictionary<string, object?>.Keys   => Store.Keys;
		IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => Store.Values;

		object? IReadOnlyDictionary<string, object?>.this[string key] => Store.TryGetValue(key, out var value)
			? value
			: throw new KeyNotFoundException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_PivotCellNotFound, key));

		bool IReadOnlyDictionary<string, object?>.ContainsKey(string key) => Store.ContainsKey(key);

		bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value)
		{
			if (Store.TryGetValue(key, out var found))
			{
				value = found;
				return true;
			}

			value = null;
			return false;
		}

		IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
			=> Store.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> ((IEnumerable<KeyValuePair<string, object?>>)this).GetEnumerator();

		#endregion
	}
}
