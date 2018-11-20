using System.Collections;
using System.Collections.Generic;

namespace Tests.Tools
{
	internal class BitArrayEqualityComparer : EqualityComparer<BitArray>
	{
		public static new BitArrayEqualityComparer Default { get; } = new BitArrayEqualityComparer();

		public override int GetHashCode(BitArray obj)
		{
			if (obj == null)
				return 0;

			var hash = obj.Length.GetHashCode();

			for (var i = 0; i < obj.Length; i++)
				hash ^= obj[i].GetHashCode();

			return hash;
		}

		public override bool Equals(BitArray x, BitArray y)
		{
			if (x == null && y == null)
				return true;

			if (x == null || y == null)
				return false;

			if (x.Length != y.Length)
				return false;

			for (var i = 0; i < x.Length; i++)
				if (x[i] != y[i])
					return false;

			return true;
		}
	}
}
