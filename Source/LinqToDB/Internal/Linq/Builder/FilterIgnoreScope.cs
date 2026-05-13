using System;
using System.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	sealed class FilterIgnoreScope : IEquatable<FilterIgnoreScope>
	{
		public FilterIgnoreScope(string[]? keys, Type[]? types)
		{
			Keys  = keys;
			Types = types;
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
