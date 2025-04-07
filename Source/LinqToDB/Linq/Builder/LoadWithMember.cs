using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	[DebuggerDisplay("{ToDebugString()}")]
	sealed class LoadWithMember
	{
		public LoadWithMember(MemberInfo memberInfo)
		{
			MemberInfo   = memberInfo;
		}

		public LoadWithEntity?   Entity           { get; set; }
		public MemberInfo        MemberInfo       { get; }
		public LambdaExpression? FilterExpression { get; set; }
		public Expression?       FilterFunc       { get; set; }

		bool Equals(LoadWithMember other)
		{
			return Equals(MemberInfo, other.MemberInfo);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((LoadWithMember)obj);
		}

		public override int GetHashCode()
		{
			return MemberInfo?.GetHashCode() ?? 0;
		}

		public string ToDebugString()
		{
			var str = MemberInfo?.Name ?? "[empty]";

			if (FilterFunc != null)
				str += "(FF)";

			if (FilterExpression != null)
				str += "(FE)";

			return str;
		}
	}
}
