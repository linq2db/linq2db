using System;
using System.Collections.Generic;
using System.Reflection;
using LinqToDB.Extensions;

namespace LinqToDB.Data.Linq
{
	using Reflection;

	class MemberInfoComparer : IEqualityComparer<MemberInfo>
	{
		public bool Equals(MemberInfo x, MemberInfo y)
		{
			return ReflectionExtensions.Equals(x, y);
		}

		public int GetHashCode(MemberInfo obj)
		{
			return obj == null ? 0 : obj.Name.GetHashCode();
		}
	}
}
