using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	sealed class FilterIgnoreScope : IEquatable<FilterIgnoreScope>
	{
		// Both dimensions are used as sets (membership tests via Array.IndexOf). Normalise to a canonical order
		// at construction time so {"A","B"} and {"B","A"} hash and compare equal, which keeps TranslationModifier's
		// equality-based cache from missing on logically-equivalent inputs.
		public FilterIgnoreScope(string[]? keys, Type[]? types)
		{
			Keys  = Normalize(keys, StringComparer.Ordinal);
			Types = Normalize(types, TypeComparer.Instance);
		}

		static T[]? Normalize<T>(T[]? array, IComparer<T> comparer) where T : class
		{
			if (array == null || array.Length == 0)
				return array;

			if (array.Length == 1)
				return array;

			var unique = new HashSet<T>(array);
			var sorted = new T[unique.Count];
			unique.CopyTo(sorted);
			Array.Sort(sorted, comparer);
			return sorted;
		}

		sealed class TypeComparer : IComparer<Type>
		{
			public static readonly TypeComparer Instance = new();

			public int Compare(Type? x, Type? y)
			{
				if (ReferenceEquals(x, y)) return 0;
				if (x is null)             return -1;
				if (y is null)             return  1;

				return string.CompareOrdinal(x.AssemblyQualifiedName ?? x.FullName ?? x.Name,
				                             y.AssemblyQualifiedName ?? y.FullName ?? y.Name);
			}
		}

		/// <summary>
		/// Filter-key dimension. <see langword="null"/> or empty means "any key".
		/// </summary>
		public string[]? Keys  { get; }

		/// <summary>
		/// Entity-type dimension. <see langword="null"/> or empty means "any type".
		/// </summary>
		public Type[]?   Types { get; }

		public bool MatchesAnyKey() => Keys == null || Keys.Length == 0;

		public bool MatchesKey(string filterKey)
		{
			if (Keys == null || Keys.Length == 0)
				return true;

			return Array.IndexOf(Keys, filterKey) >= 0;
		}

		public bool MatchesType(Type entityType)
		{
			if (Types == null || Types.Length == 0)
				return true;

			return Array.IndexOf(Types, entityType) >= 0;
		}

		public bool Equals(FilterIgnoreScope? other)
		{
			if (other is null)
				return false;

			if (ReferenceEquals(this, other))
				return true;

			return ArrayEquals(Keys, other.Keys) && ArrayEquals(Types, other.Types);
		}

		public override bool Equals(object? obj) => obj is FilterIgnoreScope other && Equals(other);

		public override int GetHashCode()
		{
			var hashCode = new HashCode();

			if (Keys != null)
				foreach (var k in Keys)
					hashCode.Add(k);

			if (Types != null)
				foreach (var t in Types)
					hashCode.Add(t);

			return hashCode.ToHashCode();
		}

		static bool ArrayEquals<T>(T[]? left, T[]? right)
		{
			if (ReferenceEquals(left, right))
				return true;

			if (left == null || right == null)
				return false;

			return left.SequenceEqual(right);
		}
	}
}
