using System;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Tools.Mapper
{
	public class MemberMapperInfo
	{
		[NotNull] public LambdaExpression ToMember { get; set; }
		[NotNull] public LambdaExpression Setter   { get; set; }
	}
}
