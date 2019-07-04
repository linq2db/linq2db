﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Tools.Comparers
{
	class EnumerableEqualityComparer : EqualityComparer<IEnumerable>
	{
		public new static EnumerableEqualityComparer Default { get; } = new EnumerableEqualityComparer();

		public override int GetHashCode(IEnumerable obj)
		{
			if (obj == null)
				return 0;

			return obj.Cast<object>().Aggregate(0, (acc, val) => acc ^ val.GetHashCode());
		}

		public override bool Equals(IEnumerable x, IEnumerable y)
		{
			if (x == null && y == null)
				return true;

			if (x == null || y == null)
				return false;

			return x.Cast<object>().SequenceEqual(y.Cast<object>());
		}
	}
}
