using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	public class SqlGenericConstructorExpression : Expression
	{
		public ReadOnlyCollection<Assignment> Assignments { get; }

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

		public SqlGenericConstructorExpression(Type objectType, IList<Assignment> assignments)
		{
			ObjectType  = objectType;
			Assignments = new ReadOnlyCollection<Assignment>(assignments);
		}

		public Type ObjectType { get; }

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ObjectType;
	}
}
