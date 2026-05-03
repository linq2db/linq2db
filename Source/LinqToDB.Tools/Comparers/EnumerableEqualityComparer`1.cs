using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Tools.Comparers
{
	sealed class EnumerableEqualityComparer<T> : EqualityComparer<IEnumerable<T>>
	{
		public static new EnumerableEqualityComparer<T> Default { get; } = new EnumerableEqualityComparer<T>();

		private readonly IEqualityComparer<T> _elementComparer;

		public EnumerableEqualityComparer() : this(EqualityComparer<T>.Default)
		{
		}

		public EnumerableEqualityComparer(IEqualityComparer<T> elementComparer)
		{
			_elementComparer = elementComparer ?? throw new ArgumentNullException(nameof(elementComparer));
		}

		public override int GetHashCode(IEnumerable<T> obj)
		{
			if (obj == null)
				return 0;

			return obj.Aggregate(0, (acc, val) => acc ^ _elementComparer.GetHashCode(val!));
		}

		public override bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
		{
			if (x == null && y == null)
				return true;

			if (x == null || y == null)
				return false;

			return x.SequenceEqual(y, _elementComparer);
		}
	}
}
