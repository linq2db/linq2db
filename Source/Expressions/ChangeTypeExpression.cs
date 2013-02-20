using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	class ChangeTypeExpression : Expression
	{
		public const int ChangeTypeType = 1000;

		public ChangeTypeExpression(Expression expression, Type type)
		{
			Expression = expression;
			_type       = type;
		}

		readonly Type _type;

		public override Type           Type     { get { return _type;                          } }
		public override ExpressionType NodeType { get { return (ExpressionType)ChangeTypeType; } }

		public Expression Expression { get; private set; }

		public override string ToString()
		{
			return "(" + Type + ")" + Expression;
		}
	}
}
