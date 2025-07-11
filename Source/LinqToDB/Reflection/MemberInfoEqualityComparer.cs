using System;
using System.Collections.Generic;
using System.Reflection;

namespace LinqToDB.Reflection
{
	public class MemberInfoEqualityComparer : IEqualityComparer<MemberInfo>
	{
		public static readonly MemberInfoEqualityComparer Default = new();

		public bool Equals(MemberInfo? x, MemberInfo? y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (ReferenceEquals(x, null))
			{
				return false;
			}

			if (ReferenceEquals(y, null))
			{
				return false;
			}

			if (x.GetType() != y.GetType())
			{
				return false;
			}

			if (x is VirtualPropertyInfoBase xv)
			{
				return xv.Equals(y);
			}

			return x.MetadataToken == y.MetadataToken && x.Module.Equals(y.Module);
		}

		public int GetHashCode(MemberInfo obj)
		{
			// We do not support obj.MetadataToken and obj.Module
			if (obj is VirtualPropertyInfoBase)
				return obj.GetHashCode();

			return HashCode.Combine(obj.MetadataToken, obj.Module);
		}
	}
}
