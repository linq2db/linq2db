using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	public class SqlGenericConstructorExpression : Expression
	{
		public IReadOnlyCollection<Assignment> Assignments { get; }

		public class Assignment
		{
			public Assignment(MemberInfo memberInfo, Expression expression)
			{
				MemberInfo = memberInfo;
				Expression = expression;
			}

			public MemberInfo MemberInfo { get;  }
			public Expression Expression { get; }
		}

		public SqlGenericConstructorExpression(IReadOnlyCollection<Assignment> assignments)
		{
			Assignments = assignments;
		}
	}
}
