using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Values a compiled table's query was built from, for the arguments its expression materialises.
	/// The cache key is compared through the tuple's per-field default comparer, which would compare an
	/// array by reference - hence a holder with value equality rather than the array itself.
	/// </summary>
	sealed class DependentArgumentValues : IEquatable<DependentArgumentValues>
	{
		public static readonly DependentArgumentValues None = new([]);

		public DependentArgumentValues(object?[] values)
		{
			_values = values;
		}

		readonly object?[] _values;

		public bool Equals([NotNullWhen(true)] DependentArgumentValues? other)
		{
			if (other == null || other._values.Length != _values.Length)
				return false;

			for (var i = 0; i < _values.Length; i++)
				if (!Equals(_values[i], other._values[i]))
					return false;

			return true;
		}

		public override bool Equals([NotNullWhen(true)] object? obj) => Equals(obj as DependentArgumentValues);

		public override int GetHashCode()
		{
			var hash = _values.Length;

			foreach (var value in _values)
				hash = unchecked(hash * 397 + (value?.GetHashCode() ?? 0));

			return hash;
		}
	}
}
