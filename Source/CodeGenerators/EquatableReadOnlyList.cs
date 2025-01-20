using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CodeGenerators
{
	/// <summary></summary>
	[ExcludeFromCodeCoverage]
	public static class EquatableReadOnlyList
	{
		/// <summary></summary>
		public static EquatableReadOnlyList<T> ToEquatableReadOnlyList<T>(this IEnumerable<T>? enumerable)
			=> new((enumerable as IReadOnlyList<T>) ?? enumerable?.ToArray());
	}

	/// <summary>
	///     A wrapper for IReadOnlyList that provides value equality support for the wrapped list.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public readonly struct EquatableReadOnlyList<T>(
		IReadOnlyList<T>? collection
	) : IEquatable<EquatableReadOnlyList<T>>, IReadOnlyList<T>
	{
		private IReadOnlyList<T> Collection => collection ?? [];

		/// <summary></summary>
		public bool Equals(EquatableReadOnlyList<T> other)
			=> this.SequenceEqual(other);

		/// <summary></summary>
		public override bool Equals(object? obj)
			=> obj is EquatableReadOnlyList<T> other && Equals(other);

		/// <summary></summary>
		public override int GetHashCode()
		{
			var hashCode = new HashCode();

			foreach (var item in Collection)
				hashCode.Add(item);

			return hashCode.ToHashCode();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
			=> Collection.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> Collection.GetEnumerator();

		/// <summary></summary>
		public int Count => Collection.Count;
		/// <summary></summary>
		public T this[int index] => Collection[index];

		/// <summary></summary>
		public static bool operator ==(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
			=> left.Equals(right);

		/// <summary></summary>
		public static bool operator !=(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
			=> !left.Equals(right);
	}
}
