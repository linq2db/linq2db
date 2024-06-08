using System.Linq.Expressions;

namespace LinqToDB.Tools.Mapper
{
	public class MemberMapperInfo
	{
		public LambdaExpression ToMember { get; set; } = null!;
		public LambdaExpression Setter   { get; set; } = null!;
	}
}
