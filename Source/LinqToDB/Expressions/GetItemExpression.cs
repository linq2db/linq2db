using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Extensions;

namespace LinqToDB.Expressions
{
	class GetItemExpression : Expression
	{
		public GetItemExpression(Expression expression)
		{
			_expression = expression;
			_type       = expression.Type.GetGenericArgumentsEx()[0];
		}

		readonly Expression _expression;
		readonly Type       _type;

		public          Expression     Expression { get { return _expression;              } }
		public override Type           Type       { get { return _type;                    } }
		public override ExpressionType NodeType   { get { return ExpressionType.Extension; } }
		public override bool           CanReduce  { get { return true;                     } }

		public override Expression Reduce()
		{
			var mi = MemberHelper.MethodOf(() => Enumerable.First<string>(null));
			var gi = mi.GetGenericMethodDefinition().MakeGenericMethod(_type);

			return Call(null, gi, _expression);
		}
	}
}
