using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	class LoadWithInfo
	{
		public LoadWithInfo(MemberInfo memberInfo)
		{
			MemberInfo = memberInfo;
		}

		public MemberInfo        MemberInfo   { get; }
		public LambdaExpression? MemberFilter { get; set; }
		public Expression?       FilterFunc   { get; set; }

		protected bool Equals(LoadWithInfo other)
		{
			return MemberInfo.Equals(other.MemberInfo);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((LoadWithInfo)obj);
		}

		public override int GetHashCode()
		{
			return MemberInfo.GetHashCode();
		}
	}
}
