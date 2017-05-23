namespace LinqToDB.Expressions
{
	using System;
	using System.Linq.Expressions;

	public class BinaryAggregateExpression : Expression
	{
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
		public override ExpressionType NodeType      { get { return ExpressionType.Extension; } }
		public override bool           CanReduce     { get { return true;                     } }

		public BinaryAggregateExpression Update(Expression[] expressions)
		{
			if (ReferenceEquals(_expressions, expressions))
				return this;
			return new BinaryAggregateExpression(AggregateType, Type, expressions);
		}

		public override Expression Reduce()
		{
			var result = _expressions[0];
			for (int i = 1; i < _expressions.Length; i++)
			{
				result = Expression.MakeBinary(AggregateType, result, _expressions[i]);
			}

			return result;
		}
	}
}
