using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	class QueryExpression : Expression
	{
		public QueryExpression(Query query, ExpressionBuilderBase expressionBuilder)
		{
			Query = query;

			AddClause(expressionBuilder);
		}

		public readonly Query Query;

		ExpressionBuilderBase _last;

		readonly List<ExpressionBuilderBase> _builders = new List<ExpressionBuilderBase>();

		public override Type           Type      { get { return _last.Type; } }
		public override bool           CanReduce { get { return true;       } }
		public override ExpressionType NodeType  { get { return ExpressionType.Extension; } }

		public override Expression Reduce()
		{
			return _last.BuildQuery();
		}

		public QueryExpression AddClause(ExpressionBuilderBase expressionBuilder)
		{
			if (_last != null)
			{
				_last.Next = expressionBuilder;
				expressionBuilder.Prev = _last;
			}

			_last = expressionBuilder;

			_builders.Add(_last);

			return _last.Query = this;
		}
	}
}
