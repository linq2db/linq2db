using System;
using System.Linq.Expressions;

namespace LinqToDB.Data.Linq.Builder
{
	class ChangeTypeExpression : Expression
	{
		/////// Check Expression Visitor

		public const int ChangeTypeType = 1000;

#if FW4 || SILVERLIGHT

		public ChangeTypeExpression(Expression expression, Type type)
		{
			Expression = expression;
			_type       = type;
		}

		readonly Type _type;

		public override   Type           Type     { get { return _type;                          } }
		public override   ExpressionType NodeType { get { return (ExpressionType)ChangeTypeType; } }

#else

		public ChangeTypeExpression(Expression expression, Type type)
			: base((ExpressionType)ChangeTypeType, type)
		{
			Expression = expression;
		}

#endif

		public Expression Expression { get; private set; }

		public override string ToString()
		{
			return "(" + Type + ")" + Expression;
		}
	}
}
