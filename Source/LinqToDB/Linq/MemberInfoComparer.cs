using System;
using System.Collections.Generic;
using System.Reflection;

namespace LinqToDB.Linq
{
	using Extensions;

	class MemberInfoComparer : IEqualityComparer<MemberInfo>
	{
		public static MemberInfoComparer Instance = new MemberInfoComparer();

		public bool Equals(MemberInfo? x, MemberInfo? y)
		{
			if (x == null && y == null) return true;
			if (x == null || y == null) return false;
			return x.EqualsTo(y);
		}

		public int GetHashCode(MemberInfo obj)
		{
			return obj == null ? 0 : obj.Name.GetHashCode();
		}
	}
}
