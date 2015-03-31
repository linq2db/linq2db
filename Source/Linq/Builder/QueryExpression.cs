using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	class QueryExpression<T> : Expression
	{
		public QueryExpression(IExpressionBuilder expressionBuilder, Type newExpressionType)
		{
			AddBuilder(expressionBuilder, newExpressionType);
		}

		Type               _type;
		IExpressionBuilder _last;

		readonly List<IExpressionBuilder> _builders = new List<IExpressionBuilder>();

		public override Type           Type      { get { return _type; } }
		public override bool           CanReduce { get { return true;  } }
		public override ExpressionType NodeType  { get { return ExpressionType.Extension; } }

		public override Expression Reduce()
		{
			var expr = _last.BuildQuery<T>();

//			if (expr.Type != Type)
//				expr = Convert(expr, Type);

			return expr;
		}

		public QueryExpression<T> AddBuilder(IExpressionBuilder expressionBuilder, Type newExpressionType)
		{
			if (_last != null)
			{
				_last.Next = expressionBuilder;
				expressionBuilder.Prev = _last;
			}

			_type = newExpressionType;
			_last = expressionBuilder;

			_builders.Add(_last);

			return this;
		}
	}
}
