namespace LinqToDB.Expressions
{
	using System;
	using System.Linq;
	using System.Linq.Expressions;

	using LinqToDB.Extensions;

	public class BinaryAggregateExpression : Expression
	{
		public const ExpressionType AggregateExpressionType = (ExpressionType)1001;

		public BinaryAggregateExpression(ExpressionType aggregateType, Type type, Expression[] expressions)
		{
			_aggregateType = aggregateType;
			_expressions   = expressions;
			_type          = type;
		}

		readonly ExpressionType _aggregateType;
		readonly Expression[]   _expressions;
		readonly Type           _type;

		public          Expression[]   Expressions   { get { return _expressions;             } }
		public          ExpressionType AggregateType { get { return _aggregateType;           } }
		public override Type           Type          { get { return _type;                    } }
		public override ExpressionType NodeType      { get { return AggregateExpressionType;  } }
		public override bool           CanReduce     { get { return false;                    } }

		public BinaryAggregateExpression Update(Expression[] expressions)
		{
			if (ReferenceEquals(_expressions, expressions))
				return this;
			return new BinaryAggregateExpression(AggregateType, Type, expressions);
		}
	}
}