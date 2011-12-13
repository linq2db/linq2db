using System;
using System.Collections.Generic;
using System.Reflection;

namespace LinqToDB.Data.Linq
{
	using Reflection;

	class MemberInfoComparer : IEqualityComparer<MemberInfo>
	{
		public bool Equals(MemberInfo x, MemberInfo y)
		{
			return TypeHelper.Equals(x, y);
		}

		public int GetHashCode(MemberInfo obj)
		{
			return obj == null ? 0 : obj.Name.GetHashCode();
		}
	}
}
