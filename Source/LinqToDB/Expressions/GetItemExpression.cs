using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	class GetItemExpression : Expression
	{
		public GetItemExpression(Expression expression)
		{
			Expression = expression;
			_type      = expression.Type.GetGenericArguments()[0];
		}

		readonly Type       _type;

		public          Expression     Expression { get; }
		public override Type           Type       { get { return _type;                    } }
		public override ExpressionType NodeType   { get { return ExpressionType.Extension; } }
		public override bool           CanReduce  { get { return true;                     } }

		public override Expression Reduce()
		{
			var mi = MemberHelper.MethodOf(() => Enumerable.First<string>(null));
			var gi = mi.GetGenericMethodDefinition().MakeGenericMethod(_type);

			return Call(null, gi, Expression);
		}
	}
}
