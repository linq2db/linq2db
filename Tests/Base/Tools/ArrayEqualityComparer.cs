using System.Collections.Generic;
using System.Linq;

namespace Tests.Tools
{
	internal class ArrayEqualityComparer<T> : EqualityComparer<T[]>
	{
		public static new ArrayEqualityComparer<T> Default { get; } = new ArrayEqualityComparer<T>();

		public override int GetHashCode(T[] obj)
		{
			if (obj == null)
				return 0;

			return obj.Aggregate(0, (acc, val) => acc ^ val.GetHashCode());
		}

		public override bool Equals(T[] x, T[] y)
		{
			if (x == null && y == null)
				return true;

			if (x == null || y == null)
				return false;

			return x.SequenceEqual(y);
		}
	}
}
