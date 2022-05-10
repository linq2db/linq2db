using System.Reflection;

namespace LinqToDB.Common;

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

		return x.MetadataToken == y.MetadataToken && x.Module.Equals(y.Module);
	}

	public int GetHashCode(MemberInfo obj)
	{
		unchecked
		{
			return (obj.MetadataToken * 397) ^ obj.Module.GetHashCode();
		}
	}
}
