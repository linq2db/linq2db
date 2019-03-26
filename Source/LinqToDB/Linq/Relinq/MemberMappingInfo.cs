using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Relinq
{
	public class MemberMappingInfo
	{
		public MemberMappingInfo(MemberInfo memberInfo, Expression expression)
		{
			MemberInfo = memberInfo;
			Expression = expression;
			IsFullEntity = false;
		}

		public MemberMappingInfo(MemberInfo memberInfo)
		{
			MemberInfo = memberInfo;
			Expression = null;
			IsFullEntity = true;
		}

		public MemberInfo MemberInfo { get; set; }
		public Expression Expression { get; set; }
		public bool IsFullEntity { get; set; }
	}
}
