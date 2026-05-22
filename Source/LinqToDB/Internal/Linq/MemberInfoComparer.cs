using System;
using System.Collections.Generic;
using System.Reflection;

using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Linq
{
	sealed class MemberInfoComparer : IEqualityComparer<MemberInfo>
	{
		public static MemberInfoComparer Instance = new ();

		public bool Equals(MemberInfo? x, MemberInfo? y)
		{
			if (x == null && y == null) return true;
			if (x == null || y == null) return false;
			return x.EqualsTo(y);
		}

		public int GetHashCode(MemberInfo obj)
		{
			return StringComparer.Ordinal.GetHashCode(obj.Name);
		}
	}
}
