using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Common;

	public class DefaultValueExpression : Expression
	{
		public DefaultValueExpression(Type type)
		{
			_type = type;
		}

		readonly Type _type;

		public override Type           Type      { get { return _type;                    } }
		public override ExpressionType NodeType  { get { return ExpressionType.Extension; } }
		public override bool           CanReduce { get { return true;                     } }

		public override Expression Reduce()
		{
			return Constant(DefaultValue.GetValue(Type), Type);
		}
	}
}
