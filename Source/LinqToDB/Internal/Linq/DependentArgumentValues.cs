using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Values a compiled table's query was built from, for the arguments its expression materialises.
	/// The cache key is compared through the tuple's per-field default comparer, which would compare an
	/// array by reference - hence a holder rather than the array itself. Values compare element-wise, the
	/// way SqlQueryDependentAttribute.ObjectsEqual compares the same values everywhere else.
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
				if (!ValuesEqual(_values[i], other._values[i]))
					return false;

			return true;
		}

		public override bool Equals([NotNullWhen(true)] object? obj) => Equals(obj as DependentArgumentValues);

		public override int GetHashCode()
		{
			var hash = _values.Length;

			foreach (var value in _values)
				hash = unchecked(hash * 397 + ValueHashCode(value));

			return hash;
		}

		static bool ValuesEqual(object? value1, object? value2)
		{
			if (ReferenceEquals(value1, value2))
				return true;

			if (value1 == null || value2 == null)
				return false;

			if (value1 is not string and IEnumerable list1 && value2 is not string and IEnumerable list2)
			{
				var enum1 = list1.GetEnumerator();
				var enum2 = list2.GetEnumerator();

				using (enum1 as IDisposable)
				using (enum2 as IDisposable)
				{
					while (enum1.MoveNext())
						if (!enum2.MoveNext() || !ValuesEqual(enum1.Current, enum2.Current))
							return false;

					return !enum2.MoveNext();
				}
			}

			return value1.Equals(value2);
		}

		static int ValueHashCode(object? value)
		{
			if (value is not string and IEnumerable list)
			{
				var hash = 17;

				foreach (var item in list)
					hash = unchecked(hash * 397 + ValueHashCode(item));

				return hash;
			}

			return value?.GetHashCode() ?? 0;
		}
	}
}
